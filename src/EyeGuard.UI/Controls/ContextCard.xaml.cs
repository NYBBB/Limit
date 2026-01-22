using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 上下文感知卡片控件 - Limit 3.0
/// Zone B: 显示当前应用、Cluster 和纠偏开关
/// Phase 2.5: 增强版 - 添加 Session Timer 和 Segmented Control
/// </summary>
public sealed partial class ContextCard : UserControl
{
    // Segmented Control 颜色常量
    private static readonly Brush ActiveBackground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
    private static readonly Brush InactiveBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Transparent
    private static readonly Brush ActiveForeground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // White
    private static readonly Brush InactiveForeground = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));

    public ContextCard()
    {
        this.InitializeComponent();
        UpdateSegmentedControl();
    }

    // ===== 依赖属性 =====

    public static readonly DependencyProperty AppNameProperty =
        DependencyProperty.Register(nameof(AppName), typeof(string), typeof(ContextCard),
            new PropertyMetadata("未知应用"));

    public string AppName
    {
        get => (string)GetValue(AppNameProperty);
        set => SetValue(AppNameProperty, value);
    }

    public static readonly DependencyProperty AppIconProperty =
        DependencyProperty.Register(nameof(AppIcon), typeof(string), typeof(ContextCard),
            new PropertyMetadata("\uE74C"));

    public string AppIcon
    {
        get => (string)GetValue(AppIconProperty);
        set => SetValue(AppIconProperty, value);
    }

    public static readonly DependencyProperty ClusterNameProperty =
        DependencyProperty.Register(nameof(ClusterName), typeof(string), typeof(ContextCard),
            new PropertyMetadata("未分类"));

    public string ClusterName
    {
        get => (string)GetValue(ClusterNameProperty);
        set => SetValue(ClusterNameProperty, value);
    }

    public static readonly DependencyProperty ClusterColorProperty =
        DependencyProperty.Register(nameof(ClusterColor), typeof(Brush), typeof(ContextCard),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 136, 136, 136))));

    public Brush ClusterColor
    {
        get => (Brush)GetValue(ClusterColorProperty);
        set => SetValue(ClusterColorProperty, value);
    }

    // Phase 2.5: Session Timer
    public static readonly DependencyProperty SessionTimeProperty =
        DependencyProperty.Register(nameof(SessionTime), typeof(string), typeof(ContextCard),
            new PropertyMetadata("00:00:00"));

    public string SessionTime
    {
        get => (string)GetValue(SessionTimeProperty);
        set => SetValue(SessionTimeProperty, value);
    }
    
    // 时间流图标属性
    public static readonly DependencyProperty RecentApp1IconProperty =
        DependencyProperty.Register(nameof(RecentApp1Icon), typeof(string), typeof(ContextCard),
            new PropertyMetadata("", OnRecentAppIconChanged));

    public string RecentApp1Icon
    {
        get => (string)GetValue(RecentApp1IconProperty);
        set => SetValue(RecentApp1IconProperty, value);
    }
    
    public static readonly DependencyProperty RecentApp2IconProperty =
        DependencyProperty.Register(nameof(RecentApp2Icon), typeof(string), typeof(ContextCard),
            new PropertyMetadata("", OnRecentAppIconChanged));

    public string RecentApp2Icon
    {
        get => (string)GetValue(RecentApp2IconProperty);
        set => SetValue(RecentApp2IconProperty, value);
    }
    
    public static readonly DependencyProperty RecentApp3IconProperty =
        DependencyProperty.Register(nameof(RecentApp3Icon), typeof(string), typeof(ContextCard),
            new PropertyMetadata("", OnRecentAppIconChanged));

    public string RecentApp3Icon
    {
        get => (string)GetValue(RecentApp3IconProperty);
        set => SetValue(RecentApp3IconProperty, value);
    }
    
    // 时间流图标可见性
    public Visibility RecentApp1IconVisibility => string.IsNullOrEmpty(RecentApp1Icon) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility RecentApp2IconVisibility => string.IsNullOrEmpty(RecentApp2Icon) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility RecentApp3IconVisibility => string.IsNullOrEmpty(RecentApp3Icon) ? Visibility.Collapsed : Visibility.Visible;
    
    private static void OnRecentAppIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContextCard card)
        {
            card.Bindings.Update();
        }
    }

    public static readonly DependencyProperty IsFocusingProperty =
        DependencyProperty.Register(nameof(IsFocusing), typeof(bool), typeof(ContextCard),
            new PropertyMetadata(false, OnIsFocusingChanged));

    public bool IsFocusing
    {
        get => (bool)GetValue(IsFocusingProperty);
        set => SetValue(IsFocusingProperty, value);
    }

    // Phase 2.5: Segmented Control 背景色
    public Brush FocusingBackground => IsFocusing ? ActiveBackground : InactiveBackground;
    public Brush ChillingBackground => !IsFocusing ? ActiveBackground : InactiveBackground;
    public Brush FocusingForeground => IsFocusing ? ActiveForeground : InactiveForeground;
    public Brush ChillingForeground => !IsFocusing ? ActiveForeground : InactiveForeground;

    public event EventHandler<bool>? FocusingChanged;

    private static void OnIsFocusingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContextCard card)
        {
            card.UpdateSegmentedControl();
            card.FocusingChanged?.Invoke(card, (bool)e.NewValue);
        }
    }

    private void UpdateSegmentedControl()
    {
        // 触发属性变更通知（手动刷新绑定）
        Bindings.Update();
    }

    // Phase 2.5: Segmented Control 点击事件
    private void FocusingButton_Click(object sender, RoutedEventArgs e)
    {
        // Phase 9: 触发专注承诺请求事件，让父页面弹出设定盘
        FocusCommitmentRequested?.Invoke(this, System.EventArgs.Empty);
    }

    private void ChillingButton_Click(object sender, RoutedEventArgs e)
    {
        IsFocusing = false;
        // Phase 9: 停止专注模式
        StopFocusRequested?.Invoke(this, System.EventArgs.Empty);
    }
    
    // Phase 9: 专注承诺请求事件
    public event System.EventHandler? FocusCommitmentRequested;
    public event System.EventHandler? StopFocusRequested;
}
