using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using CommunityToolkit.Mvvm.Input;
using EyeGuard.UI.ViewModels;
using EyeGuard.UI.Services;
using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection; // 修复泛型服务获取

namespace EyeGuard.UI;

/// <summary>
/// EyeGuard 主窗口。
/// 负责管理导航和页面切换。
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;
    private readonly TrayIconService _trayIconService;
    private readonly ToastNotificationService _toastService;
    private bool _reallyClose = false;

    public RelayCommand ShowWindowCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        // 初始化命令
        ShowWindowCommand = new RelayCommand(() => this.Activate());

        // 设置窗口标题
        Title = "Limit";

        // 获取 AppWindow 并设置窗口大小
        SetupWindow();

        // 尝试启用 Mica 背景材质
        TrySetMicaBackdrop();

        // Phase C: 初始化托盘图标
        _trayIconService = new TrayIconService();
        _trayIconService.ShowRequested += (s, e) => this.Activate();
        _trayIconService.ExitRequested += (s, e) => ExitApplication();
        _trayIconService.StartMonitoringRequested += (s, e) => StartMonitoring_Click(this, new RoutedEventArgs());
        _trayIconService.Initialize();

        // Phase C: 初始化 Toast 通知
        _toastService = new ToastNotificationService();
        _toastService.Initialize();

        // Phase 5: 启动托盘状态更新定时器
        StartTrayUpdateTimer();

        // 监听窗口关闭事件
        this.Closed += MainWindow_Closed;

        // 默认导航到 WebView2 仪表盘页面 (Limit 3.0 混合架构)
        ContentFrame.Navigate(typeof(Views.WebViewPage), "dashboard");

        // Limit 3.0 Beta 2: 监听窗口显示/隐藏，优化后台性能
        if (_appWindow != null)
        {
            _appWindow.Changed += OnAppWindowChanged;
        }

        Debug.WriteLine("[MainWindow] Initialized with tray and toast services");
    }

    /// <summary>
    /// Beta 2: 监听窗口状态变化（最小化/恢复）
    /// </summary>
    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidVisibilityChange)
        {
            var dashboardVM = DashboardViewModel3.Instance;
            dashboardVM.OnWindowVisibilityChanged(sender.IsVisible);
            Debug.WriteLine($"[MainWindow] Window visibility changed: {sender.IsVisible}");
        }
    }

    /// <summary>
    /// Phase 5: 启动托盘状态更新定时器（每秒轮询疲劳值 & 驱动核心逻辑）
    /// </summary>
    private void StartTrayUpdateTimer()
    {
        try
        {
            var timer = DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => OnMainLoopTick();
            timer.Start();

            Debug.WriteLine("[MainWindow] Main loop timer started");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] Failed to start main loop timer: {ex.Message}");
        }
    }

    /// <summary>
    /// 主循环 Tick (每秒执行)
    /// </summary>
    private void OnMainLoopTick()
    {
        try
        {
            // 1. 获取服务 (使用 GetRequiredService 泛型扩展方法)
            var userActivityManager = App.Services.GetRequiredService<EyeGuard.Infrastructure.Services.UserActivityManager>();
            var bridgeService = App.Services.GetRequiredService<EyeGuard.UI.Bridge.BridgeService>();
            var fatigueEngine = App.Services.GetRequiredService<EyeGuard.Infrastructure.Services.FatigueEngine>();

            // 2. 驱动核心逻辑
            if (userActivityManager != null)
            {
                userActivityManager.Tick();
            }

            // 3. 推送数据到前端 (Bridge)
            if (bridgeService != null)
            {
                // 注意：这里每秒全量推送可能有点重，但对于 Countdown 需要 1s 精度
                // 后续可以优化为只推送 diff 或特定消息
                bridgeService.SendAllUpdates();
            }

            // 4. 更新托盘状态
            if (fatigueEngine != null)
            {
                var fatigue = fatigueEngine.FatigueValue;
                var statusEmoji = fatigue switch
                {
                    < 40 => "😊",
                    < 60 => "😐",
                    < 80 => "😓",
                    _ => "🔥"
                };
                _trayIconService.UpdateTooltip($"Limit {statusEmoji} 疲劳: {fatigue:F0}%");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] MainLoop error: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置窗口大小和最小尺寸。
    /// </summary>
    private void SetupWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            // 设置窗口大小 (1920x1080)
            _appWindow.Resize(new Windows.Graphics.SizeInt32(1920, 1080));

            // 窗口居中显示
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                var centerX = (displayArea.WorkArea.Width - 1920) / 2;
                var centerY = (displayArea.WorkArea.Height - 1080) / 2;
                _appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
            }
        }
    }

    /// <summary>
    /// 尝试设置 Mica 背景材质。
    /// </summary>
    private void TrySetMicaBackdrop()
    {
        if (MicaController.IsSupported())
        {
            // 使用系统背景材质
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            // 降级使用亚克力效果
            SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        }
    }

    // ===== 托盘图标事件处理 =====

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        this.Activate();
    }

    private void StartMonitoring_Click(object sender, RoutedEventArgs e)
    {
        // Limit 3.0 Switched to DashboardViewModel3
        var vm = DashboardViewModel3.Instance;
        if (!vm.IsMonitoring)
        {
            vm.ToggleMonitoring();
        }
    }

    private void StopMonitoring_Click(object sender, RoutedEventArgs e)
    {
        // Limit 3.0 Switched to DashboardViewModel3
        var vm = DashboardViewModel3.Instance;
        if (vm.IsMonitoring)
        {
            vm.ToggleMonitoring();
        }
    }


    private void ExitApp_Click(object sender, RoutedEventArgs e)
    {
        ExitApplication();
    }

    /// <summary>
    /// 真正退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        _reallyClose = true;
        _trayIconService?.Dispose();
        _toastService?.Uninitialize();
        Application.Current.Exit();
    }

    /// <summary>
    /// 窗口关闭事件 - 最小化到托盘而非真正关闭
    /// </summary>
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_reallyClose)
        {
            args.Handled = true;
            this.HideToTray();
            _toastService?.ShowInterventionNotification(0, "Limit 已最小化到托盘，继续在后台监测");
            Debug.WriteLine("[MainWindow] Minimized to tray");
        }
    }

    /// <summary>
    /// 显示窗口（从托盘恢复）
    /// </summary>
    public void ShowFromTray()
    {
        this.Activate();
    }

    /// <summary>
    /// 隐藏窗口（最小化到托盘）
    /// </summary>
    public void HideToTray()
    {
        // WinUI 3 没有 Hide 方法,用最小化替代
        if (_appWindow != null)

            _appWindow.Hide();

    }

    /// <summary>
    /// 获取 Toast 服务（供其他组件使用）
    /// </summary>
    public ToastNotificationService GetToastService() => _toastService;
}
