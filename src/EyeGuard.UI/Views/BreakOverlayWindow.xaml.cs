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
        
        // Phase 5: Overlay 3.0 新文案（根据疲劳值调整语气）
        if (fatigueValue >= 80)
        {
            TitleText.Text = "Diminishing Returns";
            SubtitleText.Text = "继续工作的效率正在急剧下降";
        }
        else if (fatigueValue >= 60)
        {
            TitleText.Text = "Time for a Break";
            SubtitleText.Text = "短暂休息可以帮助恢复专注力";
        }
        else
        {
            TitleText.Text = "Quick Rest";
            SubtitleText.Text = "让眼睛放松一会儿";
        }
        
        this.Activate();
        
        // 不自动开始倒计时，等用户点击 Step Away
    }

    /// <summary>
    /// 开始倒计时（用户点击 Step Away 后触发）。
    /// </summary>
    private void StartCountdown()
    {
        TipText.Text = "请远眺窗外或闭眼休息...";
        
        // 隐藏按钮区，显示倒计时
        StartBreakButton.Visibility = Visibility.Collapsed;
        SkipButton.Visibility = Visibility.Collapsed;
        HoldProgressPanel.Visibility = Visibility.Collapsed;
        CountdownPanel.Visibility = Visibility.Visible;
        
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
            _holdTimer?.Stop();
            UserAction = BreakAction.Skipped;
            BreakCompleted?.Invoke(this, UserAction);
            this.Close();
        }
    }
    
    // ===== Phase 5: 长按 3 秒解锁逻辑 =====
    
    private DispatcherTimer? _holdTimer;
    private int _holdProgress = 0;
    private const int HoldDurationMs = 3000;  // 3 秒
    private const int HoldTickMs = 50;        // 每 50ms 更新一次
    
    private void SkipButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // 开始长按计时
        _holdProgress = 0;
        HoldProgressPanel.Visibility = Visibility.Visible;
        HoldProgressBar.Value = 0;
        HoldStatusText.Text = "继续按住...";
        
        _holdTimer = new DispatcherTimer();
        _holdTimer.Interval = TimeSpan.FromMilliseconds(HoldTickMs);
        _holdTimer.Tick += HoldTimer_Tick;
        _holdTimer.Start();
        
        Debug.WriteLine("[Overlay] Hold started");
    }
    
    private void SkipButton_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        CancelHold();
    }
    
    private void SkipButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CancelHold();
    }
    
    private void CancelHold()
    {
        _holdTimer?.Stop();
        _holdProgress = 0;
        HoldProgressBar.Value = 0;
        HoldProgressPanel.Visibility = Visibility.Collapsed;
        
        Debug.WriteLine("[Overlay] Hold cancelled");
    }
    
    private void HoldTimer_Tick(object? sender, object e)
    {
        _holdProgress += HoldTickMs;
        var progressPercent = (_holdProgress * 100.0) / HoldDurationMs;
        
        HoldProgressBar.Value = progressPercent;
        HoldStatusText.Text = $"继续按住... ({_holdProgress / 1000.0:F1}s / 3s)";
        
        if (_holdProgress >= HoldDurationMs)
        {
            // 完成长按，跳过休息
            _holdTimer?.Stop();
            _countdownTimer?.Stop();
            
            Debug.WriteLine("[Overlay] Hold completed - skipping");
            
            UserAction = BreakAction.Skipped;
            BreakCompleted?.Invoke(this, UserAction);
            this.Close();
        }
    }
    
    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        // 兼容旧代码，但长按逻辑由 PointerPressed 处理
        // 普通点击不执行任何操作，提示用户长按
        TipText.Text = "请长按 \"I Must Finish\" 按钮 3 秒才能跳过";
    }
    
    private void SnoozeButton_Click(object sender, RoutedEventArgs e)
    {
        _countdownTimer?.Stop();
        _holdTimer?.Stop();
        UserAction = BreakAction.Snoozed;
        BreakCompleted?.Invoke(this, UserAction);
        this.Close();
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
