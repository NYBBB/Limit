using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using Windows.Graphics;

namespace EyeGuard.UI.Views;

/// <summary>
/// 休息提醒窗口（窗口模式，不是全屏）。
/// </summary>
public sealed partial class BreakOverlayWindow : Window
{
    private DispatcherTimer? _countdownTimer;
    private int _remainingSeconds;
    
    /// <summary>
    /// 休息时长（秒）。
    /// </summary>
    public int BreakDurationSeconds { get; set; } = 300;
    
    /// <summary>
    /// 当前疲劳值。
    /// </summary>
    public int FatigueValue { get; set; } = 80;
    
    /// <summary>
    /// 休息任务名称 (Limit 2.0)
    /// </summary>
    public string BreakTaskName { get; set; } = "";
    
    /// <summary>
    /// 休息任务描述 (Limit 2.0)
    /// </summary>
    public string BreakTaskDescription { get; set; } = "";
    
    /// <summary>
    /// 用户选择的操作。
    /// </summary>
    public BreakAction UserAction { get; private set; } = BreakAction.None;
    
    /// <summary>
    /// 窗口关闭时触发。
    /// </summary>
    public event EventHandler<BreakAction>? BreakCompleted;

    public BreakOverlayWindow()
    {
        InitializeComponent();
        
        // 设置窗口属性
        SetupWindow();
        
        // 设置键盘事件
        RootGrid.KeyDown += OnKeyDown;
        RootGrid.Loaded += (s, e) => RootGrid.Focus(FocusState.Programmatic);
    }

    private void SetupWindow()
    {
        // 获取窗口句柄
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        
        // 设置窗口为固定大小的居中窗口（不是全屏）
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
        }
        
        // 设置窗口大小 (600x500) 并居中
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        int windowWidth = 600;
        int windowHeight = 500;
        int centerX = (displayArea.WorkArea.Width - windowWidth) / 2;
        int centerY = (displayArea.WorkArea.Height - windowHeight) / 2;
        
        appWindow.MoveAndResize(new RectInt32(centerX, centerY, windowWidth, windowHeight));
    }

    /// <summary>
    /// 显示弹窗并自动开始倒计时。
    /// </summary>
    public void ShowOverlay(int fatigueValue = 80, int breakDurationSeconds = 300)
    {
        FatigueValue = fatigueValue;
        BreakDurationSeconds = breakDurationSeconds;
        _remainingSeconds = breakDurationSeconds;
        
        // 更新 UI
        FatigueProgressBar.Value = fatigueValue;
        FatigueText.Text = $"{fatigueValue}%";
        UpdateCountdownDisplay();
        
        // 根据疲劳值设置标题
        if (fatigueValue >= 80)
        {
            TitleText.Text = "⚠️ 疲劳度过高！";
            SubtitleText.Text = "强烈建议您立即休息";
        }
        else if (fatigueValue >= 60)
        {
            TitleText.Text = "该休息了！";
            SubtitleText.Text = "您已经连续工作了一段时间";
        }
        else
        {
            TitleText.Text = "休息一下吧";
            SubtitleText.Text = "让眼睛放松一会儿";
        }
        
        this.Activate();
        
        // 自动开始倒计时
        StartCountdown();
    }

    /// <summary>
    /// 开始倒计时。
    /// </summary>
    private void StartCountdown()
    {
        StatusText.Text = "休息中...";
        TipText.Text = "请远眺窗外或闭眼休息";
        
        // 隐藏"开始休息"按钮
        StartBreakButton.Visibility = Visibility.Collapsed;
        
        _countdownTimer = new DispatcherTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += CountdownTimer_Tick;
        _countdownTimer.Start();
    }

    private void CountdownTimer_Tick(object? sender, object e)
    {
        _remainingSeconds--;
        UpdateCountdownDisplay();
        
        if (_remainingSeconds <= 0)
        {
            _countdownTimer?.Stop();
            UserAction = BreakAction.Completed;
            BreakCompleted?.Invoke(this, UserAction);
            this.Close();
        }
    }

    private void UpdateCountdownDisplay()
    {
        int minutes = _remainingSeconds / 60;
        int seconds = _remainingSeconds % 60;
        CountdownText.Text = $"{minutes}:{seconds:D2}";
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        _countdownTimer?.Stop();
        UserAction = BreakAction.Skipped;
        BreakCompleted?.Invoke(this, UserAction);
        this.Close();
    }

    private void SnoozeButton_Click(object sender, RoutedEventArgs e)
    {
        _countdownTimer?.Stop();
        UserAction = BreakAction.Snoozed;
        BreakCompleted?.Invoke(this, UserAction);
        this.Close();
    }

    private void StartBreakButton_Click(object sender, RoutedEventArgs e)
    {
        // 如果用户想手动开始（虽然现在自动开始了）
        StartCountdown();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _countdownTimer?.Stop();
        UserAction = BreakAction.Skipped;
        BreakCompleted?.Invoke(this, UserAction);
        this.Close();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _countdownTimer?.Stop();
            UserAction = BreakAction.Skipped;
            BreakCompleted?.Invoke(this, UserAction);
            this.Close();
        }
    }
}

/// <summary>
/// 用户在休息弹窗中选择的操作。
/// </summary>
public enum BreakAction
{
    None,
    Completed,  // 完成休息
    Skipped,    // 跳过
    Snoozed     // 推迟5分钟
}
