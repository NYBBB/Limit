using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EyeGuard.UI.Controls;

/// <summary>
/// Context Insight 横幅控件 - 显示动态洞察信息
/// </summary>
public sealed partial class InsightBanner : UserControl
{
    public InsightBanner()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 图标 Glyph (Material Symbols)
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(InsightBanner), new PropertyMetadata("\uE8C9")); // Default: lightbulb

    /// <summary>
    /// 主要消息文本
    /// </summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(InsightBanner), new PropertyMetadata(string.Empty));

    /// <summary>
    /// 次要文本（可选）
    /// </summary>
    public string SubText
    {
        get => (string)GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }
    public static readonly DependencyProperty SubTextProperty =
        DependencyProperty.Register(nameof(SubText), typeof(string), typeof(InsightBanner), new PropertyMetadata(string.Empty));

    /// <summary>
    /// 是否显示次要文本
    /// </summary>
    public Visibility HasSubText => string.IsNullOrEmpty(SubText) ? Visibility.Collapsed : Visibility.Visible;
}
