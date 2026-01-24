namespace EyeGuard.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using EyeGuard.Core.Models;

/// <summary>
/// 用户活动状态管理器。
/// 综合输入检测和音频检测来判断用户当前状态。
/// </summary>
public class UserActivityManager : IDisposable
{
    private readonly GlobalInputMonitor _inputMonitor;
    private readonly AudioDetector _audioDetector;
    private readonly FatigueEngine _fatigueEngine;
    
    private bool _disposed;
    
    // ===== Limit 3.0: 活跃检测增强 =====
    private bool _isFullscreen = false;
    private string _currentProcessName = "";
    private int _audioSilentSeconds = 0;  // 音频静音累积秒数（去抖用）
    private const int AudioDebounceSeconds = 5;  // 音频静音超过5秒才算真正停止
    
    // ===== Beta 2 (A2): 心流护盾 =====
    private bool _isFlowMode = false;
    private DateTime _flowModeStartTime = DateTime.MinValue;
    private string? _flowModeApp = null;
    private const int FlowModeThresholdSeconds = 20 * 60;  // 20 分钟
    private const double FlowModeFatigueMultiplier = 0.8;  // 心流模式疲劳倍率
    private const double FlowTaxPenalty = 5.0;  // 退出心流惩罚 (+5% 疲劳)
    
    // ===== Beta 2 (A3): 23 分钟切换恢复法则 =====
    private bool _isRefocusing = false;
    private DateTime _refocusStartTime = DateTime.MinValue;
    private const int RefocusDurationSeconds = 23 * 60;  // 23 分钟
    private const double RefocusFatigueMultiplier = 1.5;  // 重聚焦疲劳倍率
    private int? _lastClusterId = null;

    /// <summary>
    /// 当前用户状态。
    /// </summary>
    public UserActivityState CurrentState { get; private set; } = UserActivityState.Idle;
    
    /// <summary>
    /// Limit 3.0: 是否为被动消耗模式（看视频/全屏但无输入）
    /// </summary>
    public bool IsPassiveConsumption => CurrentState == UserActivityState.PassiveConsumption 
                                        || CurrentState == UserActivityState.MediaMode;

    /// <summary>
    /// 默认空闲阈值（秒）- 无音频时。
    /// </summary>
    public int DefaultIdleThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// 媒体模式空闲阈值（秒）- 有音频时。
    /// </summary>
    public int MediaModeIdleThresholdSeconds { get; set; } = 120;

    /// <summary>
    /// 离开阈值（秒）- 超过此时间认为用户离开。
    /// </summary>
    public int AwayThresholdSeconds { get; set; } = 300;
    
    /// <summary>
    /// Limit 3.0: 当前是否全屏
    /// </summary>
    public bool IsFullscreen => _isFullscreen;
    
    /// <summary>
    /// Limit 3.0: 当前进程名
    /// </summary>
    public string CurrentProcessName => _currentProcessName;

    /// <summary>
    /// 疲劳引擎实例。
    /// </summary>
    public FatigueEngine FatigueEngine => _fatigueEngine;

    /// <summary>
    /// 输入监测器实例。
    /// </summary>
    public GlobalInputMonitor InputMonitor => _inputMonitor;

    /// <summary>
    /// 音频检测器实例。
    /// </summary>
    public AudioDetector AudioDetector => _audioDetector;

    /// <summary>
    /// 是否正在运行。
    /// </summary>
    public bool IsRunning => _inputMonitor.IsRunning;

    /// <summary>
    /// 今日活跃时长（秒）。
    /// </summary>
    public int TodayActiveSeconds { get; private set; }

    public void SetInitialTodayActiveSeconds(int seconds)
    {
        TodayActiveSeconds = seconds;
    }

    /// <summary>
    /// 当前连续工作时长（秒）。
    /// </summary>
    public int CurrentSessionSeconds { get; private set; }

