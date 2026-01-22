using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using EyeGuard.Core.Models;
using EyeGuard.Core.Enums;
using EyeGuard.Core.Interfaces;
using EyeGuard.Infrastructure.Services;
using EyeGuard.Core.Entities;

namespace EyeGuard.UI.ViewModels;

/// <summary>
/// 仪表盘页面的 ViewModel。
/// 使用单例模式保持状态。
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private static DashboardViewModel? _instance;
    public static DashboardViewModel Instance => _instance ??= new DashboardViewModel();
    
    private DispatcherQueueTimer? _timer;
    private readonly UserActivityManager _activityManager;
    private readonly SettingsService _settingsService;
    private readonly DatabaseService _databaseService;
    private readonly ForecastService _forecastService;
    private readonly BreakTaskService _breakTaskService;
    private readonly IWindowTracker _windowTracker;
    private readonly InterventionPolicy _interventionPolicy;
    private int _secondsToNextBreak = 15 * 60;
    private const int TopAppsCount = 5; // 默认显示前5个应用
    
    // 疲劳快照保存计时器
    private int _secondsSinceLastSnapshot = 0;
    
    public ObservableCollection<AppUsageItem> TodayUsageApps { get; } = new();
    
    [ObservableProperty]
    private bool _showAllApps = false;
    
    /// <summary>
    /// 显示的应用列表（受ShowAllApps控制）
    /// </summary>
    public ObservableCollection<AppUsageItem> DisplayedApps
    {
        get
        {
            if (ShowAllApps || TodayUsageApps.Count <= TopAppsCount)
                return TodayUsageApps;
            
            var topApps = new ObservableCollection<AppUsageItem>();
            for (int i = 0; i < Math.Min(TopAppsCount, TodayUsageApps.Count); i++)
            {
                topApps.Add(TodayUsageApps[i]);
            }
            return topApps;
        }
    }
    
    /// <summary>
    /// 是否显示展开/收起按钮（当应用数量超过TopAppsCount时显示）
    /// </summary>
    public bool HasMoreApps => TodayUsageApps.Count > TopAppsCount;

    [ObservableProperty]
    private int _fatigueValue = 0;

    [ObservableProperty]
    private int _todayHours = 0;

    [ObservableProperty]
    private int _todayMinutes = 0;

    [ObservableProperty]
    private int _todaySeconds = 0;

    [ObservableProperty]
    private int _nextBreakMinutes = 15;

    [ObservableProperty]
    private int _nextBreakSeconds = 0;

    [ObservableProperty]
    private int _longestSessionMinutes = 0;

    [ObservableProperty]
    private string _statusText = "准备就绪";

    [ObservableProperty]
    private string _currentAppText = "点击开始监测按钮启动";

    [ObservableProperty]
    private bool _isMonitoring = false;

    [ObservableProperty]
    private string _userState = "未启动";

    [ObservableProperty]
    private string _fatigueLevel = "精力充沛";

    [ObservableProperty]
    private bool _isAudioPlaying = false;

    // ===== 开发者调试属性 =====
    
    /// <summary>
    /// 开发模式开关 - 发布时设为 false
    /// </summary>
    public Microsoft.UI.Xaml.Visibility IsDevMode => 
#if DEBUG
        Microsoft.UI.Xaml.Visibility.Visible;
#else
        Microsoft.UI.Xaml.Visibility.Collapsed;
