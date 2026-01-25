using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.DependencyInjection;
using EyeGuard.UI.Bridge;
using EyeGuard.Infrastructure.Services;
using System.Diagnostics;

namespace EyeGuard.UI.Views;

/// <summary>
/// WebView2 承载页面
/// 用于加载 Vue 3 前端应用
/// </summary>
public sealed partial class WebViewPage : Page
{
    private BridgeService? _bridgeService;
    private DispatcherTimer? _updateTimer;
    private string _currentView = "dashboard";

    // ===== 疲劳快照保存相关 =====
    private int _secondsSinceLastSnapshot = 0;
    private int _todaySnapshotCount = 0;

    public WebViewPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 页面导航参数
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // 接收导航参数（指定显示哪个视图）
        if (e.Parameter is string viewName)
        {
            _currentView = viewName;
        }
    }

    /// <summary>
    /// WebView2 加载完成
    /// </summary>
    private async void WebContent_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 确保 WebView2 环境初始化
            await WebContent.EnsureCoreWebView2Async();

            // 配置 WebView2 设置
            ConfigureWebView();

            // 初始化 Bridge 服务
            InitializeBridge();

            // ===== 关键：加载初始数据（恢复疲劳值） =====
            await LoadInitialDataAsync();

            // 加载前端页面
            LoadFrontend();

            // 隐藏加载指示器
            LoadingIndicator.IsActive = false;
            LoadingIndicator.Visibility = Visibility.Collapsed;

            Debug.WriteLine("[WebViewPage] WebView2 初始化完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewPage] WebView2 初始化失败: {ex.Message}");
            ShowErrorMessage(ex.Message);
        }
    }

    /// <summary>
    /// 配置 WebView2 设置
    /// </summary>
    private void ConfigureWebView()
    {
        var settings = WebContent.CoreWebView2.Settings;

        // 开发模式设置
#if DEBUG
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = true;
#else
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
#endif

        // 安全设置
        settings.IsWebMessageEnabled = true;
        settings.IsScriptEnabled = true;
        settings.IsStatusBarEnabled = false;

        // 禁用不需要的功能
        settings.IsZoomControlEnabled = false;
        settings.IsPinchZoomEnabled = false;
    }

    /// <summary>
    /// 初始化 Bridge 服务
    /// </summary>
    private void InitializeBridge()
    {
        _bridgeService = new BridgeService(App.Services);
        _bridgeService.Initialize(WebContent.CoreWebView2);

        // ===== 关键：启动 UserActivityManager（疲劳引擎） =====
        var activityManager = App.Services.GetRequiredService<UserActivityManager>();
        activityManager.Start();
        Debug.WriteLine("[WebViewPage] UserActivityManager 已启动");

        // 注册导航处理
        _bridgeService.Handler?.RegisterHandler(BridgeMessages.Navigate, data =>
        {
            if (data.TryGetProperty("view", out var viewElement))
            {
                var view = viewElement.GetString();
                // 通知前端切换视图
                _currentView = view ?? "dashboard";
            }
        });

        // 启动定时数据更新
        StartUpdateTimer();
    }

    /// <summary>
    /// 加载初始数据（疲劳值恢复、今日使用时间恢复）
    /// </summary>
    private async Task LoadInitialDataAsync()
    {
        try
        {
            var db = App.Services.GetRequiredService<DatabaseService>();
            var activityManager = App.Services.GetRequiredService<UserActivityManager>();
            var fatigueEngine = activityManager.FatigueEngine;

            // ===== 恢复今日疲劳值 =====
            var latestSnapshot = await db.GetLatestFatigueSnapshotAsync();
            if (latestSnapshot != null)
            {
                if (latestSnapshot.Date == DateTime.Today)
                {
                    // 同一天，恢复疲劳值
                    fatigueEngine.SetFatigue(latestSnapshot.FatigueValue);
                    Debug.WriteLine($"[WebViewPage] 恢复今日疲劳值: {latestSnapshot.FatigueValue:F2}%");
                }
                else
                {
                    // 跨天，疲劳值归零（FatigueEngine 默认就是0）
                    Debug.WriteLine($"[WebViewPage] 跨天重置，上次记录: {latestSnapshot.Date:yyyy-MM-dd}");
                }
            }
            else
            {
                Debug.WriteLine("[WebViewPage] 无疲劳快照记录，使用默认值");
            }

            // 统计今日快照数量
            var todaySnapshots = await db.GetFatigueSnapshotsAsync(DateTime.Today);
            _todaySnapshotCount = todaySnapshots.Count;

            // ===== 加载今日使用记录 =====
            var usageRecords = await db.GetUsageForDateAsync(DateTime.Today);
            int totalSeconds = usageRecords.Sum(r => r.DurationSeconds);
            activityManager.SetInitialTodayActiveSeconds(totalSeconds);

            Debug.WriteLine($"[WebViewPage] 初始化完成: 快照数={_todaySnapshotCount}, 使用时间={totalSeconds / 60}分钟");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewPage] LoadInitialData error: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动定时数据更新
    /// </summary>
    private void StartUpdateTimer()
    {
        var settingsService = App.Services.GetRequiredService<SettingsService>();

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _updateTimer.Tick += (s, e) =>
        {
            // ===== 关键：每秒触发疲劳引擎更新 =====
            var activityManager = App.Services.GetRequiredService<UserActivityManager>();
            activityManager.Tick();

            _bridgeService?.SendAllUpdates(); // 发送所有数据更新

            // ===== 疲劳快照保存逻辑（用于图表显示和恢复）=====
            _secondsSinceLastSnapshot++;
            // 使用图表间隔设置（分钟转秒）
            var snapshotInterval = settingsService.Settings.FatigueChartIntervalMinutes * 60;

            if (_secondsSinceLastSnapshot >= snapshotInterval)
            {
                _secondsSinceLastSnapshot = 0;
                SaveFatigueSnapshotAsync();
            }
        };
        _updateTimer.Start();

        // 立即发送一次全量数据
        _bridgeService?.SendAllUpdates();
    }

    /// <summary>
    /// 异步保存疲劳快照到数据库
    /// </summary>
    private async void SaveFatigueSnapshotAsync()
    {
        try
        {
            var db = App.Services.GetRequiredService<DatabaseService>();
            var activityManager = App.Services.GetRequiredService<UserActivityManager>();
            var fatigueValue = activityManager.FatigueEngine.FatigueValue;

            await db.SaveFatigueSnapshotAsync(fatigueValue);

            _todaySnapshotCount++;

            Debug.WriteLine($"[WebViewPage] 保存疲劳快照: {fatigueValue:F2}% (今日第{_todaySnapshotCount}个)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewPage] SaveFatigueSnapshot error: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载前端页面
    /// </summary>
    private void LoadFrontend()
    {
#if DEBUG
        // 开发模式：加载 Vite 开发服务器
        var devUrl = $"http://localhost:5173/?view={_currentView}";
        WebContent.Source = new Uri(devUrl);
        Debug.WriteLine($"[WebViewPage] 加载开发服务器: {devUrl}");
#else
        // 发布模式：加载本地文件
        var localPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "WebRoot",
            "index.html"
        );

        if (File.Exists(localPath))
        {
            WebContent.Source = new Uri(localPath);
            Debug.WriteLine($"[WebViewPage] 加载本地文件: {localPath}");
        }
        else
        {
            ShowErrorMessage("前端资源文件不存在，请确保已构建前端项目。");
        }
#endif
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        LoadingIndicator.IsActive = false;

        // 在页面上显示错误信息
        var errorText = new TextBlock
        {
            Text = $"加载失败: {message}",
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.OrangeRed
            ),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 400
        };

        ((Grid)Content).Children.Add(errorText);
    }

    /// <summary>
    /// 页面导航离开时清理资源
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        // ===== 离开前保存一次疲劳快照 =====
        SaveFatigueSnapshotAsync();

        _updateTimer?.Stop();
        _bridgeService?.Dispose();

        Debug.WriteLine("[WebViewPage] 资源已清理");
    }

    /// <summary>
    /// 刷新前端页面
    /// </summary>
    public void Refresh()
    {
        WebContent.Reload();
    }

    /// <summary>
    /// 发送数据更新到前端
    /// </summary>
    public void SendUpdate()
    {
        _bridgeService?.SendAllUpdates();
    }
}
