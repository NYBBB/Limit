using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyeGuard.Core.Entities;
using EyeGuard.Infrastructure.Services;
using EyeGuard.UI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace EyeGuard.UI.ViewModels;

/// <summary>
/// 分析页面 ViewModel - 负责加载和展示历史数据、24小时柱状图等
/// </summary>
public partial class AnalyticsViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    
    // 日期选择
    [ObservableProperty]
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
    
    [ObservableProperty]
    private string _selectedDateText = "今天";
    
    // 应用使用柱状图
    [ObservableProperty]
    private ISeries[] _hourlyUsageSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _usageXAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _usageYAxes = Array.Empty<Axis>();
    
    // 疲劳趋势折线图
    [ObservableProperty]
    private ISeries[] _fatigueTrendSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _fatigueXAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _fatigueYAxes = Array.Empty<Axis>();
    
    private readonly ObservableCollection<ObservablePoint> _fatigueData = new();
    
    // 图例字体
    public SolidColorPaint LegendPaint { get; } = new SolidColorPaint(new SKColor(150, 150, 150))
    {
        SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
    };
    
    public AnalyticsViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        
        // 初始化坐标轴
        InitializeAxes();
        
        // 异步加载今日数据
        LoadDataForDateAsync(DateTime.Today);
    }
    
    partial void OnSelectedDateChanged(DateTimeOffset value)
    {
        // 日期改变时重新加载数据
        var date = value.Date;
        UpdateDateText(date);
        LoadDataForDateAsync(date);
    }
    
    private void UpdateDateText(DateTime date)
    {
        if (date.Date == DateTime.Today)
            SelectedDateText = "今天";
        else if (date.Date == DateTime.Today.AddDays(-1))
            SelectedDateText = "昨天";
        else
            SelectedDateText = date.ToString("yyyy-MM-dd");
    }
    
    private void InitializeAxes()
    {
        // 创建支持中文的字体
        var labelPaint = new SolidColorPaint(new SKColor(150, 150, 150))
        {
            SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
        };
        
        // 应用使用柱状图坐标轴
        UsageXAxes = new Axis[]
        {
            new Axis
            {
                Name = "小时",
                MinLimit = 0,
                MaxLimit = 23,
                ForceStepToMin = true,
                MinStep = 1,
                Labeler = value => $"{value:F0}:00",
                TextSize = 12,
                LabelsPaint = labelPaint,
                NamePaint = labelPaint,
            }
        };
        
        UsageYAxes = new Axis[]
        {
            new Axis
            {
                Name = "使用时长 (分钟)",
                MinLimit = 0,
                MinStep = 10,
                Labeler = value => $"{value:F0}",
                TextSize = 12,
                LabelsPaint = labelPaint,
                NamePaint = labelPaint,
            }
        };
        
        // 疲劳趋势折线图坐标轴
        FatigueXAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 24,
                ForceStepToMin = true,
                MinStep = 2,
                Labeler = value => value.ToString(),
                TextSize = 12,
                LabelsPaint = labelPaint,
            }
        };
        
        FatigueYAxes = new Axis[]
        {
            new Axis
            {
                Name = "疲劳度",
                MinLimit = 0,
                MaxLimit = 100,
                MinStep = 20,
                Labeler = value => $"{value}%",
                TextSize = 12,
                LabelsPaint = labelPaint,
                NamePaint = labelPaint,
            }
        };
        
        // 初始化疲劳趋势 Series
        FatigueTrendSeries = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = _fatigueData,
                Name = "疲劳值",
                Fill = new SolidColorPaint(new SKColor(138, 43, 226, 40)),
                Stroke = new SolidColorPaint(new SKColor(138, 43, 226)) { StrokeThickness = 3 },
                GeometrySize = 8,  // 减小数据点大小
                GeometryFill = new SolidColorPaint(new SKColor(138, 43, 226)),
                GeometryStroke = null,  // 移除白色描边
                LineSmoothness = 0.3,
            }
        };
    }
    
    [RelayCommand]
    private void SelectToday() => SelectedDate = DateTimeOffset.Now;
    
    [RelayCommand]
    private void SelectYesterday() => SelectedDate = DateTimeOffset.Now.AddDays(-1);
    
    [RelayCommand]
    private void SelectWeekAgo() => SelectedDate = DateTimeOffset.Now.AddDays(-7);
    
    /// <summary>
    /// 加载指定日期的所有数据（应用使用 + 疲劳趋势）
    /// </summary>
    private async void LoadDataForDateAsync(DateTime date)
    {
        await LoadHourlyUsageAsync(date);
        await LoadFatigueTrendAsync(date);
    }
    
    /// <summary>
    /// 加载指定日期的每小时使用数据并生成堆叠柱状图
    /// </summary>
    private async Task LoadHourlyUsageAsync(DateTime date)
    {
        try
        {
            var records = await _databaseService.GetHourlyUsageAsync(date);
            
            if (records.Count == 0)
            {
                App.MainWindow.DispatcherQueue.TryEnqueue(() => HourlyUsageSeries = Array.Empty<ISeries>());
                Debug.WriteLine($"[Analytics] {date:yyyy-MM-dd} 暂无每小时使用记录");
                return;
            }
            
            // 1. 计算全天各应用总时长，找出 Top 8
            var appTotalDurations = records
                .GroupBy(r => r.AppName)
                .Select(g => new { AppName = g.Key, TotalSeconds = g.Sum(r => r.DurationSeconds) })
                .OrderByDescending(x => x.TotalSeconds)
                .ToList();
            
            var top8Apps = appTotalDurations.Take(8).Select(x => x.AppName).ToList();
            
            // 2. 为每个 Top 8 应用创建一个 StackedColumnSeries
            var seriesList = new List<ISeries>();
            var colors = new[] 
            { 
                new SKColor(138, 43, 226), new SKColor(0, 122, 204), new SKColor(255, 140, 0), new SKColor(34, 139, 34),
                new SKColor(220, 20, 60), new SKColor(255, 215, 0), new SKColor(0, 191, 255), new SKColor(255, 105, 180)
            };
            
            for (int i = 0; i < top8Apps.Count; i++)
            {
                var appName = top8Apps[i];
                var values = new double[24];
                
                foreach (var record in records.Where(r => r.AppName == appName))
                {
                    values[record.Hour] = record.DurationSeconds / 60.0;
                }
                
                seriesList.Add(new StackedColumnSeries<double>
                {
                    Name = IconMapper.GetFriendlyName(appName),
                    Values = values,
                    Stroke = null,
                    Fill = new SolidColorPaint(colors[i]),
                    MaxBarWidth = 30,
                });
            }
            
            // 3. "其他" 应用的聚合
            var othersValues = new double[24];
            foreach (var record in records.Where(r => !top8Apps.Contains(r.AppName)))
            {
                othersValues[record.Hour] += record.DurationSeconds / 60.0;
            }
            
            if (othersValues.Any(v => v > 0))
            {
                seriesList.Add(new StackedColumnSeries<double>
                {
                    Name = "其他",
                    Values = othersValues,
                    Stroke = null,
                    Fill = new SolidColorPaint(new SKColor(128, 128, 128)),
                    MaxBarWidth = 30,
                });
            }
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                HourlyUsageSeries = seriesList.ToArray();
                Debug.WriteLine($"[Analytics] 已加载 {date:yyyy-MM-dd} 的 {records.Count} 条每小时记录");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading hourly usage: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 加载指定日期的疲劳趋势数据
    /// </summary>
    private async Task LoadFatigueTrendAsync(DateTime date)
    {
        try
        {
            var snapshots = await _databaseService.GetFatigueSnapshotsAsync(date);
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                _fatigueData.Clear();
                foreach (var snapshot in snapshots)
                {
                    var hour = snapshot.RecordedAt.Hour;
                    var minuteFraction = snapshot.RecordedAt.Minute / 60.0;
                    var hourPosition = hour + minuteFraction;
                    _fatigueData.Add(new ObservablePoint(hourPosition, snapshot.FatigueValue));
                }
                Debug.WriteLine($"[Analytics] 已加载 {date:yyyy-MM-dd} 的 {snapshots.Count} 个疲劳快照");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading fatigue trend: {ex.Message}");
        }
    }
}
