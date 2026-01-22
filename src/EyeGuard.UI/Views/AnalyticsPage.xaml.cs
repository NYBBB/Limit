using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EyeGuard.UI.ViewModels;
using EyeGuard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EyeGuard.UI.Views;

/// <summary>
/// 分析页面 - 显示历史使用统计。
/// </summary>
public sealed partial class AnalyticsPage : Page
{
    public AnalyticsViewModel ViewModel { get; private set; }
    private readonly DatabaseService _databaseService;

    public AnalyticsPage()
    {
        this.InitializeComponent();
        
        // 创建 ViewModel 但不在构造函数中加载数据
        ViewModel = new AnalyticsViewModel(skipInitialLoad: true);
        this.DataContext = ViewModel;
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        
        // 在 Loaded 事件中异步加载数据，避免阻塞 UI 线程
        this.Loaded += AnalyticsPage_Loaded;
    }

    private async void AnalyticsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 异步加载数据，不阻塞 UI
        await ViewModel.LoadDataForDateAsync(DateTime.Today);
        
        // 加载热力图数据
        await LoadHeatmapDataAsync();
    }
    
    /// <summary>
    /// 加载过去7天的热力图数据
    /// </summary>
    private async Task LoadHeatmapDataAsync()
    {
        try
        {
            var heatmapData = new List<(DateTime Date, int Hour, double Fatigue, int ActiveMinutes, string TopApp)>();
            
            // 加载过去7天的数据
            for (int dayOffset = 6; dayOffset >= 0; dayOffset--)
            {
                var date = DateTime.Today.AddDays(-dayOffset);
                
                // 获取该天的疲劳快照
                var snapshots = await _databaseService.GetFatigueSnapshotsAsync(date);
                var hourlyRecords = await _databaseService.GetHourlyUsageAsync(date);
                
                // 按小时聚合
                for (int hour = 0; hour < 24; hour++)
                {
                    // 该小时的疲劳快照
                    var hourSnapshots = snapshots.Where(s => s.RecordedAt.Hour == hour).ToList();
                    double avgFatigue = hourSnapshots.Count > 0 ? hourSnapshots.Average(s => s.FatigueValue) : 0;
                    
                    // 该小时的使用记录
                    var hourRecords = hourlyRecords.Where(r => r.Hour == hour).ToList();
                    int activeMinutes = hourRecords.Sum(r => r.DurationSeconds) / 60;
                    
                    // 该小时最常用的应用
                    var topApp = hourRecords
                        .OrderByDescending(r => r.DurationSeconds)
                        .FirstOrDefault()?.AppName ?? "";
                    
                    heatmapData.Add((date, hour, avgFatigue, activeMinutes, topApp));
                }
            }
            
            // 更新热力图控件
            HeatmapView.LoadData(heatmapData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Analytics] LoadHeatmapData error: {ex.Message}");
        }
        
        // 加载时间轴数据
        await LoadTimelineDataAsync();
    }
    
    /// <summary>
    /// 加载今日工作流时间轴数据
    /// </summary>
    private async Task LoadTimelineDataAsync()
    {
        try
        {
            var sessions = new List<(DateTime Time, string AppName, string ClusterName, int DurationMinutes, bool IsFragmented)>();
            
            // 获取今日的使用记录
            var hourlyRecords = await _databaseService.GetHourlyUsageAsync(DateTime.Today);
            var clusterService = App.Services.GetRequiredService<Infrastructure.Services.ClusterService>();
            
            // 按小时分组，找出每小时的主要应用
            var recordsByHour = hourlyRecords
                .GroupBy(r => r.Hour)
                .OrderBy(g => g.Key)
                .ToList();
            
            foreach (var hourGroup in recordsByHour)
            {
                var topRecord = hourGroup.OrderByDescending(r => r.DurationSeconds).FirstOrDefault();
                if (topRecord == null) continue;
                
                // 获取 Cluster
                var cluster = clusterService.GetClusterForProcess(topRecord.AppName);
                var clusterName = cluster?.Name ?? "";
                
                // 检查是否为碎片时间（该小时内切换了多个应用）
                bool isFragmented = hourGroup.Count() > 3;
                
                sessions.Add((
                    DateTime.Today.AddHours(hourGroup.Key),
                    topRecord.AppName,
                    clusterName,
                    topRecord.DurationSeconds / 60,
                    isFragmented
                ));
            }
            
            TimelineView.LoadData(sessions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Analytics] LoadTimelineData error: {ex.Message}");
        }
    }
}
