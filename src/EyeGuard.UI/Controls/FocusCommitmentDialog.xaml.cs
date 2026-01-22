using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 专注承诺设定盘 - Phase 9
/// 允许用户设定专注时间和任务
/// </summary>
public sealed partial class FocusCommitmentDialog : ContentDialog
{
    // 历史任务记录（简单内存存储，后续可持久化）
    private static readonly List<string> _taskHistory = new()
    {
        "写作业", "完成报告", "代码开发", "阅读学习", "会议准备"
    };

    public FocusCommitmentDialog()
    {
        this.InitializeComponent();
        
        // 设置历史任务建议
        TaskInput.ItemsSource = _taskHistory;
        TaskInput.TextChanged += OnTaskInputChanged;
    }

    /// <summary>
    /// 选定的专注时间（分钟）
    /// </summary>
    public int SelectedMinutes { get; private set; } = 30;

    /// <summary>
    /// 选定的任务名称
    /// </summary>
    public string SelectedTaskName { get; private set; } = "";

    /// <summary>
    /// 是否确认开始
    /// </summary>
    public bool IsConfirmed { get; private set; } = false;

    private void OnSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (SliderValueText != null)
        {
            int value = (int)e.NewValue;
            SliderValueText.Text = $"{value}分钟";
            
            // 取消预设按钮选择（仅当滑块值不匹配预设时）
            if (value != 15 && value != 30 && value != 45 && value != 60 && value != 90)
            {
                Time15.IsChecked = false;
                Time30.IsChecked = false;
                Time45.IsChecked = false;
                Time60.IsChecked = false;
                Time90.IsChecked = false;
            }
        }
    }
    
    private void OnPresetTimeChecked(object sender, RoutedEventArgs e)
    {
        // 初始化时控件可能还未加载，需要空检查
        if (CustomTimeSlider == null || SliderValueText == null) return;
        
        if (sender is RadioButton radio && radio.Tag is string tagStr && int.TryParse(tagStr, out int minutes))
        {
            CustomTimeSlider.Value = minutes;
            SliderValueText.Text = $"{minutes}分钟";
        }
    }

    private void OnTaskInputChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suggestions = new List<string>();
            var text = sender.Text.ToLower();
            
            foreach (var task in _taskHistory)
            {
                if (task.ToLower().Contains(text))
                {
                    suggestions.Add(task);
                }
            }
            
            sender.ItemsSource = suggestions;
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 获取选定时间
        SelectedMinutes = (int)CustomTimeSlider.Value;
        
        // 检查预设按钮
        if (Time15.IsChecked == true) SelectedMinutes = 15;
        else if (Time30.IsChecked == true) SelectedMinutes = 30;
        else if (Time45.IsChecked == true) SelectedMinutes = 45;
        else if (Time60.IsChecked == true) SelectedMinutes = 60;
        else if (Time90.IsChecked == true) SelectedMinutes = 90;
        
        // 获取任务名称
        SelectedTaskName = string.IsNullOrWhiteSpace(TaskInput.Text) ? "专注中..." : TaskInput.Text;
        
        // 添加到历史记录
        if (!string.IsNullOrWhiteSpace(TaskInput.Text) && !_taskHistory.Contains(TaskInput.Text))
        {
            _taskHistory.Insert(0, TaskInput.Text);
            if (_taskHistory.Count > 10) _taskHistory.RemoveAt(_taskHistory.Count - 1);
        }
        
        IsConfirmed = true;
    }
}
