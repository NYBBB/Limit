using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.UI;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 消耗排行条目数据模型
/// </summary>
public class DrainerItem
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public string IconGlyph { get; set; } = "\uE74C"; // 默认应用图标
    public double Percentage { get; set; }
    public string Duration { get; set; } = "";
    public Brush BarColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 138, 43, 226));
}

/// <summary>
/// 消耗排行卡片控件 - Limit 3.0
/// Zone C: 显示 Top 3 精力消耗应用
/// </summary>
public sealed partial class DrainersCard : UserControl
{
    public DrainersCard()
    {
        this.InitializeComponent();
    }

    // ===== 依赖属性 =====

    /// <summary>
    /// Top 3 消耗排行列表
    /// </summary>
    public static readonly DependencyProperty DrainersProperty =
        DependencyProperty.Register(nameof(Drainers), typeof(ObservableCollection<DrainerItem>), typeof(DrainersCard),
            new PropertyMetadata(new ObservableCollection<DrainerItem>()));

    public ObservableCollection<DrainerItem> Drainers
    {
        get => (ObservableCollection<DrainerItem>)GetValue(DrainersProperty);
        set => SetValue(DrainersProperty, value);
    }

    /// <summary>
    /// 是否显示碎片时间警告
    /// </summary>
    public static readonly DependencyProperty ShowFragmentWarningProperty =
        DependencyProperty.Register(nameof(ShowFragmentWarning), typeof(Visibility), typeof(DrainersCard),
            new PropertyMetadata(Visibility.Collapsed));

    public Visibility ShowFragmentWarning
    {
        get => (Visibility)GetValue(ShowFragmentWarningProperty);
        set => SetValue(ShowFragmentWarningProperty, value);
    }

    /// <summary>
    /// 碎片时间文本
    /// </summary>
    public static readonly DependencyProperty FragmentTimeProperty =
        DependencyProperty.Register(nameof(FragmentTime), typeof(string), typeof(DrainersCard),
            new PropertyMetadata("0分钟"));

    public string FragmentTime
    {
        get => (string)GetValue(FragmentTimeProperty);
        set => SetValue(FragmentTimeProperty, value);
    }
}
