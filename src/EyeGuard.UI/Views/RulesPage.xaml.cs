using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EyeGuard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EyeGuard.UI.Views;

/// <summary>
/// 规则设置页面。
/// 合并了原规则页面和准确性页面，支持简单/智能模式切换。
/// </summary>
public sealed partial class RulesPage : Page
{
    private readonly SettingsService _settingsService;
    private readonly ClusterService _clusterService;

    public RulesPage()
    {
        InitializeComponent();
        _settingsService = SettingsService.Instance;
        _clusterService = App.Services.GetRequiredService<ClusterService>();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        
        // 智能模式设置
        SoftReminderSlider.Value = settings.SoftReminderThreshold;
        ForceBreakSlider.Value = settings.ForceBreakThreshold;
        IdleThresholdBox.Value = settings.IdleThresholdSeconds;
        KeyboardToggle.IsOn = settings.EnableKeyboardMonitor;
        AudioToggle.IsOn = settings.EnableAudioMonitor;
        
        // 更新滑块文本
        SoftReminderText.Text = $"{settings.SoftReminderThreshold}%";
        ForceBreakText.Text = $"{settings.ForceBreakThreshold}%";
        
        // 绑定滑块值变化
        SoftReminderSlider.ValueChanged += (s, e) => SoftReminderText.Text = $"{(int)e.NewValue}%";
        ForceBreakSlider.ValueChanged += (s, e) => ForceBreakText.Text = $"{(int)e.NewValue}%";
        
        // 提醒设置
        EnableRemindersToggle.IsOn = settings.EnableReminders;
        ReminderTypeCombo.SelectedIndex = settings.ReminderType;
        
        // 高级设置
        TrayIconToggle.IsOn = settings.ShowTrayIcon;
        AutoStartToggle.IsOn = settings.AutoStartOnBoot;
        SnapshotIntervalSlider.Value = settings.FatigueSnapshotIntervalSeconds;
        SnapshotIntervalText.Text = $"{settings.FatigueSnapshotIntervalSeconds}秒";
        ChartIntervalSlider.Value = settings.FatigueChartIntervalMinutes;
        ChartIntervalText.Text = $"{settings.FatigueChartIntervalMinutes}分钟";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.UpdateAndSave(settings =>
        {
            // 智能模式设置（永久启用）
            settings.IsSmartMode = true;
            
            // 智能模式设置
            settings.SoftReminderThreshold = (int)SoftReminderSlider.Value;
            settings.ForceBreakThreshold = (int)ForceBreakSlider.Value;
            settings.IdleThresholdSeconds = (int)IdleThresholdBox.Value;
            settings.EnableKeyboardMonitor = KeyboardToggle.IsOn;
            settings.EnableAudioMonitor = AudioToggle.IsOn;
            
            // 提醒设置
            settings.EnableReminders = EnableRemindersToggle.IsOn;
            settings.ReminderType = ReminderTypeCombo.SelectedIndex;
            
            // 高级设置
            settings.ShowTrayIcon = TrayIconToggle.IsOn;
            settings.AutoStartOnBoot = AutoStartToggle.IsOn;
            settings.FatigueSnapshotIntervalSeconds = (int)SnapshotIntervalSlider.Value;
            settings.FatigueChartIntervalMinutes = (int)ChartIntervalSlider.Value;
        });

        SaveStatusText.Text = "✓ 设置已保存";
        System.Diagnostics.Debug.WriteLine($"[RulesPage] 设置已保存: IdleThreshold={_settingsService.Settings.IdleThresholdSeconds}s");
        
        // 3秒后清除提示
        await Task.Delay(3000);
        SaveStatusText.Text = "";
    }
    
    private async void ResetClusters_Click(object sender, RoutedEventArgs e)
    {
        // 显示确认对话框
        var dialog = new ContentDialog
        {
            Title = "恢复默认分类",
            Content = "确定要恢复所有应用分类为默认设置吗？此操作不可撤销。",
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _clusterService.ResetToDefaultAsync();
            
            // 刷新 ClusterEditor 显示
            if (ClusterEditor != null)
            {
                await ClusterEditor.RefreshAsync();
            }
        }
    }
    
    private async void ResetAllSettings_Click(object sender, RoutedEventArgs e)
    {
        // 显示确认对话框
        var dialog = new ContentDialog
        {
            Title = "恢复默认设置",
            Content = "确定要恢复所有规则设置为默认值吗？此操作不可撤销。",
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // 使用 UserSettings 的 ResetToDefault 方法
            _settingsService.Settings.ResetToDefault();
            _settingsService.Save();
            
            // 重新加载 UI
            LoadSettings();
            
            SaveStatusText.Text = "✓ 已恢复默认设置";
            await Task.Delay(3000);
            SaveStatusText.Text = "";
        }
    }
    
    // ===== 高级设置事件处理 =====
    
    private void SnapshotIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (SnapshotIntervalText != null)
        {
            SnapshotIntervalText.Text = $"{(int)e.NewValue}秒";
        }
    }
    
    private void ChartIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ChartIntervalText != null)
        {
            ChartIntervalText.Text = $"{(int)e.NewValue}分钟";
        }
    }
}
