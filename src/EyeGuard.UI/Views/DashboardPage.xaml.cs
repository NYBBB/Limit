using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using EyeGuard.UI.ViewModels;
using Windows.UI;

namespace EyeGuard.UI.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = DashboardViewModel.Instance;
        this.InitializeComponent();
        this.Loaded += DashboardPage_Loaded;
        
        // 监听IsMonitoring变化来更新状态指示器颜色
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }
    
    private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化Timer（必须在UI线程上）
        ViewModel.InitializeTimer(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        
        // 初始化状态指示器颜色
        UpdateStatusIndicatorColor();
    }
    
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsMonitoring))
        {
            UpdateStatusIndicatorColor();
        }
    }
    
    private void UpdateStatusIndicatorColor()
    {
        if (StatusIndicator != null)
        {
            StatusIndicator.Fill = ViewModel.IsMonitoring 
                ? new SolidColorBrush(Color.FromArgb(255, 0, 200, 83))  // 绿色
                : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)); // 灰色
        }
    }
    
    /// <summary>
    /// 切换子项展开/折叠
    /// </summary>
    private void ToggleExpand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is AppUsageItem item)
        {
            item.IsExpanded = !item.IsExpanded;
        }
    }
    
    /// <summary>
    /// 开始/停止监测按钮
    /// </summary>
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StartSimulationCommand.Execute(null);
    }
    
    /// <summary>
    /// 重置按钮
    /// </summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetSimulationCommand.Execute(null);
    }
    
    /// <summary>
    /// 测试遮罩窗口按钮
    /// </summary>
    private void TestOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        // 创建并显示休息提醒窗口
        var overlayWindow = new BreakOverlayWindow
        {
            BreakDurationSeconds = 20,
            FatigueValue = ViewModel.FatigueValue
        };
        
        overlayWindow.BreakCompleted += (s, action) =>
        {
            System.Diagnostics.Debug.WriteLine($"[TestOverlay] User action: {action}");
        };
        
        overlayWindow.Activate();
    }
}