    /// <summary>
    /// 今日最长连续工作时长（秒）。
    /// </summary>
    public int LongestSessionSeconds { get; private set; }

    /// <summary>
    /// 状态变化事件。
    /// </summary>
    public event EventHandler<UserActivityState>? StateChanged;
    
    /// <summary>
    /// Beta 2 (A2): 心流模式是否激活
    /// </summary>
    public bool IsFlowMode => _isFlowMode;
    
    /// <summary>
    /// Beta 2 (A3): 是否在重聚焦状态
    /// </summary>
    public bool IsRefocusing => _isRefocusing;

    /// <summary>
    /// Phase 7: 构造函数 - 使用 DI 注入 FatigueEngine 保证单例
    /// </summary>
    public UserActivityManager(FatigueEngine fatigueEngine, GlobalInputMonitor inputMonitor, AudioDetector audioDetector)
    {
        _fatigueEngine = fatigueEngine;
        _inputMonitor = inputMonitor;
        _audioDetector = audioDetector;
    }

    /// <summary>
    /// 启动监测。
    /// </summary>
    public void Start()
    {
        _inputMonitor.Start();
        Debug.WriteLine("[UserActivityManager] Started");
    }

    /// <summary>
    /// 停止监测。
    /// </summary>
    public void Stop()
    {
        _inputMonitor.Stop();
        Debug.WriteLine("[UserActivityManager] Stopped");
    }

    /// <summary>
    /// 每秒调用一次，更新状态和疲劳值。
    /// Limit 3.0: 活跃检测公式 IsActive = (Input || (Audio && MediaApp) || Fullscreen)
    /// </summary>
    public void Tick()
    {
        if (!IsRunning) return;

        // 检测输入状态
        _inputMonitor.CheckIdleState();
        
        double idleSeconds = _inputMonitor.IdleSeconds;
        bool isAudioPlaying = _audioDetector.IsAudioPlaying;
        
        // Phase 7 修复：直接从 SettingsService 读取设置（不再使用硬编码或事件同步）
        var settings = SettingsService.Instance.Settings;
        int idleThreshold = settings.IdleThresholdSeconds;
        int mediaIdleThreshold = idleThreshold * 2;  // 媒体模式阈值为普通阈值的2倍
        
        // Limit 3.0: 音频去抖逻辑（修复状态横跳）
        // 音频可能因为视频静音片段导致瞬间静音，需要缓冲
        if (isAudioPlaying)
        {
            _audioSilentSeconds = 0;  // 有音频，重置静音计数
        }
        else
        {
            _audioSilentSeconds++;  // 静音累积
        }
        
        // 判定是否有持续音频（5秒内有过音频就认为有）
        bool hasAudio = (_audioSilentSeconds < AudioDebounceSeconds);
        
        // Limit 3.0: 活跃检测公式
        bool isPassivelyActive = hasAudio || _isFullscreen;
        
        // 确定当前空闲阈值（直接使用设置值）
        int currentIdleThreshold = (hasAudio || _isFullscreen)
            ? mediaIdleThreshold 
            : idleThreshold;

        // 状态机逻辑
        var previousState = CurrentState;
        
        // Phase 7 修复：使用设置的空闲阈值判断 hasRecentInput
        // 原来硬编码为 2 秒，现在正确使用设置
        bool hasRecentInput = idleSeconds < idleThreshold;
        
        // Limit 3.0 判定逻辑（修复状态横跳问题）：
        // 1. 有输入（idleSeconds < idleThreshold）-> Active
        // 2. 无输入但有音频/全屏，且未超过阈值 -> PassiveConsumption (低负载，不恢复)
        // 3. 无输入无音频，超过空闲阈值 -> Idle (恢复疲劳)
        // 4. 长时间无活动 -> Away (快速恢复)
        
        if (hasRecentInput)
        {
            // 有物理输入 - 主动工作状态
            CurrentState = UserActivityState.Active;
            CurrentSessionSeconds++;
            TodayActiveSeconds++;
            
            // Beta 2: 检测心流模式和重聚焦状态
            CheckFlowMode();
            CheckRefocusStatus();
            
            // 计算有效疲劳倍率
            double fatigueMultiplier = 1.0;
            if (_isFlowMode) fatigueMultiplier *= FlowModeFatigueMultiplier;  // A2: 心流保护
            if (_isRefocusing) fatigueMultiplier *= RefocusFatigueMultiplier; // A3: 重聚焦惩罚
            
            _fatigueEngine.IncreaseFatigue(fatigueMultiplier, isMediaMode: false);
        }
        else if (isPassivelyActive && idleSeconds < currentIdleThreshold)
        {
            // 无输入但有音频/全屏，且未超过阈值 - 被动消耗状态 (Limit 3.0)
            // 关键修复：看视频不再被当成空闲！
            CurrentState = UserActivityState.PassiveConsumption;
            TodayActiveSeconds++;
            // 被动消耗：低负载疲劳增长，不恢复
            _fatigueEngine.IncreaseFatigue(1, isMediaMode: true, reasonCode: "PASSIVE_CONSUMPTION");
        }
        else if (idleSeconds < AwayThresholdSeconds)
        {
            // 真正的空闲状态（无输入、无音频或音频超时、非全屏）
            CurrentState = UserActivityState.Idle;
            CurrentSessionSeconds = 0;
            
            // 空闲状态：恢复疲劳
            _fatigueEngine.DecreaseFatigue(1);
        }
        else
        {
            // 离开状态
            CurrentState = UserActivityState.Away;
            CurrentSessionSeconds = 0;
            
            // 离开状态：快速恢复疲劳
            _fatigueEngine.DecreaseFatigue(2);
        }

        // 更新最长连续时间
        if (CurrentSessionSeconds > LongestSessionSeconds)
        {
            LongestSessionSeconds = CurrentSessionSeconds;
        }

        // 触发状态变化事件
        if (previousState != CurrentState)
        {
            Debug.WriteLine($"[UserActivityManager] State changed: {previousState} -> {CurrentState}");
            StateChanged?.Invoke(this, CurrentState);
        }
    }

