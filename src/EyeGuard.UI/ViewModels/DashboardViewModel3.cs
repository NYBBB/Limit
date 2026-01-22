using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;
using EyeGuard.Core.Models;
using EyeGuard.Core.Interfaces;
using EyeGuard.Infrastructure.Services;
using EyeGuard.UI.Controls;
using EyeGuard.UI.Services;
using CommunityToolkit.Mvvm.Input;

namespace EyeGuard.UI.ViewModels;

/// <summary>
/// DashboardViewModel3 - Limit 3.0 Bento Grid 布局 ViewModel
/// 使用单例模式保持状态
/// </summary>
public partial class DashboardViewModel3 : ObservableObject
{
    private static DashboardViewModel3? _instance;
    public static DashboardViewModel3 Instance => _instance ??= new DashboardViewModel3();

    private DispatcherQueueTimer? _timer;
    private readonly UserActivityManager _activityManager;
    private readonly SettingsService _settingsService;
    private readonly DatabaseService _databaseService;
    private readonly FatigueEngine _fatigueEngine;
    private readonly IWindowTracker _windowTracker;
    private readonly ClusterService _clusterService;
    private readonly ToastNotificationService _toastService;

    // ===== Zone A: 精力反应堆属性 =====

    [ObservableProperty]
    private double _fatigueValue = 0;
    
    partial void OnFatigueValueChanged(double value)
    {
        // 同步更新 Debug 滑块和文本
        OnPropertyChanged(nameof(DebugFatigueValue));
        OnPropertyChanged(nameof(DebugFatigueText));
    }
    
    public string DebugFatigueText => _fatigueValue.ToString("F1");

    [ObservableProperty]
    private bool _isMonitoring = false;

    [ObservableProperty]
    private string _monitoringStatus = "未启动";
    
    [ObservableProperty]
    private string _statusLabel = "精力充沛";

