using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using EyeGuard.Infrastructure.Services;

namespace EyeGuard.UI.Views;

/// <summary>
/// 规则设置页面。
/// 合并了原规则页面和准确性页面，支持简单/智能模式切换。
/// </summary>
public sealed partial class RulesPage : Page
{
    private readonly SettingsService _settingsService;

    public RulesPage()
    {
        InitializeComponent();
        _settingsService = SettingsService.Instance;
        LoadSettings();
        UpdateModeVisibility();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        
        // 模式选择
        SmartModeRadio.IsChecked = settings.IsSmartMode;
        SimpleModeRadio.IsChecked = !settings.IsSmartMode;
        
        // 简单模式设置
        MicroBreakIntervalBox.Value = settings.MicroBreakIntervalMinutes;
        MicroBreakDurationBox.Value = settings.MicroBreakDurationSeconds;
        LongBreakIntervalBox.Value = settings.LongBreakIntervalMinutes;
        LongBreakDurationBox.Value = settings.LongBreakDurationMinutes;
        
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
    }

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        UpdateModeVisibility();
    }

    private void UpdateModeVisibility()
    {
        if (SimpleModePanel == null || SmartModePanel == null) return;
        
        bool isSmartMode = SmartModeRadio?.IsChecked == true;
        
        SimpleModePanel.Visibility = isSmartMode ? Visibility.Collapsed : Visibility.Visible;
        SmartModePanel.Visibility = isSmartMode ? Visibility.Visible : Visibility.Collapsed;
        
        // 更新模式描述
        if (ModeDescriptionText != null)
        {
            ModeDescriptionText.Text = isSmartMode
                ? "智能模式通过检测键鼠活动、音频播放等多种方式自动判断您是否在使用电脑，并动态计算疲劳值。推荐使用。"
                : "简单模式使用固定的时间间隔提醒休息，不进行智能检测。适合希望简单设置的用户。";
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.UpdateAndSave(settings =>
        {
            // 模式
            settings.IsSmartMode = SmartModeRadio?.IsChecked == true;
            
            // 简单模式设置
            settings.MicroBreakIntervalMinutes = (int)MicroBreakIntervalBox.Value;
            settings.MicroBreakDurationSeconds = (int)MicroBreakDurationBox.Value;
            settings.LongBreakIntervalMinutes = (int)LongBreakIntervalBox.Value;
            settings.LongBreakDurationMinutes = (int)LongBreakDurationBox.Value;
            
            // 智能模式设置
            settings.SoftReminderThreshold = (int)SoftReminderSlider.Value;
            settings.ForceBreakThreshold = (int)ForceBreakSlider.Value;
            settings.IdleThresholdSeconds = (int)IdleThresholdBox.Value;
            settings.EnableKeyboardMonitor = KeyboardToggle.IsOn;
            settings.EnableAudioMonitor = AudioToggle.IsOn;
            
            // 提醒设置
            settings.EnableReminders = EnableRemindersToggle.IsOn;
            settings.ReminderType = ReminderTypeCombo.SelectedIndex;
        });

        SaveStatusText.Text = "✓ 设置已保存";
        
        // 3秒后清除提示
        await Task.Delay(3000);
        SaveStatusText.Text = "";
    }
}