    /// <summary>
    /// 重置所有统计数据。
    /// </summary>
    public void Reset()
    {
        _fatigueEngine.Reset();
        TodayActiveSeconds = 0;
        CurrentSessionSeconds = 0;
        LongestSessionSeconds = 0;
        CurrentState = UserActivityState.Idle;
    }
    
    /// <summary>
    /// 仅重置当前连续工作时长（用于休息任务完成后）
    /// </summary>
    public void ResetCurrentSession()
    {
        CurrentSessionSeconds = 0;
        Debug.WriteLine("[UserActivityManager] CurrentSession reset");
    }

    /// <summary>
    /// 获取状态描述文本。
    /// </summary>
    public string GetStateDescription()
    {
        return CurrentState switch
        {
            UserActivityState.Active => "正在工作中",
            UserActivityState.MediaMode => "媒体模式（看视频/听音乐）",
            UserActivityState.PassiveConsumption => "被动消耗（全屏/音频）",  // Limit 3.0
            UserActivityState.Idle => "用户空闲，正在恢复...",
            UserActivityState.Away => "用户已离开",
            _ => "未知状态"
        };
    }
    
    // ===== Limit 3.0: 全屏检测与状态设置 =====
    
    /// <summary>
    /// 设置当前全屏状态（由外部窗口检测调用）
    /// </summary>
    public void SetFullscreenState(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
    }
    
    /// <summary>
    /// 设置当前进程名（由外部窗口检测调用）
    /// </summary>
    public void SetCurrentProcess(string processName)
    {
        _currentProcessName = processName;
    }
    
    // ===== Beta 2 (A2): 心流护盾检测 =====
    
