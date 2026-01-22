using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EyeGuard.UI.ViewModels;
using EyeGuard.UI.Controls;

namespace EyeGuard.UI.Views;

/// <summary>
/// DashboardPage3 - Limit 3.0 Bento Grid 布局
/// </summary>
public sealed partial class DashboardPage3 : Page
{
    public DashboardViewModel3 ViewModel { get; }

    public DashboardPage3()
    {
        this.InitializeComponent();
        ViewModel = DashboardViewModel3.Instance;
        
        // Phase 9: 订阅 ContextCard 专注事件
        this.Loaded += OnPageLoaded;
    }
    
    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // 订阅 ContextCard 事件
        if (ContextCardControl != null)
        {
            ContextCardControl.FocusCommitmentRequested += OnFocusCommitmentRequested;
            ContextCardControl.StopFocusRequested += OnStopFocusRequested;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.InitializeTimer(DispatcherQueue);
    }

    // ===== 操作按钮事件 =====

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleMonitoring();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetAll();
    }

    // ===== 校准菜单事件 =====

    private void CalibrateAsTired_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CalibrateAsTired();
    }

    private void CalibrateAsFresh_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CalibrateAsFresh();
    }

    private void CalibrateAfterRest_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CalibrateAfterRest();
    }

    // ===== Zone B: Focusing 切换 =====

    private void OnFocusingChanged(object sender, bool isFocusing)
    {
        ViewModel.SetFocusingMode(isFocusing);
    }
    
    // ===== Phase 9: 专注承诺事件处理 =====
    
    private async void OnFocusCommitmentRequested(object? sender, System.EventArgs e)
    {
        var dialog = new FocusCommitmentDialog
        {
            XamlRoot = this.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.IsConfirmed)
        {
            // 启动专注模式
            ViewModel.StartFocusCommitment(dialog.SelectedMinutes, dialog.SelectedTaskName);
            
            // 更新 ContextCard 状态
            if (ContextCardControl != null)
            {
                ContextCardControl.IsFocusing = true;
            }
        }
    }
    
    private void OnStopFocusRequested(object? sender, System.EventArgs e)
    {
        ViewModel.StopFocusCommitment();
    }
    
    // ===== Debug 面板 =====
    
    private void DebugButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleDebugPanel();
    }
}