    [ObservableProperty]
    private Brush _stateIndicatorColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));

    [ObservableProperty]
    private Visibility _careModeVisibility = Visibility.Collapsed;

    // Phase 10: 省电模式
    [ObservableProperty]
    private Visibility _ecoModeVisibility = Visibility.Collapsed;
    
    [ObservableProperty]
    private bool _isEcoMode = false;

    [ObservableProperty]
    private string _startButtonText = "开始监测";
    
    [ObservableProperty]
    private string _startButtonIcon = "\uE768"; // Play icon

    // ===== Zone B: 上下文感知属性 =====

    [ObservableProperty]
    private string _currentAppName = "未检测";

    [ObservableProperty]
    private string _currentAppIcon = "\uE74C";

    [ObservableProperty]
    private string _currentClusterName = "未分类";

    [ObservableProperty]
    private Brush _currentClusterColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));

    [ObservableProperty]
    private bool _isFocusingMode = false;
    
    [ObservableProperty]
    private string _currentSessionTime = "00:00:00";
    
    // 当前应用的使用时长（秒）
    private int _currentAppSessionSeconds = 0;
    private string _lastAppName = "";
    
    // 时间流：最近应用历史（用于显示切换路径）
    private readonly List<string> _recentApps = new();
    
    [ObservableProperty]
    private string _recentApp1Icon = "";
    
    [ObservableProperty]
    private string _recentApp2Icon = "";
    
    [ObservableProperty]
    private string _recentApp3Icon = "";

    // ===== Phase 9: Focus Commitment 专注承诺属性 =====
    
    [ObservableProperty]
    private bool _isFocusCommitmentActive = false;
    
    [ObservableProperty]
    private int _focusRemainingSeconds = 0;
    
    [ObservableProperty]
    private int _focusTotalSeconds = 0;
    
    [ObservableProperty]
    private string _focusTaskName = "";
    
    [ObservableProperty]
    private string _focusCountdownText = "00:00";
    
    // 专注计时器
    private DispatcherTimer? _focusTimer;
    private DateTime _focusStartTime;

    // ===== Zone C: 消耗排行属性 =====

    public ObservableCollection<DrainerItem> TopDrainers { get; } = new();

    [ObservableProperty]
    private Visibility _showFragmentWarning = Visibility.Collapsed;

    [ObservableProperty]
    private string _fragmentTimeText = "0分钟";

    // ===== Cluster 颜色映射 =====
    private static readonly Dictionary<string, Color> ClusterColors = new()
    {
        { "Coding", Color.FromArgb(255, 0, 120, 215) },      // Blue
        { "Writing", Color.FromArgb(255, 16, 124, 16) },     // Green
        { "Meeting", Color.FromArgb(255, 255, 140, 0) },     // Orange
        { "Entertainment", Color.FromArgb(255, 232, 17, 35) } // Red
    };

    // ===== 应用使用记录 =====
    private Dictionary<string, int> _appUsageSeconds = new();
    
    // ===== Debug 面板属性 =====
    
    [ObservableProperty]
    private bool _showDebugPanel = false;
    
    [ObservableProperty]
    private string _debugUserState = "Idle";
    
    [ObservableProperty]
    private string _debugFatiguePrecise = "0.00%";
    
    [ObservableProperty]
    private string _debugIdleSeconds = "0s";
    
    [ObservableProperty]
    private string _debugSensitivityBias = "0.00";
    
    [ObservableProperty]
    private string _debugCareMode = "关闭";
    
    [ObservableProperty]
    private string _debugPassiveConsumption = "否";
    
    [ObservableProperty]
    private string _debugIsFullscreen = "否";
    
    [ObservableProperty]
    private string _debugSnapshotCountdown = "--";
    
    [ObservableProperty]
    private string _debugTodaySnapshots = "0";
    
    [ObservableProperty]
    private string _debugSoftThreshold = "40%";
    
    [ObservableProperty]
    private string _debugForceThreshold = "80%";
    
    // ===== Debug 控制 =====
    
    public double DebugFatigueValue
    {
        get => _fatigueValue;
        set
        {
            if (_fatigueEngine != null)
            {
                _fatigueEngine.SetFatigue(value);
                FatigueValue = value; // 触发 UI 更新
                OnPropertyChanged(nameof(DebugFatigueValue));
            }
        }
    }

    [RelayCommand]
    private void TriggerDebugNotification(string type)
    {
        if (_toastService == null) return;
        
        switch (type)
        {
            case "Info":
                _toastService.ShowNotification("EyeGuard Debug", "这是一个信息通知测试。");
                break;
            case "Warning":
                _toastService.ShowInterventionNotification(FatigueValue, "检测到疲劳累积，建议适应性休息。");
                break;
            case "Break":
                _toastService.ShowBreakTaskNotification("眼球运动操", 30);
                break;
        }
    }
    
    [ObservableProperty]
    private string _debugInterventionMode = "平衡";
    
    // Phase 7: 快照保存计时器
    private int _secondsSinceLastSnapshot = 0;
    private int _todaySnapshotCount = 0;

    private DashboardViewModel3()
    {
        var services = App.Services;
        _activityManager = services.GetRequiredService<UserActivityManager>();
        _settingsService = services.GetRequiredService<SettingsService>();
        _databaseService = services.GetRequiredService<DatabaseService>();
        // Phase 7: 从 UserActivityManager 获取 FatigueEngine，确保单例
        _fatigueEngine = _activityManager.FatigueEngine;
        _windowTracker = services.GetRequiredService<IWindowTracker>();
        _clusterService = services.GetRequiredService<ClusterService>();
        _toastService = services.GetRequiredService<ToastNotificationService>();
        
        // Phase 7: 应用初始设置
        ApplySettings();
        
        // Phase 7: 监听设置变更
        _settingsService.SettingsChanged += (s, e) => ApplySettings();
        
        // Phase 10: 订阅电源感知事件
        var powerService = PowerAwarenessService.Instance;
        powerService.EcoModeChanged += OnEcoModeChanged;
        UpdateEcoModeUI(powerService.IsEcoModeActive);
        
        // Phase 7: 加载初始数据
        _ = LoadInitialDataAsync();
    }
    
    // Phase 10: 处理 Eco 模式变化
    private void OnEcoModeChanged(object? sender, bool isEcoMode)
    {
        _dispatcherQueue?.TryEnqueue(() => UpdateEcoModeUI(isEcoMode));
    }
    
    private void UpdateEcoModeUI(bool isEcoMode)
    {
        IsEcoMode = isEcoMode;
        EcoModeVisibility = isEcoMode ? Visibility.Visible : Visibility.Collapsed;
        Debug.WriteLine($"[DashboardVM3] Eco mode: {isEcoMode}");
    }
    
    /// <summary>
    /// 将用户设置应用到 UserActivityManager
    /// </summary>
    private void ApplySettings()
    {
        var settings = _settingsService.Settings;
        
        // 应用空闲阈值
        _activityManager.DefaultIdleThresholdSeconds = settings.IdleThresholdSeconds;
        
        // 应用媒体模式阈值（智能模式时生效，通常为普通阈值的2倍）
        _activityManager.MediaModeIdleThresholdSeconds = settings.IdleThresholdSeconds * 2;
        
        Debug.WriteLine($"[DashboardVM3] Settings applied - IdleThreshold: {settings.IdleThresholdSeconds}s");
    }

    private DispatcherQueue? _dispatcherQueue;
    
    public void InitializeTimer(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
        
        if (_timer == null)
        {
            _timer = dispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            Debug.WriteLine("[DashboardVM3] Timer initialized");
            
            // 自动开始监测
            ToggleMonitoring();
        }
    }

    // ===== 定时器 Tick =====

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (!IsMonitoring) return;

        // 执行活动管理器 tick
        _activityManager.Tick();

        // 更新 Zone A
        UpdateZoneA();

        // 更新 Zone B
        UpdateZoneB();

        // 更新 Zone C (每10秒更新一次)
        if (DateTime.Now.Second % 10 == 0)
        {
            UpdateZoneC();
        }
        
        // Phase 7: 疑劳快照保存（同步设置中的间隔）
        _secondsSinceLastSnapshot++;
        var snapshotInterval = _settingsService.Settings.FatigueChartIntervalMinutes * 60;
        
        if (_secondsSinceLastSnapshot >= snapshotInterval)
        {
            _secondsSinceLastSnapshot = 0;
            SaveFatigueSnapshotAsync();
        }
    }

    /// <summary>
    /// 更新 Zone A: 精力反应堆
    /// </summary>
    private void UpdateZoneA()
    {
        FatigueValue = _fatigueEngine.FatigueValue;
        
        // 更新状态指示灯颜色
        var state = _activityManager.CurrentState;
        StateIndicatorColor = state switch
        {
            UserActivityState.Active => new SolidColorBrush(Color.FromArgb(255, 0, 178, 148)),  // Teal
            UserActivityState.PassiveConsumption => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)),  // Amber
            UserActivityState.Idle => new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)),  // Gray
            UserActivityState.Away => new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),  // Dark Gray
            _ => new SolidColorBrush(Color.FromArgb(255, 136, 136, 136))
        };

        MonitoringStatus = state switch
        {
            UserActivityState.Active => "主动工作中",
            UserActivityState.PassiveConsumption => "被动消耗中",
            UserActivityState.Idle => "休息恢复中",
            UserActivityState.Away => "离开中",
            _ => "监测中"
        };
        
        // Phase 2.5: 更新状态标签
        StatusLabel = FatigueValue switch
        {
            < 30 => "精力充沛",
            < 50 => "专注中",
            < 70 => "略感疲惫",
            < 85 => "能量不足",
            _ => "需要休息"
        };

        // 更新 Care Mode 指示器
        CareModeVisibility = _fatigueEngine.IsCareMode ? Visibility.Visible : Visibility.Collapsed;
        
        // 更新调试属性
        if (ShowDebugPanel)
        {
            UpdateDebugInfo();
        }
    }
    
    /// <summary>
    /// 更新调试面板信息
    /// </summary>
    private void UpdateDebugInfo()
    {
        var debugInfo = _activityManager.GetDebugInfo();
        
        DebugUserState = debugInfo.ContainsKey("状态") ? debugInfo["状态"] : "Unknown";
        DebugFatiguePrecise = $"{_fatigueEngine.FatigueValue:F2}%";
        DebugIdleSeconds = $"{debugInfo.GetValueOrDefault("空闲秒数", "0")} (阈值:{_settingsService.Settings.IdleThresholdSeconds}s)";
        DebugSensitivityBias = $"{_fatigueEngine.SensitivityBias:F2}";
        DebugCareMode = _fatigueEngine.IsCareMode ? "开启" : "关闭";
        DebugPassiveConsumption = debugInfo.ContainsKey("被动消耗") ? debugInfo["被动消耗"] : "否";
        
        var currentWindow = _windowTracker.GetActiveWindow();
        DebugIsFullscreen = debugInfo.ContainsKey("全屏") ? debugInfo["全屏"] : "否";
        
        // Phase 7: 快照状态
        var snapshotInterval = _settingsService.Settings.FatigueChartIntervalMinutes * 60;
        var remaining = snapshotInterval - _secondsSinceLastSnapshot;
        DebugSnapshotCountdown = $"{remaining}s";
        DebugTodaySnapshots = _todaySnapshotCount.ToString();
        
        // Phase 8: 提醒阈值
        DebugSoftThreshold = $"{_settingsService.Settings.SoftReminderThreshold}%";
        DebugForceThreshold = $"{_settingsService.Settings.ForceBreakThreshold}%";
        DebugInterventionMode = _settingsService.Settings.InterventionMode switch
        {
            0 => "礼貌",
            1 => "平衡",
            2 => "强制",
            _ => "平衡"
        };
    }
    
    /// <summary>
    /// 切换调试面板显示
    /// </summary>
    public void ToggleDebugPanel()
    {
        ShowDebugPanel = !ShowDebugPanel;
        if (ShowDebugPanel)
        {
            UpdateDebugInfo();
        }
    }

    /// <summary>
    /// 更新 Zone B: 上下文感知
    /// </summary>
    private void UpdateZoneB()
    {
        try
        {
            var windowInfo = _windowTracker.GetActiveWindow();
            if (windowInfo != null)
            {
                var appDisplayName = !string.IsNullOrEmpty(windowInfo.SanitizedTitle) 
                    ? windowInfo.SanitizedTitle 
                    : windowInfo.ProcessName;
                
                // Phase 2.5: Session Timer - 跟踪当前应用使用时长
                if (appDisplayName != _lastAppName)
                {
                    // 切换到新应用，更新时间流历史
                    if (!string.IsNullOrEmpty(_lastAppName))
                    {
                        // 将旧应用添加到历史（最多保留3个）
                        _recentApps.Insert(0, _lastAppName);
                        if (_recentApps.Count > 3)
                        {
                            _recentApps.RemoveAt(3);
                        }
                        UpdateRecentAppIcons();
                    }
                    
                    // 切换到新应用，重置计时
                    _currentAppSessionSeconds = 0;
                    _lastAppName = appDisplayName;
                }
                else
                {
                    _currentAppSessionSeconds++;
                }
                
                // 更新显示
                CurrentAppName = appDisplayName;
                CurrentSessionTime = FormatSessionTime(_currentAppSessionSeconds);

                // 获取应用归属的 Cluster
                var cluster = _clusterService.GetClusterForProcess(windowInfo.ProcessName);
                if (cluster != null)
                {
                    CurrentClusterName = cluster.Name;
                    if (ClusterColors.TryGetValue(cluster.Name, out var clusterColor))
                    {
                        CurrentClusterColor = new SolidColorBrush(clusterColor);
                    }
                }
                else
                {
                    CurrentClusterName = "未分类";
                    CurrentClusterColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));
                }

                // 累计应用使用时间
                var appKey = CurrentAppName;
                if (!_appUsageSeconds.ContainsKey(appKey))
                {
                    _appUsageSeconds[appKey] = 0;
                }
                _appUsageSeconds[appKey]++;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardVM3] UpdateZoneB error: {ex.Message}");
        }
    }
    
    private string FormatSessionTime(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.ToString(@"hh\:mm\:ss");
    }
    
    /// <summary>
    /// 更新时间流图标
    /// </summary>
    private void UpdateRecentAppIcons()
    {
        RecentApp1Icon = _recentApps.Count > 0 ? Services.IconMapper.GetAppIcon(_recentApps[0]) : "";
        RecentApp2Icon = _recentApps.Count > 1 ? Services.IconMapper.GetAppIcon(_recentApps[1]) : "";
        RecentApp3Icon = _recentApps.Count > 2 ? Services.IconMapper.GetAppIcon(_recentApps[2]) : "";
    }

    /// <summary>
    /// 更新 Zone C: 消耗排行
    /// </summary>
    private void UpdateZoneC()
    {
        try
        {
            // 按使用时间排序，取 Top 3
            var topApps = _appUsageSeconds
                .OrderByDescending(x => x.Value)
                .Take(3)
                .ToList();

            TopDrainers.Clear();
            int rank = 1;
            int maxSeconds = topApps.FirstOrDefault().Value;

            foreach (var app in topApps)
            {
                TopDrainers.Add(new DrainerItem
                {
                    Rank = rank++,
                    Name = app.Key,
                    IconGlyph = Services.IconMapper.GetAppIcon(app.Key),
                    Percentage = maxSeconds > 0 ? (app.Value * 100.0 / maxSeconds) : 0,
                    Duration = FormatDuration(app.Value),
                    BarColor = GetDrainerBarColor(rank - 1)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardVM3] UpdateZoneC error: {ex.Message}");
        }
    }

    private string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds}秒";
        if (seconds < 3600) return $"{seconds / 60}分钟";
        return $"{seconds / 3600}小时{(seconds % 3600) / 60}分";
    }

    private Brush GetDrainerBarColor(int rank)
    {
        return rank switch
        {
            0 => new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)),  // Red
            1 => new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)),  // Orange
            2 => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)),  // Amber
            _ => new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)) // Gray
        };
    }

    // ===== 操作方法 =====

    public void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            // 停止
            IsMonitoring = false;
            StartButtonText = "开始监测";
            StartButtonIcon = "\uE768"; // Play
            MonitoringStatus = "已停止";
            _activityManager.Stop();
            _timer?.Stop();
        }
        else
        {
            // 开始
            IsMonitoring = true;
            StartButtonText = "停止监测";
            StartButtonIcon = "\uE71A"; // Pause
            MonitoringStatus = "启动中...";
            _activityManager.Start();
            _timer?.Start();
        }
    }

    public void ResetAll()
    {
        _fatigueEngine.Reset();
        _appUsageSeconds.Clear();
        TopDrainers.Clear();
        FatigueValue = 0;
    }

    // ===== 校准方法 (Limit 3.0) =====

    public void CalibrateAsTired()
    {
        _fatigueEngine.CalibrateAsTired();
        CareModeVisibility = Visibility.Visible;
        Debug.WriteLine("[DashboardVM3] Calibrated as tired - Care Mode activated");
    }

    public void CalibrateAsFresh()
    {
        _fatigueEngine.CalibrateAsFresh();
        Debug.WriteLine("[DashboardVM3] Calibrated as fresh");
    }

    public void CalibrateAfterRest()
    {
        _fatigueEngine.CalibrateAfterRest();
        Debug.WriteLine("[DashboardVM3] Calibrated after rest");
    }

    // ===== Zone B: Focusing 模式切换 =====

    public void SetFocusingMode(bool isFocusing)
    {
        IsFocusingMode = isFocusing;
        // TODO: 将模式切换同步到疲劳引擎（影响负载权重）
        Debug.WriteLine($"[DashboardVM3] Focusing mode set to: {isFocusing}");
    }
    
    // ===== Phase 9: Focus Commitment 专注承诺方法 =====
    
    /// <summary>
    /// 启动专注承诺模式
    /// </summary>
    public void StartFocusCommitment(int totalMinutes, string taskName)
    {
        FocusTotalSeconds = totalMinutes * 60;
        FocusRemainingSeconds = FocusTotalSeconds;
        FocusTaskName = taskName;
        _focusStartTime = DateTime.Now;
        
        // 更新倒计时文本
        UpdateFocusCountdownText();
        
        // 启动专注计时器（每秒更新）
        _focusTimer?.Stop();
        _focusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _focusTimer.Tick += OnFocusTimerTick;
        _focusTimer.Start();
        
        IsFocusCommitmentActive = true;
        
        Debug.WriteLine($"[DashboardVM3] Focus Commitment started: {totalMinutes}min - {taskName}");
    }
    
    /// <summary>
    /// 停止专注承诺模式
    /// </summary>
    public void StopFocusCommitment()
    {
        _focusTimer?.Stop();
        _focusTimer = null;
        
        IsFocusCommitmentActive = false;
        FocusRemainingSeconds = 0;
        FocusTotalSeconds = 0;
        FocusTaskName = "";
        
        Debug.WriteLine("[DashboardVM3] Focus Commitment stopped");
    }
    
    private void OnFocusTimerTick(object? sender, object e)
    {
        if (FocusRemainingSeconds > 0)
        {
            FocusRemainingSeconds--;
            UpdateFocusCountdownText();
        }
        else
        {
            // 时间到，触发完成
            OnFocusCommitmentComplete();
        }
    }
    
    private void UpdateFocusCountdownText()
    {
        int minutes = FocusRemainingSeconds / 60;
        int seconds = FocusRemainingSeconds % 60;
        FocusCountdownText = $"{minutes:D2}:{seconds:D2}";
    }
    
    private void OnFocusCommitmentComplete()
    {
        _focusTimer?.Stop();
        _focusTimer = null;
        
        var taskName = FocusTaskName;
        IsFocusCommitmentActive = false;
        
        Debug.WriteLine($"[DashboardVM3] Focus Commitment complete: {taskName}");
        
        // TODO: 发送 Toast 通知
        // TODO: 保存 FocusSession 到数据库
    }
    
    // ===== Phase 7: 数据持久化方法 =====
    
    /// <summary>
    /// 加载初始数据（疲劳值恢复、今日快照数量）
    /// </summary>
    private async Task LoadInitialDataAsync()
    {
        try
        {
            // 恢复今日疲劳值
            var latestSnapshot = await _databaseService.GetLatestFatigueSnapshotAsync();
            if (latestSnapshot != null)
            {
                if (latestSnapshot.Date == DateTime.Today)
                {
                    // 同一天，恢复疲劳值
                    _fatigueEngine.SetFatigue(latestSnapshot.FatigueValue);
                    FatigueValue = latestSnapshot.FatigueValue;
                    Debug.WriteLine($"[DashboardVM3] 恢复今日疲劳值: {latestSnapshot.FatigueValue:F2}%");
                }
                else
                {
                    Debug.WriteLine($"[DashboardVM3] 跨天重置，上次记录: {latestSnapshot.Date:yyyy-MM-dd}");
                }
            }
            
            // 统计今日快照数量
            var todaySnapshots = await _databaseService.GetFatigueSnapshotsAsync(DateTime.Today);
            _todaySnapshotCount = todaySnapshots.Count;
            
            // 加载今日使用记录并恢复 _appUsageSeconds
            var usageRecords = await _databaseService.GetUsageForDateAsync(DateTime.Today);
            int totalSeconds = usageRecords.Sum(r => r.DurationSeconds);
            _activityManager.SetInitialTodayActiveSeconds(totalSeconds);
            
            // Phase 8: 恢复精力排行数据
            _appUsageSeconds.Clear();
            foreach (var record in usageRecords)
            {
                if (!string.IsNullOrEmpty(record.AppName))
                {
                    if (_appUsageSeconds.ContainsKey(record.AppName))
                    {
                        _appUsageSeconds[record.AppName] += record.DurationSeconds;
                    }
                    else
                    {
                        _appUsageSeconds[record.AppName] = record.DurationSeconds;
                    }
                }
            }
            
            // 刷新 TopDrainers 显示
            UpdateZoneC();
            
            Debug.WriteLine($"[DashboardVM3] 初始化完成: 快照={_todaySnapshotCount}, 使用时间={totalSeconds / 60}分钟, 应用数={_appUsageSeconds.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardVM3] LoadInitialData error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 异步保存疲劳快照到数据库
    /// </summary>
    private async void SaveFatigueSnapshotAsync()
    {
        try
        {
            var fatigueValue = _fatigueEngine.FatigueValue;
            await _databaseService.SaveFatigueSnapshotAsync(fatigueValue);
            
            _todaySnapshotCount++;
            
            Debug.WriteLine($"[DashboardVM3] 保存疲劳快照: {fatigueValue:F2}% (今日第{_todaySnapshotCount}个)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardVM3] SaveFatigueSnapshot error: {ex.Message}");
        }
    }
}