    /// <summary>
    /// 检测并更新心流模式状态
    /// </summary>
    private void CheckFlowMode()
    {
        // 如果已在心流模式，检查是否需要退出
        if (_isFlowMode)
        {
            // 应用切换 = 退出心流模式 + 惩罚
            if (_currentProcessName != _flowModeApp)
            {
                _isFlowMode = false;
                _fatigueEngine.SetFatigueValue(_fatigueEngine.FatigueValue + FlowTaxPenalty);
                Debug.WriteLine($"[UserActivityManager] ⚡ Flow Shield broken! +{FlowTaxPenalty}% fatigue");
            }
            return;
        }
        
        // 检测是否可以进入心流模式
        // 条件：同一应用连续使用超过 20 分钟
        if (_flowModeApp == _currentProcessName)
        {
            var duration = (DateTime.Now - _flowModeStartTime).TotalSeconds;
            if (duration >= FlowModeThresholdSeconds)
            {
                _isFlowMode = true;
                Debug.WriteLine($"[UserActivityManager] 🎯 Flow Shield activated for {_currentProcessName}!");
            }
        }
        else
        {
            // 应用切换，重置计时器
            _flowModeApp = _currentProcessName;
            _flowModeStartTime = DateTime.Now;
        }
    }
    
    // ===== Beta 2 (A3): 23 分钟切换恢复法则 =====
    
    /// <summary>
    /// 检查重聚焦状态（簇切换后 23 分钟内增加疲劳）
    /// </summary>
    private void CheckRefocusStatus()
    {
        if (_isRefocusing)
        {
            var elapsed = (DateTime.Now - _refocusStartTime).TotalSeconds;
            if (elapsed >= RefocusDurationSeconds)
            {
                _isRefocusing = false;
                Debug.WriteLine("[UserActivityManager] ✅ Refocusing complete (23 min)");
            }
        }
    }
    
    /// <summary>
    /// 外部调用：通知簇切换（触发 A3 法则）
    /// </summary>
    public void NotifyClusterChange(int? newClusterId)
    {
        if (_lastClusterId.HasValue && newClusterId.HasValue && _lastClusterId != newClusterId)
        {
            // 跨簇切换，启动重聚焦计时器
            _isRefocusing = true;
            _refocusStartTime = DateTime.Now;
            Debug.WriteLine($"[UserActivityManager] 🔄 Cluster switch detected - Refocusing started (23 min penalty)");
        }
        _lastClusterId = newClusterId;
    }
    
    /// <summary>
    /// Limit 3.0: 获取调试信息字典
    /// </summary>
    public Dictionary<string, string> GetDebugInfo()
    {
        return new Dictionary<string, string>
        {
            ["状态"] = GetStateDescription(),
            ["空闲秒数"] = $"{_inputMonitor.IdleSeconds:F1}s",
            ["音频播放"] = _audioDetector.IsAudioPlaying ? "是" : "否",
            ["全屏"] = _isFullscreen ? "是" : "否",
            ["被动消耗"] = IsPassiveConsumption ? "是" : "否",
            ["疲劳值"] = $"{_fatigueEngine.FatigueValue:F1}%",
            ["敏感度偏差"] = $"{_fatigueEngine.SensitivityBias:P0}",
            ["关怀模式"] = _fatigueEngine.IsCareMode ? "开启" : "关闭",
            ["疲劳斜率"] = $"{_fatigueEngine.FatigueSlope:F2}%/分",
            ["当前进程"] = string.IsNullOrEmpty(_currentProcessName) ? "未知" : _currentProcessName,
            ["今日活跃"] = $"{TodayActiveSeconds / 60}分钟",
            ["连续工作"] = $"{CurrentSessionSeconds / 60}分钟"
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        Stop();
        _inputMonitor.Dispose();
        _audioDetector.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~UserActivityManager()
    {
        Dispose();
    }
}
