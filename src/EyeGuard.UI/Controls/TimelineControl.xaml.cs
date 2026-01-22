using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 时间轴条目数据
/// </summary>
public class TimelineItem
{
    public DateTime Time { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconGlyph { get; set; } = "\uE74C";
    public string ClusterName { get; set; } = "";
    public int DurationMinutes { get; set; }
    public bool IsFragmented { get; set; }
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }
    
    public string TimeText => Time.ToString("HH:mm");
    
    /// <summary>
    /// 圆点颜色（基于 Cluster 或碎片状态）
    /// </summary>
    public Brush DotColor
    {
        get
        {
            if (IsFragmented)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)); // 橙色 - 碎片
            }
            
            return ClusterName switch
            {
                "Coding" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)),
                "Writing" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 16)),
                "Meeting" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 140, 0)),
                "Entertainment" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 17, 35)),
                _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 138, 43, 226)) // 紫色默认
            };
        }
    }
    
    /// <summary>
    /// 连接线颜色（与圆点颜色一致，碎片时为虚线灰色）
    /// </summary>
    public Brush LineColor
    {
        get
        {
            if (IsFragmented)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(100, 128, 128, 128));
            }
            return DotColor;
        }
    }
    
    /// <summary>
    /// 是否显示连接线
    /// </summary>
    public Visibility ShowLine => IsLast ? Visibility.Collapsed : Visibility.Visible;
}

/// <summary>
/// 工作流时间轴控件 - Phase 3
/// 显示今日工作流程的垂直时间轴
/// </summary>
public sealed partial class TimelineControl : UserControl
{
    public TimelineControl()
    {
        this.InitializeComponent();
    }

    // 时间轴条目
    public ObservableCollection<TimelineItem> TimelineItems { get; } = new();
    
    // 空状态可见性
    public Visibility EmptyStateVisibility => TimelineItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 加载时间轴数据
    /// </summary>
    public void LoadData(List<(DateTime Time, string AppName, string ClusterName, int DurationMinutes, bool IsFragmented)> sessions)
    {
        TimelineItems.Clear();
        
        if (sessions.Count == 0) return;
        
        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var friendlyName = Services.IconMapper.GetFriendlyName(session.AppName);
            
            TimelineItems.Add(new TimelineItem
            {
                Time = session.Time,
                Title = session.ClusterName != "" ? session.ClusterName : friendlyName,
                Description = session.DurationMinutes > 0 
                    ? $"{friendlyName} - {session.DurationMinutes}分钟" 
                    : friendlyName,
                IconGlyph = Services.IconMapper.GetAppIcon(session.AppName),
                ClusterName = session.ClusterName,
                DurationMinutes = session.DurationMinutes,
                IsFragmented = session.IsFragmented,
                IsFirst = i == 0,
                IsLast = i == sessions.Count - 1
            });
        }
        
        // 通知 UI 更新
        Bindings.Update();
    }
}
