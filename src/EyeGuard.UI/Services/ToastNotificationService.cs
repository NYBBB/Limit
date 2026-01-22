namespace EyeGuard.UI.Services;

using System;
using System.Diagnostics;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

/// <summary>
/// Toast 通知服务 - 发送 Windows 原生通知
/// </summary>
public class ToastNotificationService
{
    private AppNotificationManager? _notificationManager;
    private bool _initialized = false;
    
    /// <summary>
    /// 初始化通知管理器
    /// </summary>
    public void Initialize()
    {
        try
        {
            _notificationManager = AppNotificationManager.Default;
            _notificationManager.NotificationInvoked += OnNotificationInvoked;
            _notificationManager.Register();
            _initialized = true;
            Debug.WriteLine("[Toast] Notification manager initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Toast] Failed to initialize: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 通知操作被触发时发生
    /// 参数: action ("rest", "startBreak", "snooze", "ignore")
    /// </summary>
    public event EventHandler<string>? NotificationActionInvoked;

    /// <summary>
    /// 发送干预提醒通知
    /// </summary>
    public void ShowInterventionNotification(double fatigueValue, string message)
    {
        if (!_initialized) return;
        
        var config = EyeGuard.Core.Models.NotificationConfig.Default;
        
        var toast = new AppNotificationBuilder()
            .AddText(string.Format(config.InterventionTitle, fatigueValue.ToString("F0")))
            .AddText(message)
            .AddButton(new AppNotificationButton(config.InterventionButtonRest)
                .AddArgument("action", "rest"))
            .AddButton(new AppNotificationButton(config.InterventionButtonSnooze)
                .AddArgument("action", "snooze"))
            .SetScenario(AppNotificationScenario.Alarm)
            .BuildNotification();
        
        _notificationManager?.Show(toast);
        Debug.WriteLine($"[Toast] Intervention notification sent: {message}");
    }
    
    /// <summary>
    /// 发送通用通知 (Debug用)
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        if (!_initialized) return;

        var toast = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message)
            .BuildNotification();

        _notificationManager?.Show(toast);
        Debug.WriteLine($"[Toast] General notification sent: {title} - {message}");
    }

    /// <summary>
    /// 发送 Break Task 通知
    /// </summary>
    public void ShowBreakTaskNotification(string taskName, int durationSeconds)
    {
        if (!_initialized) return;
        
        var config = EyeGuard.Core.Models.NotificationConfig.Default;
        
        var toast = new AppNotificationBuilder()
            .AddText(config.BreakTaskTitle)
            .AddText(string.Format(config.BreakTaskContent, taskName, durationSeconds))
            .AddButton(new AppNotificationButton(config.BreakTaskButtonStart)
                .AddArgument("action", "startBreak"))
            .AddButton(new AppNotificationButton(config.BreakTaskButtonIgnore)
                .AddArgument("action", "ignore"))
            .SetScenario(AppNotificationScenario.Alarm)
            .BuildNotification();
        
        _notificationManager?.Show(toast);
        Debug.WriteLine($"[Toast] Break task notification sent: {taskName}");
    }
    
    /// <summary>
    /// 通知被点击时的回调
    /// </summary>
    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        var arguments = args.Arguments;
        Debug.WriteLine($"[Toast] Notification invoked with arguments: {arguments}");
        
        // 解析 action 参数
        // 格式通常是: action=rest
        string action = "";
        if (args.Arguments.TryGetValue("action", out var value))
        {
            action = value;
        }
        
        if (!string.IsNullOrEmpty(action))
        {
            // 在 UI 线程触发事件
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                NotificationActionInvoked?.Invoke(this, action);
                
                // 确保主窗口激活
                if (action == "rest" || action == "startBreak")
                {
                   App.MainWindow.Activate();
                   if (App.MainWindow.Content is Microsoft.UI.Xaml.UIElement content)
                   {
                       content.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                   }
                }
            });
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Uninitialize()
    {
        if (_notificationManager != null)
        {
            _notificationManager.Unregister();
            _initialized = false;
        }
    }
}
