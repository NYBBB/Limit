using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 热力图单元格数据
/// </summary>
public class HeatmapCell
{
    public DateTime Date { get; set; }
    public int Hour { get; set; }
    public double FatigueValue { get; set; }
    public int ActiveMinutes { get; set; }
    public string TopApp { get; set; } = "";
    
    /// <summary>
    /// 根据疲劳值计算颜色
    /// </summary>
    public Brush CellColor
    {
        get
        {
            if (ActiveMinutes == 0)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128));
            }
            
            byte alpha = (byte)(100 + (FatigueValue * 1.55));
            
            if (FatigueValue < 30)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 76, 175, 80));
            }
            else if (FatigueValue < 60)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 138, 43, 226));
            }
            else if (FatigueValue < 80)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 255, 152, 0));
            }
            else
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 244, 67, 54));
            }
        }
    }
    
    public string Tooltip
    {
        get
        {
            if (ActiveMinutes == 0)
            {
                return $"{Date:MM/dd} {Hour}:00 - 无数据";
            }
            return $"{Date:MM/dd} {Hour}:00\n疲劳: {FatigueValue:F0}%\n活跃: {ActiveMinutes}分钟\n{TopApp}";
        }
    }
}

/// <summary>
/// 热力图行数据
/// </summary>
public class HeatmapRow
{
    public DateTime Date { get; set; }
    public ObservableCollection<HeatmapCell> Cells { get; } = new();
}

/// <summary>
/// 精力热力图控件 - Phase 3
/// </summary>
public sealed partial class HeatmapControl : UserControl
{
    public HeatmapControl()
    {
        this.InitializeComponent();
        InitializeHourLabels();
    }

    public ObservableCollection<string> DayLabels { get; } = new();
    public ObservableCollection<HeatmapRow> HeatmapRows { get; } = new();

    private void InitializeHourLabels()
    {
        // 创建小时标签
        for (int i = 0; i < 24; i++)
        {
            HourLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        
        // 只显示关键小时
        int[] keyHours = { 0, 6, 12, 18, 23 };
        foreach (var hour in keyHours)
        {
            var label = new TextBlock
            {
                Text = hour.ToString(),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
            };
            Grid.SetColumn(label, hour);
            HourLabelsGrid.Children.Add(label);
        }
    }

    public void LoadData(List<(DateTime Date, int Hour, double Fatigue, int ActiveMinutes, string TopApp)> data)
    {
        DayLabelsPanel.Children.Clear();
        HeatmapGrid.Children.Clear();
        
        var today = DateTime.Today;
        for (int dayOffset = 6; dayOffset >= 0; dayOffset--)
        {
            var date = today.AddDays(-dayOffset);
            
            // 日期标签
            DayLabelsPanel.Children.Add(new TextBlock
            {
                Text = date.ToString("ddd"),
                FontSize = 11,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            
            // 创建该天的格子行
            var rowGrid = new Grid { Height = 24 };
            for (int i = 0; i < 24; i++)
            {
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            
            // 填充 24 个格子
            for (int hour = 0; hour < 24; hour++)
            {
                var cellData = data.FirstOrDefault(d => d.Date.Date == date && d.Hour == hour);
                var cell = new HeatmapCell
                {
                    Date = date,
                    Hour = hour,
                    FatigueValue = cellData.Fatigue,
                    ActiveMinutes = cellData.ActiveMinutes,
                    TopApp = cellData.TopApp ?? ""
                };
                
                var border = new Border
                {
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Background = cell.CellColor
                };
                ToolTipService.SetToolTip(border, cell.Tooltip);
                
                Grid.SetColumn(border, hour);
                rowGrid.Children.Add(border);
            }
            
            HeatmapGrid.Children.Add(rowGrid);
        }
    }
}