#endif

    [ObservableProperty]
    private string _fatiguePrecise = "0.00%";

    [ObservableProperty]
    private string _idleSecondsText = "0.0s";

    [ObservableProperty]
    private string _audioPeakText = "0.000";

    [ObservableProperty]
    private string _isAudioPlayingText = "否";

    [ObservableProperty]
    private string _nextBreakPrecise = "15:00.0";

    [ObservableProperty]
    private string _currentSessionText = "0分0秒";

    [ObservableProperty]
    private string _recoveryEstimate = "无需休息";

    [ObservableProperty]
    private string _breakSuggestion = "继续工作";

    /// <summary>
    /// 推荐休息时间（智能模式卡片显示）
    /// </summary>
    [ObservableProperty]
    private string _recommendedBreakTime = "无需休息";

    /// <summary>
    /// 是否为智能模式（用于卡片显示切换）
    /// </summary>
    [ObservableProperty]
    private bool _isSmartMode = true;
    
    // ===== Limit 3.0: 主观校准调试属性 =====
    
    /// <summary>
    /// 敏感度偏差文本
    /// </summary>
    [ObservableProperty]
    private string _sensitivityBiasText = "0%";
    
    /// <summary>
    /// 关怀模式是否开启
    /// </summary>
    [ObservableProperty]
    private string _careModeText = "关闭";
    
    /// <summary>
    /// 是否为被动消耗状态
    /// </summary>
    [ObservableProperty]
    private string _passiveConsumptionText = "否";
    
    /// <summary>
    /// 是否全屏
    /// </summary>
    [ObservableProperty]
    private string _isFullscreenText = "否";
    
    // ===== Limit 2.0: 精力预测属性 =====
    
    /// <summary>
    /// 枯竭倒计时文本 (如 "42 分钟")
    /// </summary>
    [ObservableProperty]
    private string _burnoutCountdownText = "> 2 小时";
    
    /// <summary>
    /// 倒计时副标题 (如 "后进入低效区")
    /// </summary>
    [ObservableProperty]
    private string _burnoutCountdownSubtitle = "精力充沛";
    
    /// <summary>
    /// 当前疲劳状态
    /// </summary>
    [ObservableProperty]
    private FatigueState _currentFatigueState = FatigueState.Fresh;
    
    /// <summary>
    /// 疲劳状态对应的颜色
    /// </summary>
    [ObservableProperty]
    private string _fatigueStateColor = "#00C853";
    
    /// <summary>
    /// 延长方案建议 (如 "切换到媒体模式可延长至 1小时15分")
    /// </summary>
    [ObservableProperty]
    private string? _extensionSuggestion;
    
    /// <summary>
    /// 是否显示延长建议
    /// </summary>
    public bool HasExtensionSuggestion => !string.IsNullOrEmpty(ExtensionSuggestion);
    
    /// <summary>
    /// 疲劳变化斜率 (%/分钟)
    /// </summary>
    [ObservableProperty]
    private string _fatigueSlopeText = "0.0%/min";
    
    // ===== Limit 2.0: 休息任务属性 =====
    
    /// <summary>
    /// 是否有待处理的休息任务
    /// </summary>
    [ObservableProperty]
    private bool _hasBreakTask = false;
    
    /// <summary>
    /// 当前休息任务名称
    /// </summary>
    [ObservableProperty]
    private string _breakTaskName = "";
    
    /// <summary>
    /// 当前休息任务描述
    /// </summary>
    [ObservableProperty]
    private string _breakTaskDescription = "";
    
    /// <summary>
    /// 当前休息任务时长（秒）
    /// </summary>
    [ObservableProperty]
    private int _breakTaskDuration = 0;
    
    /// <summary>
    /// 当前休息任务触发原因
    /// </summary>
    [ObservableProperty]
    private string _breakTaskReason = "";

    // 图表数据 - 24小时时间轴
    public ISeries[] Series { get; set; }
    public ICartesianAxis[] XAxes { get; set; }
    public ICartesianAxis[] YAxes { get; set; }
    private readonly ObservableCollection<ObservablePoint> _hourlyFatigueData;
    
    // ===== Limit 2.0: 上下文分类 =====
    
    /// <summary>
    /// 当前上下文状态
    /// </summary>
    [ObservableProperty]
    private ContextState _currentContext = ContextState.Other;
    
    /// <summary>
    /// 当前上下文名称
    /// </summary>
    [ObservableProperty]
    private string _currentContextName = "其他";
    
    // ===== 阶段 5：干预系统属性 =====
    
    /// <summary>
    /// 当前干预级别
    /// </summary>
    [ObservableProperty]
    private InterventionLevel _currentInterventionLevel = InterventionLevel.None;
    
    /// <summary>
    /// 干预消息
    /// </summary>
    [ObservableProperty]
    private string _interventionMessage = "";
    
    /// <summary>
    /// 是否显示干预卡片
    /// </summary>
    public bool ShowInterventionCard => CurrentInterventionLevel >= InterventionLevel.Suggestion;
    
    /// <summary>
    /// 干预卡片边框颜色
    /// </summary>
    public string InterventionBorderColor => CurrentInterventionLevel switch
    {
        InterventionLevel.Nudge => "#FFC107",      // 黄色
        InterventionLevel.Suggestion => "#FF9800", // 橙色
        InterventionLevel.Intervention => "#F44336", // 红色
        _ => "Transparent"
    };

    private DashboardViewModel()
    {
        // 初始化设置服务
        _settingsService = SettingsService.Instance;
        
        // 获取数据库服务
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        
        // Phase 7: 从 DI 获取 UserActivityManager（保证单例）
        _activityManager = App.Services.GetRequiredService<UserActivityManager>();
        
        // 初始化预测服务
        _forecastService = new ForecastService(_activityManager.FatigueEngine);
        
        // 初始化休息任务服务
        _breakTaskService = new BreakTaskService(_activityManager.FatigueEngine);
        _breakTaskService.TaskGenerated += OnBreakTaskGenerated;
        _breakTaskService.TaskCompleted += OnBreakTaskCompleted;
        _breakTaskService.ResetSessionTimer = () => _activityManager.ResetCurrentSession();
        
        // 获取窗口追踪器（用于上下文分类）
        _windowTracker = App.Services.GetRequiredService<IWindowTracker>();
        
        // 初始化干预策略服务 (Phase 5)
        _interventionPolicy = new InterventionPolicy();
        
        // 异步加载初始数据
        LoadInitialDataAsync();
        _activityManager.StateChanged += (s, state) => 
        {
            UserState = _activityManager.GetStateDescription();
        };
        
        // 应用设置到活动管理器
        ApplySettings();
        
        // 监听设置变化
        _settingsService.SettingsChanged += (s, e) => ApplySettings();
        
        // 初始化疲劳趋势数据（空列表，从数据库加载）
        _hourlyFatigueData = new ObservableCollection<ObservablePoint>();
        
        Series = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = _hourlyFatigueData,
                Name = "疲劳值",
                Fill = new SolidColorPaint(new SKColor(138, 43, 226, 40)),  // 填充区域半透明紫色
                Stroke = new SolidColorPaint(new SKColor(138, 43, 226)) { StrokeThickness = 3 },  // 线条粗细从2增加到3
                GeometrySize = 8,  // 数据点大小从6增加到12
                GeometryFill = new SolidColorPaint(new SKColor(138, 43, 226)),  // 紫色圆点填充
                GeometryStroke = null,
                LineSmoothness = 0.3,  // 稍微降低平滑度，让线条更直接连接点
            }
        };

        // 创建支持中文的字体
        var labelPaint = new SolidColorPaint(new SKColor(150, 150, 150))
        {
            SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
        };

        XAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 24,
                ForceStepToMin = true,
                MinStep = 2,
                Labeler = value => value.ToString(),
                TextSize = 12,
                LabelsPaint = labelPaint,
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "疲劳度",
                MinLimit = 0,
                MaxLimit = 100,
                MinStep = 20,
                Labeler = value => $"{value}%",
                TextSize = 12,
                LabelsPaint = labelPaint,
                NamePaint = labelPaint,
            }
        };
    }

    /// <summary>
    /// 应用用户设置到活动管理器。
    /// </summary>
    private void ApplySettings()
    {
        var settings = _settingsService.Settings;
        
        // 应用空闲阈值
        _activityManager.DefaultIdleThresholdSeconds = settings.IdleThresholdSeconds;
        
        // 应用媒体模式阈值（智能模式时生效）
        _activityManager.MediaModeIdleThresholdSeconds = settings.IdleThresholdSeconds * 2;
        
        // 更新模式
        IsSmartMode = settings.IsSmartMode;
        
        Debug.WriteLine($"[DashboardViewModel] Settings applied - Mode: {(settings.IsSmartMode ? "Smart" : "Simple")}, Idle Threshold: {settings.IdleThresholdSeconds}s");
    }

    public void InitializeTimer(DispatcherQueue dispatcherQueue)
    {
        if (_timer == null)
        {
            _timer = dispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            Debug.WriteLine("Timer initialized");
            
            // 自动开始监测（必须在定时器初始化后调用）
            StartSimulation();
        }
    }

    [RelayCommand]
    private void StartSimulation()
    {
        Debug.WriteLine($"StartSimulation called, IsMonitoring = {IsMonitoring}");
        
        if (!IsMonitoring)
        {
            IsMonitoring = true;
            StatusText = "正在监测中";
            CurrentAppText = "正在监听用户活动...";
            _activityManager.Start();
            _timer?.Start();
            Debug.WriteLine("Monitoring started");
        }
        else
        {
            IsMonitoring = false;
            StatusText = "已暂停";
            CurrentAppText = "点击开始监测继续";
            _activityManager.Stop();
            _timer?.Stop();
            Debug.WriteLine("Monitoring stopped");
        }
    }
    
    /// <summary>
    /// 调试用：手动设置疲劳值（测试干预系统）
    /// </summary>
    public void SetDebugFatigueValue(double value)
    {
        _activityManager.FatigueEngine.SetFatigueValue(value);
        FatigueValue = (int)Math.Round(value);
        Debug.WriteLine($"[Debug] Fatigue value set to: {value:F1}%");
    }

    [RelayCommand]
    private void ResetSimulation()
    {
        _activityManager.Stop();
        _activityManager.Reset();
        _timer?.Stop();
        IsMonitoring = false;
        FatigueValue = 0;
        TodayHours = 0;
        TodayMinutes = 0;
        TodaySeconds = 0;
        NextBreakMinutes = 15;
        NextBreakSeconds = 0;
        LongestSessionMinutes = 0;
        StatusText = "准备就绪";
        CurrentAppText = "点击开始监测按钮启动";
        UserState = "未启动";
        FatigueLevel = "精力充沛";
        IsAudioPlaying = false;
        _secondsToNextBreak = 15 * 60;
        
        // 重置图表数据
        for (int hour = 0; hour <= 24; hour++)
        {
            _hourlyFatigueData[hour] = new ObservablePoint(hour, null);
        }
        
        Debug.WriteLine("Simulation reset");
    }

    private void OnTimerTick(object? sender, object e)
    {
        if (!IsMonitoring) return;
        
        // ===== Limit 2.0: 上下文分类 =====
        var activeWindow = _windowTracker.GetActiveWindow();
        if (activeWindow != null)
        {
            // 识别网站（如果是浏览器）
            string? websiteName = null;
            if (WebsiteRecognizer.IsBrowserProcess(activeWindow.ProcessName))
            {
                WebsiteRecognizer.TryRecognizeWebsite(activeWindow.WindowTitle, out websiteName);
            }
            
            // 分类上下文
            CurrentContext = ContextClassifier.Classify(
                activeWindow.ProcessName, 
                websiteName, 
                activeWindow.WindowTitle,
                activeWindow.Url  // Limit 2.0: URL 优先分类
            );
            CurrentContextName = ContextClassifier.GetContextName(CurrentContext);
            
            // 更新疲劳引擎的负荷权重
            _activityManager.FatigueEngine.LoadWeight = ContextClassifier.GetLoadWeight(CurrentContext);
        }

        // 更新活动管理器
        _activityManager.Tick();
        
        var fatigue = _activityManager.FatigueEngine;
        var state = _activityManager.CurrentState;
        
        // ===== 同步数据到 UI =====
        FatigueValue = (int)Math.Round(fatigue.FatigueValue);
        FatigueLevel = fatigue.GetFatigueLevel();
        UserState = _activityManager.GetStateDescription();
        IsAudioPlaying = _activityManager.AudioDetector.IsAudioPlaying;
        
        // ===== Limit 2.0: 更新预测服务和 UI =====
        _forecastService.Update();
        BurnoutCountdownText = _forecastService.GetCountdownText();
        BurnoutCountdownSubtitle = _forecastService.GetCountdownSubtitle();
        CurrentFatigueState = fatigue.CurrentFatigueState;
        FatigueStateColor = fatigue.GetFatigueStateColor();
        ExtensionSuggestion = _forecastService.GetExtensionSuggestionText();
        OnPropertyChanged(nameof(HasExtensionSuggestion));
        FatigueSlopeText = $"{fatigue.FatigueSlope:F2}%/min";
        
        // ===== Phase 5: 干预系统评估 =====
        var intervention = _interventionPolicy.Evaluate(fatigue.FatigueValue, CurrentContext);
        if (intervention.ShouldShow)
        {
            CurrentInterventionLevel = intervention.Level;
            InterventionMessage = intervention.Message;
            OnPropertyChanged(nameof(ShowInterventionCard));
            OnPropertyChanged(nameof(InterventionBorderColor));
        }
        
        // ===== Limit 2.0: 久坐保护检查 =====
        if (state == UserActivityState.Active)
        {
            _breakTaskService.CheckMobilityTaskTrigger(_activityManager.CurrentSessionSeconds);
        }
        
        // 更新时长
        int totalSeconds = _activityManager.TodayActiveSeconds;
        TodayHours = totalSeconds / 3600;
        TodayMinutes = (totalSeconds % 3600) / 60;
        TodaySeconds = totalSeconds % 60;
        
        // 更新最长连续时间
        LongestSessionMinutes = _activityManager.LongestSessionSeconds / 60;
        
        // ===== 开发者调试数据 =====
        FatiguePrecise = $"{fatigue.FatigueValue:F2}%";
        IdleSecondsText = $"{_activityManager.InputMonitor.IdleSeconds:F1}s";
        AudioPeakText = $"{_activityManager.AudioDetector.CurrentPeakValue:F3}";
        IsAudioPlayingText = IsAudioPlaying ? "🎵 是" : "否";
        CurrentSessionText = $"{_activityManager.CurrentSessionSeconds / 60}分{_activityManager.CurrentSessionSeconds % 60}秒";
        
        // ===== Limit 3.0: 主观校准调试 =====
        SensitivityBiasText = $"{fatigue.SensitivityBias:P0}";
        CareModeText = fatigue.IsCareMode ? "💜 开启" : "关闭";
        PassiveConsumptionText = _activityManager.IsPassiveConsumption ? "🎬 是" : "否";
        IsFullscreenText = _activityManager.IsFullscreen ? "📺 是" : "否";
        
        // 恢复时间估算
        double recoveryMinutes = fatigue.EstimateRecoveryTime(20);
        RecoveryEstimate = recoveryMinutes <= 0 ? "无需休息" : $"约 {recoveryMinutes:F1} 分钟";
        
        // ===== 智能模式：基于疲劳度的智能休息提醒 =====
        // 空闲时暂停倒计时（不重置），活跃时继续倒计时
        if (state == UserActivityState.Idle || state == UserActivityState.Away)
        {
            // 用户正在休息，暂停倒计时（不减少）
            StatusText = "休息中...";
        }
        else if (state == UserActivityState.Active || state == UserActivityState.MediaMode)
        {
            _secondsToNextBreak--;
            StatusText = "正在监测中";
        }
        
        // 更新精确倒计时显示
        NextBreakMinutes = Math.Max(0, _secondsToNextBreak / 60);
        NextBreakSeconds = Math.Max(0, _secondsToNextBreak % 60);
        NextBreakPrecise = $"{NextBreakMinutes}:{NextBreakSeconds:D2}";
        
        // ===== 推荐休息时间（智能模式用）=====
        RecommendedBreakTime = fatigue.GetRecommendedBreakText();
        
        // ===== 休息建议（基于疲劳度）=====
        if (fatigue.FatigueValue >= 80)
        {
            BreakSuggestion = "⚠️ 强烈建议立即休息！";
            StatusText = "⚠️ 疲劳度过高，请休息！";
        }
        else if (fatigue.FatigueValue >= 60)
        {
            BreakSuggestion = $"🔔 {RecommendedBreakTime}";
        }
        else if (fatigue.FatigueValue >= 40)
        {
            BreakSuggestion = $"💡 {RecommendedBreakTime}";
        }
        else
        {
            BreakSuggestion = "✅ 状态良好，继续工作";
        }
        
        // 时间倒计时也到了
        if (_secondsToNextBreak <= 0 && (state == UserActivityState.Active || state == UserActivityState.MediaMode))
        {
            StatusText = "⏰ 定时提醒：该休息了！";
            BreakSuggestion = "⏰ 已工作15分钟，建议休息";
            _secondsToNextBreak = 15 * 60;
        }
        
        // 更新状态文本
        CurrentAppText = state switch
        {
            UserActivityState.Active => IsAudioPlaying 
                ? "🎧 正在工作中（有音频播放）" 
                : "⌨️ 正在工作中...",
            UserActivityState.MediaMode => "🎬 媒体模式（看视频/听音乐）",
            UserActivityState.Idle => $"💤 空闲中，疲劳正在恢复... ({_activityManager.InputMonitor.IdleSeconds:F0}秒)",
            UserActivityState.Away => "🚶 用户已离开",
            _ => "未知状态"
        };
        
        // ===== 疲劳快照保存逻辑（用于图表显示）=====
        _secondsSinceLastSnapshot++;
        // 使用图表间隔设置（分钟转秒）
        var snapshotInterval = _settingsService.Settings.FatigueChartIntervalMinutes * 60;
        
        if (_secondsSinceLastSnapshot >= snapshotInterval)
        {
            _secondsSinceLastSnapshot = 0;
            SaveFatigueSnapshotAsync();
        }
        
        // 每隔 DashboardRefreshInterval 秒更新一次数据库统计
        var refreshInterval = _settingsService.Settings.DashboardRefreshIntervalSeconds;
        if (DateTime.Now.Second % refreshInterval == 0)
        {
            UpdateDatabaseStatsAsync();
        }
    }

    private async void LoadInitialDataAsync()
    {
        try
        {
            // 加载应用使用记录
            var records = await _databaseService.GetUsageForDateAsync(DateTime.Today);
            int totalSeconds = records.Sum(r => r.DurationSeconds);
            
            // 设置初始值
            _activityManager.SetInitialTodayActiveSeconds(totalSeconds);
            
            // 更新列表
            UpdateAppUsageList(records);
            
            // ===== 加载疲劳快照并智能恢复 =====
            var latestSnapshot = await _databaseService.GetLatestFatigueSnapshotAsync();
            if (latestSnapshot != null)
            {
                // 判断是否为今天的记录
                if (latestSnapshot.Date == DateTime.Today)
                {
                    // 同一天，恢复疲劳值
                    _activityManager.FatigueEngine.SetFatigue(latestSnapshot.FatigueValue);
                    FatigueValue = (int)Math.Round(latestSnapshot.FatigueValue);
                    Debug.WriteLine($"[LoadInitial] 恢复今日疲劳值: {latestSnapshot.FatigueValue:F2}%");
                }
                else
                {
                    // 跨天，疲劳值归零
                    Debug.WriteLine($"[LoadInitial] 跨天重置，上次记录: {latestSnapshot.Date:yyyy-MM-dd}");
                }
            }
            
            // 加载今日疲劳趋势数据并填充到图表
            var todaySnapshots = await _databaseService.GetFatigueSnapshotsAsync(DateTime.Today);
            
            // 清空并重新填充图表数据
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                _hourlyFatigueData.Clear();
                foreach (var snapshot in todaySnapshots)
                {
                    var hour = snapshot.RecordedAt.Hour;
                    var minuteFraction = snapshot.RecordedAt.Minute / 60.0;
                    var hourPosition = hour + minuteFraction;
                    
                    _hourlyFatigueData.Add(new ObservablePoint(hourPosition, snapshot.FatigueValue));
                }
                
                Debug.WriteLine($"[LoadInitial] 加载了 {todaySnapshots.Count} 个疲劳快照到图表");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading initial data: {ex.Message}");
        }
    }

    private async void UpdateDatabaseStatsAsync()
    {
        try
        {
            var records = await _databaseService.GetTopUsageAsync(DateTime.Today, 10);
            UpdateAppUsageList(records);
        }
        catch (Exception ex)
        {
             Debug.WriteLine($"Error updating stats: {ex.Message}");
        }
    }

    private void UpdateAppUsageList(List<EyeGuard.Core.Entities.UsageRecord> records)
    {
        // 在 UI 线程更新
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            TodayUsageApps.Clear();
            var total = records.Sum(x => x.DurationSeconds);
            
            // 按应用分组
            var appGroups = records.GroupBy(r => r.AppName);
            
            foreach (var appGroup in appGroups.OrderByDescending(g => g.Sum(x => x.DurationSeconds)))
            {
                var appName = appGroup.Key;
                var appTotalSeconds = appGroup.Sum(x => x.DurationSeconds);
                
                // 检测是否为浏览器
                var isBrowser = EyeGuard.Infrastructure.Services.WebsiteRecognizer.IsBrowserProcess(appName);
                
                var hours = appTotalSeconds / 3600;
                var minutes = (appTotalSeconds % 3600) / 60;
                var durationText = hours > 0 ? $"{hours}小时{minutes}分" : $"{minutes}分";
                
                var appItem = new AppUsageItem
                {
                    Name = isBrowser 
                        ? EyeGuard.Infrastructure.Services.WebsiteRecognizer.GetBrowserDisplayName(appName)
                        : appName,
                    DurationText = durationText,
                    Percentage = total > 0 ? (double)appTotalSeconds / total * 100 : 0,
                    IsBrowser = isBrowser,
                    IconGlyph = EyeGuard.UI.Services.IconMapper.GetAppIcon(appName)
                };
                
                // 如果是浏览器，添加网站子项
                if (isBrowser)
                {
                    // 按网站分组
                    var websiteGroups = appGroup.GroupBy(r => 
                        !string.IsNullOrEmpty(r.WebsiteName) ? r.WebsiteName : "未识别");
                    
                    foreach (var websiteGroup in websiteGroups.OrderByDescending(g => g.Sum(x => x.DurationSeconds)))
                    {
                        var websiteName = websiteGroup.Key;
                        var websiteSeconds = websiteGroup.Sum(x => x.DurationSeconds);
                        
                        var wHours = websiteSeconds / 3600;
                        var wMinutes = (websiteSeconds % 3600) / 60;
                        var wDurationText = wHours > 0 ? $"{wHours}小时{wMinutes}分" : $"{wMinutes}分";
                        
                        var websiteItem = new AppUsageItem
                        {
                            Name = websiteName,
                            DurationText = wDurationText,
                            Percentage = appTotalSeconds > 0 ? (double)websiteSeconds / appTotalSeconds * 100 : 0,
                            WebsiteName = websiteName,
                            IconGlyph = EyeGuard.UI.Services.IconMapper.GetWebsiteIcon(websiteName),
                            IsExpanded = false
                        };
                        
                        // 如果是"未识别"，添加具体页面标题子项
                        if (websiteName == "未识别")
                        {
                            foreach (var pageRecord in websiteGroup.Where(r => !string.IsNullOrEmpty(r.PageTitle)))
                            {
                                var pageSeconds = pageRecord.DurationSeconds;
                                var pH = pageSeconds / 3600;
                                var pM = (pageSeconds % 3600) / 60;
                                var pDuration = pH > 0 ? $"{pH}小时{pM}分" : $"{pM}分";
                                
                                websiteItem.Children.Add(new AppUsageItem
                                {
                                    Name = pageRecord.PageTitle ?? "未知页面",
                                    DurationText = pDuration,
                                    Percentage = websiteSeconds > 0 ? (double)pageSeconds / websiteSeconds * 100 : 0,
                                    IconGlyph = "\uE8A5"
                                });
                            }
                        }
                        
                        appItem.Children.Add(websiteItem);
                    }
                }
                
                
                TodayUsageApps.Add(appItem);
            }
            
            // 通知DisplayedApps和HasMoreApps更新
            OnPropertyChanged(nameof(DisplayedApps));
            OnPropertyChanged(nameof(HasMoreApps));
        });
    }
    
    [RelayCommand]
    private void ToggleShowAllApps()
    {
        ShowAllApps = !ShowAllApps;
        OnPropertyChanged(nameof(DisplayedApps));
        OnPropertyChanged(nameof(HasMoreApps));
    }
    
    /// <summary>
    /// 异步保存疲劳快照到数据库
    /// </summary>
    private async void SaveFatigueSnapshotAsync()
    {
        try
        {
            var fatigueValue = _activityManager.FatigueEngine.FatigueValue;
            await _databaseService.SaveFatigueSnapshotAsync(fatigueValue);
            
            // 同步更新图表
            var now = DateTime.Now;
            var hour = now.Hour;
            var minuteFraction = now.Minute / 60.0;
            var hourPosition = hour + minuteFraction;
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                _hourlyFatigueData.Add(new ObservablePoint(hourPosition, fatigueValue));
            });
            
            Debug.WriteLine($"[Snapshot] 保存疲劳快照: {fatigueValue:F2}% 到数据库和图表");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving fatigue snapshot: {ex.Message}");
        }
    }
    
    // ===== Limit 2.0: 休息任务事件处理 =====
    
    private void OnBreakTaskGenerated(object? sender, BreakTaskRecord task)
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            HasBreakTask = true;
            BreakTaskName = BreakTaskService.GetTaskTypeName(task.TaskType);
            BreakTaskDescription = BreakTaskService.GetTaskTypeDescription(task.TaskType);
            BreakTaskDuration = task.DurationSeconds;
            BreakTaskReason = task.TriggerReason;
            
            Debug.WriteLine($"[BreakTask] 生成任务: {BreakTaskName}, 原因: {BreakTaskReason}");
        });
    }
    
    private void OnBreakTaskCompleted(object? sender, BreakTaskRecord task)
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            HasBreakTask = false;
            BreakTaskName = "";
            BreakTaskDescription = "";
            BreakTaskDuration = 0;
            BreakTaskReason = "";
            
            Debug.WriteLine($"[BreakTask] 任务完成: {task.Result}, 恢复加成: {task.RecoveryCredit:F1}");
        });
    }
    
    /// <summary>
    /// 完成休息任务命令 - 用户自主标记已完成（信任用户）
    /// </summary>
    [RelayCommand]
    private void CompleteBreakTask()
    {
        var currentTask = _breakTaskService.CurrentTask;
        if (currentTask == null) return;
        
        var recoveryCredit = _breakTaskService.SettleTask(currentTask, BreakTaskResult.Completed);
        
        Debug.WriteLine($"[BreakTask] 用户完成任务，恢复值: -{recoveryCredit:F1}%");
    }
    
    /// <summary>
    /// 跳过休息任务命令
    /// </summary>
    [RelayCommand]
    private void SkipBreakTask()
    {
        var currentTask = _breakTaskService.CurrentTask;
        if (currentTask == null) return;
        
        _breakTaskService.SettleTask(currentTask, BreakTaskResult.Skipped);
    }
}
