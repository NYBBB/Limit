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
    private int _secondsToNextBreak = 15 * 60;
    private const int TopAppsCount = 5; // 默认显示前5个应用
    
    // 疲劳快照保存计时器
    private int _secondsSinceLastSnapshot = 0;
    private int _minutesSinceLastChartPoint = 0;
    
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

    // 图表数据 - 24小时时间轴
    public ISeries[] Series { get; set; }
    public ICartesianAxis[] XAxes { get; set; }
    public ICartesianAxis[] YAxes { get; set; }
    private readonly ObservableCollection<ObservablePoint> _hourlyFatigueData;

    private DashboardViewModel()
    {
        // 初始化设置服务
        _settingsService = SettingsService.Instance;
        
        // 获取数据库服务
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        
        // 初始化用户活动管理器
        _activityManager = new UserActivityManager();
        
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
        
        // 初始化24小时数据点
        _hourlyFatigueData = new ObservableCollection<ObservablePoint>();
        for (int hour = 0; hour <= 24; hour++)
        {
            _hourlyFatigueData.Add(new ObservablePoint(hour, null));
        }
        
        Series = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = _hourlyFatigueData,
                Name = "疲劳值",
                Fill = new SolidColorPaint(new SKColor(138, 43, 226, 40)),
                Stroke = new SolidColorPaint(new SKColor(138, 43, 226)) { StrokeThickness = 2 },
                GeometrySize = 6,
                GeometryFill = new SolidColorPaint(new SKColor(138, 43, 226)),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                LineSmoothness = 0.5,
            }
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

        // 更新活动管理器
        _activityManager.Tick();
        
        var fatigue = _activityManager.FatigueEngine;
        var state = _activityManager.CurrentState;
        
        // ===== 同步数据到 UI =====
        FatigueValue = (int)Math.Round(fatigue.FatigueValue);
        FatigueLevel = fatigue.GetFatigueLevel();
        UserState = _activityManager.GetStateDescription();
        IsAudioPlaying = _activityManager.AudioDetector.IsAudioPlaying;
        
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
        
        // 更新图表
        var currentHour = DateTime.Now.Hour;
        var minuteFraction = DateTime.Now.Minute / 60.0;
        var hourPosition = currentHour + minuteFraction;
        _hourlyFatigueData[currentHour] = new ObservablePoint(hourPosition, FatigueValue);
        
        // ===== 疲劳快照保存逻辑 =====
        _secondsSinceLastSnapshot++;
        var snapshotInterval = _settingsService.Settings.FatigueSnapshotIntervalSeconds;
        
        if (_secondsSinceLastSnapshot >= snapshotInterval)
        {
            _secondsSinceLastSnapshot = 0;
            SaveFatigueSnapshotAsync();
        }
        
        // 每隔 ChartIntervalMinutes 分钟记录一个图表点
        if (DateTime.Now.Second == 0)
        {
            _minutesSinceLastChartPoint++;
            var chartInterval = _settingsService.Settings.FatigueChartIntervalMinutes;
            
            if (_minutesSinceLastChartPoint >= chartInterval)
            {
                _minutesSinceLastChartPoint = 0;
                // 图表点已经在上面更新了，这里只是记录日志
                Debug.WriteLine($"[Chart] 记录疲劳趋势点: {FatigueValue}%");
            }
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
            
            // 加载今日疲劳趋势数据
            var todaySnapshots = await _databaseService.GetFatigueSnapshotsAsync(DateTime.Today);
            foreach (var snapshot in todaySnapshots)
            {
                var hour = snapshot.RecordedAt.Hour;
                var minuteFraction = snapshot.RecordedAt.Minute / 60.0;
                var hourPosition = hour + minuteFraction;
                
                // 更新对应小时的数据点
                if (hour < _hourlyFatigueData.Count)
                {
                    _hourlyFatigueData[hour] = new ObservablePoint(hourPosition, snapshot.FatigueValue);
                }
            }
            Debug.WriteLine($"[LoadInitial] 加载了 {todaySnapshots.Count} 个疲劳快照");
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
            await _databaseService.SaveFatigueSnapshotAsync(_activityManager.FatigueEngine.FatigueValue);
            Debug.WriteLine($"[Snapshot] 保存疲劳快照: {_activityManager.FatigueEngine.FatigueValue:F2}%");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving fatigue snapshot: {ex.Message}");
        }
    }
}
