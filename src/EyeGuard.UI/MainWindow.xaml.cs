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
        
        // 监听窗口关闭事件
        this.Closed += MainWindow_Closed;
        
        // 默认导航到仪表盘页面
        ContentFrame.Navigate(typeof(Views.DashboardPage));
        
        Debug.WriteLine("[MainWindow] Initialized with tray and toast services");
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

    /// <summary>
    /// 导航选项变化时触发。
    /// </summary>
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var pageTag = selectedItem.Tag?.ToString();
            NavigateToPage(pageTag);
        }
    }

    /// <summary>
    /// 根据页面标签导航到对应页面。
    /// </summary>
    private void NavigateToPage(string? pageTag)
    {
        var pageType = pageTag switch
        {
            "Dashboard" => typeof(Views.DashboardPage),
            "Analytics" => typeof(Views.AnalyticsPage),
            "Rules" => typeof(Views.RulesPage),
            "Settings" => typeof(Views.SettingsPage),
            _ => typeof(Views.DashboardPage)
        };

        ContentFrame.Navigate(pageType);
    }

    // ===== 托盘图标事件处理 =====

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        this.Activate();
    }

    private void StartMonitoring_Click(object sender, RoutedEventArgs e)
    {
        var vm = DashboardViewModel.Instance;
        if (!vm.IsMonitoring)
        {
            vm.StartSimulationCommand.Execute(null);
        }
    }

    private void StopMonitoring_Click(object sender, RoutedEventArgs e)
    {
        var vm = DashboardViewModel.Instance;
        if (vm.IsMonitoring)
        {
            vm.StartSimulationCommand.Execute(null);
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
        {
            _appWindow.Hide();
        }
    }
    
    /// <summary>
    /// 获取 Toast 服务（供其他组件使用）
    /// </summary>
    public ToastNotificationService GetToastService() => _toastService;
}
