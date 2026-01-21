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
    /// 发送干预提醒通知
    /// </summary>
    public void ShowInterventionNotification(double fatigueValue, string message)
    {
        if (!_initialized) return;
        
        var toast = new AppNotificationBuilder()
            .AddText($"疲劳提醒 ({fatigueValue:F0}%)")
            .AddText(message)
            .AddButton(new AppNotificationButton("休息一下")
                .AddArgument("action", "rest"))
            .AddButton(new AppNotificationButton("稍后提醒")
                .AddArgument("action", "snooze"))
            .BuildNotification();
        
        _notificationManager?.Show(toast);
        Debug.WriteLine($"[Toast] Intervention notification sent: {message}");
    }
    
    /// <summary>
    /// 发送 Break Task 通知
    /// </summary>
    public void ShowBreakTaskNotification(string taskName, int durationSeconds)
    {
        if (!_initialized) return;
        
        var toast = new AppNotificationBuilder()
            .AddText("该休息了！")
            .AddText($"{taskName} - {durationSeconds}秒")
            .AddButton(new AppNotificationButton("开始休息")
                .AddArgument("action", "startBreak"))
            .AddButton(new AppNotificationButton("忽略")
                .AddArgument("action", "ignore"))
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
        
        // TODO: 根据 action 参数处理不同的点击事件
        // 例如: "rest" -> 打开主窗口并触发休息
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
