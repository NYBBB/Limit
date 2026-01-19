using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EyeGuard.Infrastructure.Services;
using System.Diagnostics;

namespace EyeGuard.UI.Views;

/// <summary>
/// 设置页面 - 通用应用设置。
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly SettingsService _settingsService;
    
    public SettingsPage()
    {
        InitializeComponent();
        _settingsService = SettingsService.Instance;
        
        // 页面加载时读取设置
        Loaded += SettingsPage_Loaded;
    }
    
    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 加载数据持久化设置
        var settings = _settingsService.Settings;
        
        SnapshotIntervalSlider.Value = settings.FatigueSnapshotIntervalSeconds;
        ChartIntervalSlider.Value = settings.FatigueChartIntervalMinutes;
        RefreshIntervalSlider.Value = settings.DashboardRefreshIntervalSeconds;
        
        Debug.WriteLine("[SettingsPage] 已加载设置");
    }
    
    private void SnapshotIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        int value = (int)e.NewValue;
        if (SnapshotIntervalText != null)
        {
            SnapshotIntervalText.Text = $"{value}秒";
        }
    }
    
    private void ChartIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        int value = (int)e.NewValue;
        if (ChartIntervalText != null)
        {
            ChartIntervalText.Text = $"{value}分钟";
        }
    }
    
    private void RefreshIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        int value = (int)e.NewValue;
        if (RefreshIntervalText != null)
        {
            RefreshIntervalText.Text = $"{value}秒";
        }
    }
    
    private async void SavePersistenceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settingsService.UpdateAndSave(settings =>
            {
                settings.FatigueSnapshotIntervalSeconds = (int)SnapshotIntervalSlider.Value;
                settings.FatigueChartIntervalMinutes = (int)ChartIntervalSlider.Value;
                settings.DashboardRefreshIntervalSeconds = (int)RefreshIntervalSlider.Value;
            });
            
            // 显示保存成功提示
            SaveStatusText.Text = "✓ 已保存";
            await System.Threading.Tasks.Task.Delay(2000);
            SaveStatusText.Text = "";
            
            Debug.WriteLine("[SettingsPage] 数据持久化设置已保存");
        }
        catch (System.Exception ex)
        {
            SaveStatusText.Text = "✗ 保存失败";
            SaveStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Red);
            Debug.WriteLine($"Error saving persistence settings: {ex.Message}");
        }
    }
}
