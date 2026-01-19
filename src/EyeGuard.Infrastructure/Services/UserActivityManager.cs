namespace EyeGuard.Infrastructure.Services;

using System;
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

    /// <summary>
    /// 当前用户状态。
    /// </summary>
    public UserActivityState CurrentState { get; private set; } = UserActivityState.Idle;

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

    public UserActivityManager()
    {
        _inputMonitor = new GlobalInputMonitor();
        _audioDetector = new AudioDetector();
        _fatigueEngine = new FatigueEngine();
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
    /// </summary>
    public void Tick()
    {
        if (!IsRunning) return;

        // 检测输入状态
        _inputMonitor.CheckIdleState();
        
        double idleSeconds = _inputMonitor.IdleSeconds;
        bool isAudioPlaying = _audioDetector.IsAudioPlaying;
        
        // 确定当前空闲阈值
        int currentIdleThreshold = isAudioPlaying 
            ? MediaModeIdleThresholdSeconds 
            : DefaultIdleThresholdSeconds;

        // 状态机逻辑
        var previousState = CurrentState;
        
        // 判定逻辑：
        // - idleSeconds < currentIdleThreshold: 用户活跃（正在工作或看视频）
        // - idleSeconds >= currentIdleThreshold: 用户空闲
        // - idleSeconds >= AwayThresholdSeconds: 用户离开
        
        if (idleSeconds < currentIdleThreshold)
        {
            // 未超过空闲阈值 - 用户活跃
            if (isAudioPlaying && idleSeconds > 5)
            {
                // 有音频但无输入超过5秒：媒体模式（看视频/听音乐）
                CurrentState = UserActivityState.MediaMode;
                TodayActiveSeconds++;
                _fatigueEngine.IncreaseFatigue(1, isMediaMode: true);
            }
            else
            {
                // 正常工作状态
                CurrentState = UserActivityState.Active;
                CurrentSessionSeconds++;
                TodayActiveSeconds++;
                _fatigueEngine.IncreaseFatigue(1, isMediaMode: false);
            }
        }
        else if (idleSeconds < AwayThresholdSeconds)
        {
            // 空闲状态（超过空闲阈值但未离开）
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
    /// 获取状态描述文本。
    /// </summary>
    public string GetStateDescription()
    {
        return CurrentState switch
        {
            UserActivityState.Active => "正在工作中",
            UserActivityState.MediaMode => "媒体模式（看视频/听音乐）",
            UserActivityState.Idle => "用户空闲，正在恢复...",
            UserActivityState.Away => "用户已离开",
            _ => "未知状态"
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
