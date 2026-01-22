using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.Foundation;
using System;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 精力反应堆核心控件 - Limit 3.0 Phase 9
/// 支持单环(疲劳)和双环(疲劳+专注倒计时)模式
/// 使用自定义 Arc 绘制，呼吸动画同步作用于弧线
/// </summary>
public sealed partial class FatigueRing : UserControl
{
    // 圆环几何参数
    // 内环轨道 Ellipse: 260x260, StrokeThickness=24 → 描边中心半径 = 130 - 12 = 118
    // 外环轨道 Ellipse: 290x290, StrokeThickness=12 → 描边中心半径 = 145 - 6 = 139
    private const double InnerRadius = 118; // 内环描边中心半径
    private const double OuterRadius = 139; // 外环描边中心半径
    private const double CenterX = 150;
    private const double CenterY = 150;
    
    public FatigueRing()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 启动呼吸动画
        BreathingAnimation.Begin();
        
        // 如果是省电模式，立即暂停动画
        if (IsEcoMode)
        {
            BreathingAnimation.Pause();
        }
        
        // 初始化弧线
        UpdateInnerArc();
        UpdateOuterArc();
        UpdateValueText();
    }

    // ===== 依赖属性 =====

    /// <summary>
    /// 疲劳值 (0-100)
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(FatigueRing),
            new PropertyMetadata(0.0, OnValueChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// 状态标签
    /// </summary>
    public static readonly DependencyProperty StatusLabelProperty =
        DependencyProperty.Register(nameof(StatusLabel), typeof(string), typeof(FatigueRing),
            new PropertyMetadata("精力充沛"));

    public string StatusLabel
    {
        get => (string)GetValue(StatusLabelProperty);
        set => SetValue(StatusLabelProperty, value);
    }

    /// <summary>
    /// 建议文案
    /// </summary>
    public static readonly DependencyProperty SuggestionProperty =
        DependencyProperty.Register(nameof(Suggestion), typeof(string), typeof(FatigueRing),
            new PropertyMetadata("适合高强度工作"));

    public string Suggestion
    {
        get => (string)GetValue(SuggestionProperty);
        set => SetValue(SuggestionProperty, value);
    }

    /// <summary>
    /// Care Mode 可见性
    /// </summary>
    public static readonly DependencyProperty IsCareModeProperty =
        DependencyProperty.Register(nameof(IsCareMode), typeof(Visibility), typeof(FatigueRing),
            new PropertyMetadata(Visibility.Collapsed));

    public Visibility IsCareMode
    {
        get => (Visibility)GetValue(IsCareModeProperty);
        set => SetValue(IsCareModeProperty, value);
    }

    /// <summary>
    /// 是否为省电模式（暂停动画）
    /// </summary>
    public static readonly DependencyProperty IsEcoModeProperty =
        DependencyProperty.Register(nameof(IsEcoMode), typeof(bool), typeof(FatigueRing),
            new PropertyMetadata(false, OnEcoModeChanged));

    public bool IsEcoMode
    {
        get => (bool)GetValue(IsEcoModeProperty);
        set => SetValue(IsEcoModeProperty, value);
    }

    private static void OnEcoModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FatigueRing ring)
        {
            if ((bool)e.NewValue)
            {
                // Eco 模式：暂停呼吸动画
                ring.BreathingAnimation.Pause();
            }
            else
            {
                // 正常模式：恢复呼吸动画
                ring.BreathingAnimation.Resume();
            }
        }
    }

    /// <summary>
    /// 内环颜色
    /// </summary>
    public static readonly DependencyProperty RingColorProperty =
        DependencyProperty.Register(nameof(RingColor), typeof(Brush), typeof(FatigueRing),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 178, 148))));

    public Brush RingColor
    {
        get => (Brush)GetValue(RingColorProperty);
        set => SetValue(RingColorProperty, value);
    }

    // ===== 双环模式属性 =====

    /// <summary>
    /// 是否为专注模式（双环）
    /// </summary>
    public static readonly DependencyProperty IsFocusModeProperty =
        DependencyProperty.Register(nameof(IsFocusMode), typeof(bool), typeof(FatigueRing),
            new PropertyMetadata(false, OnFocusModeChanged));

    public bool IsFocusMode
    {
        get => (bool)GetValue(IsFocusModeProperty);
        set => SetValue(IsFocusModeProperty, value);
    }

    /// <summary>
    /// 专注剩余秒数
    /// </summary>
    public static readonly DependencyProperty FocusRemainingSecondsProperty =
        DependencyProperty.Register(nameof(FocusRemainingSeconds), typeof(int), typeof(FatigueRing),
            new PropertyMetadata(0, OnFocusTimeChanged));

    public int FocusRemainingSeconds
    {
        get => (int)GetValue(FocusRemainingSecondsProperty);
        set => SetValue(FocusRemainingSecondsProperty, value);
    }

    /// <summary>
    /// 专注总秒数
    /// </summary>
    public static readonly DependencyProperty FocusTotalSecondsProperty =
        DependencyProperty.Register(nameof(FocusTotalSeconds), typeof(int), typeof(FatigueRing),
            new PropertyMetadata(0, OnFocusTimeChanged));

    public int FocusTotalSeconds
    {
        get => (int)GetValue(FocusTotalSecondsProperty);
        set => SetValue(FocusTotalSecondsProperty, value);
    }

    /// <summary>
    /// 专注任务名称
    /// </summary>
    public static readonly DependencyProperty FocusTaskNameProperty =
        DependencyProperty.Register(nameof(FocusTaskName), typeof(string), typeof(FatigueRing),
            new PropertyMetadata("专注中..."));

    public string FocusTaskName
    {
        get => (string)GetValue(FocusTaskNameProperty);
        set => SetValue(FocusTaskNameProperty, value);
    }

    /// <summary>
    /// 倒计时文本
    /// </summary>
    public static readonly DependencyProperty FocusCountdownTextProperty =
        DependencyProperty.Register(nameof(FocusCountdownText), typeof(string), typeof(FatigueRing),
            new PropertyMetadata("00:00"));

    public string FocusCountdownText
    {
        get => (string)GetValue(FocusCountdownTextProperty);
        set => SetValue(FocusCountdownTextProperty, value);
    }

    /// <summary>
    /// 外环颜色
    /// </summary>
    public static readonly DependencyProperty OuterRingColorProperty =
        DependencyProperty.Register(nameof(OuterRingColor), typeof(Brush), typeof(FatigueRing),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 138, 43, 226)))); // 紫色

    public Brush OuterRingColor
    {
        get => (Brush)GetValue(OuterRingColorProperty);
        set => SetValue(OuterRingColorProperty, value);
    }

    // ===== 计算属性 =====

    public Visibility OuterRingVisibility => IsFocusMode ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SingleModeVisibility => IsFocusMode ? Visibility.Collapsed : Visibility.Visible;
    public Visibility DualModeVisibility => IsFocusMode ? Visibility.Visible : Visibility.Collapsed;

    // ===== 事件处理 =====

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FatigueRing ring)
        {
            ring.UpdateValueText();
            ring.UpdateRingColor();
            ring.UpdateStatusAndSuggestion();
            ring.UpdateInnerArc();
        }
    }

    private static void OnFocusModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FatigueRing ring)
        {
            // 更新可见性绑定
            ring.Bindings.Update();
        }
    }

    private static void OnFocusTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FatigueRing ring)
        {
            ring.UpdateOuterArc();
            ring.UpdateCountdownText();
        }
    }

    // ===== Arc 绘制方法 =====

    /// <summary>
    /// 更新内环弧线（疲劳值）
    /// </summary>
    private void UpdateInnerArc()
    {
        if (InnerArcFigure == null || InnerArcSegment == null) return;

        double percentage = Math.Max(0, Math.Min(100, Value)) / 100.0;
        // 内环圆心 (150, 150)，半径 130
        UpdateArcGeometry(InnerArcFigure, InnerArcSegment, InnerRadius, percentage, CenterX, CenterY);
    }

    /// <summary>
    /// 更新外环弧线（专注倒计时）
    /// </summary>
    private void UpdateOuterArc()
    {
        if (OuterArcFigure == null || OuterArcSegment == null) return;
        if (FocusTotalSeconds <= 0) return;

        double percentage = (double)FocusRemainingSeconds / FocusTotalSeconds;
        percentage = Math.Max(0, Math.Min(1, percentage));
        // 外环圆心 (150, 150)，半径 145
        UpdateArcGeometry(OuterArcFigure, OuterArcSegment, OuterRadius, percentage, CenterX, CenterY);
    }

    /// <summary>
    /// 通用弧线几何更新
    /// </summary>
    private void UpdateArcGeometry(PathFigure figure, ArcSegment segment, double radius, double percentage, double centerX, double centerY)
    {
        // 从顶部 (-90°) 开始顺时针绘制
        double startAngle = -90;
        double sweepAngle = percentage * 360;

        // 防止 360° 时弧线消失
        if (sweepAngle >= 360) sweepAngle = 359.99;
        if (sweepAngle <= 0) sweepAngle = 0.01;

        double startRad = startAngle * Math.PI / 180;
        double endRad = (startAngle + sweepAngle) * Math.PI / 180;

        Point startPoint = new Point(
            centerX + radius * Math.Cos(startRad),
            centerY + radius * Math.Sin(startRad)
        );

        Point endPoint = new Point(
            centerX + radius * Math.Cos(endRad),
            centerY + radius * Math.Sin(endRad)
        );

        figure.StartPoint = startPoint;
        segment.Point = endPoint;
        segment.Size = new Size(radius, radius);
        segment.IsLargeArc = sweepAngle > 180;
        segment.SweepDirection = SweepDirection.Clockwise;
    }

    // ===== 辅助方法 =====

    private void UpdateValueText()
    {
        if (ValueText != null)
        {
            ValueText.Text = ((int)Math.Round(Value)).ToString();
        }
        if (SmallFatigueText != null)
        {
            SmallFatigueText.Text = ((int)Math.Round(Value)).ToString();
        }
    }

    private void UpdateCountdownText()
    {
        int minutes = FocusRemainingSeconds / 60;
        int seconds = FocusRemainingSeconds % 60;
        FocusCountdownText = $"{minutes:D2}:{seconds:D2}";
    }

    private void UpdateRingColor()
    {
        Color color;
        if (Value < 40)
        {
            color = Color.FromArgb(255, 0, 178, 148); // Teal
        }
        else if (Value < 70)
        {
            color = Color.FromArgb(255, 255, 185, 0); // Amber
        }
        else
        {
            color = Color.FromArgb(255, 232, 17, 35); // Red
        }
        RingColor = new SolidColorBrush(color);
    }

    private void UpdateStatusAndSuggestion()
    {
        if (Value < 30)
        {
            StatusLabel = "精力充沛";
            Suggestion = "适合高强度工作";
        }
        else if (Value < 50)
        {
            StatusLabel = "专注中";
            Suggestion = "保持当前节奏";
        }
        else if (Value < 70)
        {
            StatusLabel = "略感疲惫";
            Suggestion = "建议处理轻量任务";
        }
        else if (Value < 85)
        {
            StatusLabel = "能量不足";
            Suggestion = "仅适合浏览邮件";
        }
        else
        {
            StatusLabel = "需要休息";
            Suggestion = "效率已大幅下降";
        }
    }
}
