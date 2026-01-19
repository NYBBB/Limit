using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using CommunityToolkit.Mvvm.Input;
using EyeGuard.UI.ViewModels;

namespace EyeGuard.UI;

/// <summary>
/// EyeGuard 主窗口。
/// 负责管理导航和页面切换。
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;
    
    public RelayCommand ShowWindowCommand { get; }

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化命令
        ShowWindowCommand = new RelayCommand(() => this.Activate());
        
        // 设置窗口标题
        Title = "EyeGuard";
        
        // 获取 AppWindow 并设置窗口大小
        SetupWindow();
        
        // 尝试启用 Mica 背景材质
        TrySetMicaBackdrop();
        
        // 默认导航到仪表盘页面
        ContentFrame.Navigate(typeof(Views.DashboardPage));
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
        Application.Current.Exit();
    }
}
