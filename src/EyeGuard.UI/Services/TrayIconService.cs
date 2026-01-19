using System;
using System.Diagnostics;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;

namespace EyeGuard.UI.Services;

/// <summary>
/// 系统托盘管理服务。
/// 使用 H.NotifyIcon.WinUI 库。
/// </summary>
public class TrayIconService : IDisposable
{
    private TaskbarIcon? _taskbarIcon;
    private bool _disposed;
    
    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? StartMonitoringRequested;
    public event EventHandler? StopMonitoringRequested;

    public void Initialize()
    {
        _taskbarIcon = new TaskbarIcon();
        
        // 设置工具提示
        _taskbarIcon.ToolTipText = "EyeGuard - 眼睛守护";
        
        // 左键点击显示窗口
        _taskbarIcon.LeftClickCommand = new RelayCommand(() => ShowRequested?.Invoke(this, EventArgs.Empty));
        
        // 创建右键菜单
        var menuFlyout = new MenuFlyout();
        
        var showItem = new MenuFlyoutItem { Text = "显示主窗口" };
        showItem.Click += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        menuFlyout.Items.Add(showItem);
        
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        
        var startItem = new MenuFlyoutItem { Text = "开始监测" };
        startItem.Click += (s, e) => StartMonitoringRequested?.Invoke(this, EventArgs.Empty);
        menuFlyout.Items.Add(startItem);
        
        var stopItem = new MenuFlyoutItem { Text = "暂停监测" };
        stopItem.Click += (s, e) => StopMonitoringRequested?.Invoke(this, EventArgs.Empty);
        menuFlyout.Items.Add(stopItem);
        
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        
        var exitItem = new MenuFlyoutItem { Text = "退出" };
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menuFlyout.Items.Add(exitItem);
        
        _taskbarIcon.ContextFlyout = menuFlyout;
        
        Debug.WriteLine("[TrayIcon] Initialized");
    }

    /// <summary>
    /// 更新托盘图标提示文本。
    /// </summary>
    public void UpdateTooltip(string text)
    {
        if (_taskbarIcon != null)
        {
            _taskbarIcon.ToolTipText = text;
        }
    }

    /// <summary>
    /// 显示气泡通知。
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        _taskbarIcon?.ShowNotification(title, message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _taskbarIcon?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~TrayIconService()
    {
        Dispose();
    }
}

/// <summary>
/// 简单的命令实现。
/// </summary>
public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
