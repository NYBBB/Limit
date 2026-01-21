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
    
    // ===== Phase 6: Energy Pie (精力分布饼图) =====
    [ObservableProperty]
    private ISeries[] _energyPieSeries = Array.Empty<ISeries>();
    
    // ===== Phase 6: The Grind 统计 =====
    [ObservableProperty]
    private int _longestWorkSession = 0;  // 今日最长连续工作（分钟）
    
    [ObservableProperty]
    private int _overloadMinutes = 0;  // 过载时间（分钟）
    
    [ObservableProperty]
    private double _overloadPercentage = 0;  // 过载占比
    
    [ObservableProperty]
    private int _totalActiveMinutes = 0;  // 总活跃时间
    
    // ===== Phase 6 P1: Daily Rhythm (日节奏图) =====
    [ObservableProperty]
    private ISeries[] _dailyRhythmSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _dailyRhythmXAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _dailyRhythmYAxes = Array.Empty<Axis>();
    
    // ===== Phase 6 P2: Weekly Trends (周趋势) =====
    [ObservableProperty]
    private ISeries[] _weeklyTrendsSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _weeklyTrendsXAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _weeklyTrendsYAxes = Array.Empty<Axis>();
    
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
    /// 加载指定日期的所有数据（应用使用 + 疲劳趋势 + Phase 6 数据）
    /// </summary>
    private async void LoadDataForDateAsync(DateTime date)
    {
        await LoadHourlyUsageAsync(date);
        await LoadFatigueTrendAsync(date);
        await LoadEnergyPieAsync(date);
        await LoadGrindStatisticsAsync(date);
        await LoadDailyRhythmAsync(date);
        await LoadWeeklyTrendsAsync(date);
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
    
    /// <summary>
    /// Phase 6: 加载精力分布饼图数据
    /// </summary>
    private async Task LoadEnergyPieAsync(DateTime date)
    {
        try
        {
            var records = await _databaseService.GetHourlyUsageAsync(date);
            
            if (records.Count == 0)
            {
                App.MainWindow.DispatcherQueue.TryEnqueue(() => EnergyPieSeries = Array.Empty<ISeries>());
                return;
            }
            
            // 按上下文分类聚合时长（使用 ContextClassifier）
            var contextDurations = new Dictionary<string, double>
            {
                { "工作/学习", 0 },
                { "娱乐", 0 },
                { "沟通", 0 },
                { "其他", 0 }
            };
            
            foreach (var record in records)
            {
                // 基于应用名分类
                var context = Infrastructure.Services.ContextClassifier.ClassifyApp(record.AppName);
                var contextName = context switch
                {
                    Core.Enums.ContextState.Work => "工作/学习",
                    Core.Enums.ContextState.Entertainment => "娱乐",
                    Core.Enums.ContextState.Communication => "沟通",
                    _ => "其他"
                };
                contextDurations[contextName] += record.DurationSeconds / 60.0;
            }
            
            // 创建饼图 Series
            var colors = new Dictionary<string, SKColor>
            {
                { "工作/学习", new SKColor(138, 43, 226) },  // 紫色
                { "娱乐", new SKColor(255, 140, 0) },        // 橙色
                { "沟通", new SKColor(0, 122, 204) },        // 蓝色
                { "其他", new SKColor(128, 128, 128) }       // 灰色
            };
            
            var pieSeries = contextDurations
                .Where(kv => kv.Value > 0)
                .Select(kv => new PieSeries<double>
                {
                    Name = kv.Key,
                    Values = new[] { kv.Value },
                    Fill = new SolidColorPaint(colors[kv.Key]),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsFormatter = point => $"{kv.Key}: {kv.Value:F0}分钟",
                    DataLabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150))
                    {
                        SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
                    }
                })
                .ToArray();
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                EnergyPieSeries = pieSeries;
                TotalActiveMinutes = (int)contextDurations.Values.Sum();
                Debug.WriteLine($"[Analytics] 精力分布: 工作{contextDurations["工作/学习"]:F0}min, 娱乐{contextDurations["娱乐"]:F0}min");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading energy pie: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Phase 6: 加载 Grind 统计（连续工作、过载时间）
    /// </summary>
    private async Task LoadGrindStatisticsAsync(DateTime date)
    {
        try
        {
            var snapshots = await _databaseService.GetFatigueSnapshotsAsync(date);
            
            if (snapshots.Count == 0)
            {
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    LongestWorkSession = 0;
                    OverloadMinutes = 0;
                    OverloadPercentage = 0;
                });
                return;
            }
            
            // 统计过载时间（疲劳 > 80%）
            int overloadCount = snapshots.Count(s => s.FatigueValue >= 80);
            int totalCount = snapshots.Count;
            
            // 估算过载分钟数（根据快照间隔）
            int snapshotIntervalMinutes = 5; // 默认假设 5 分钟间隔
            int overloadMins = overloadCount * snapshotIntervalMinutes;
            int totalMins = totalCount * snapshotIntervalMinutes;
            double overloadPct = totalMins > 0 ? (overloadMins * 100.0 / totalMins) : 0;
            
            // 最长连续工作估算（连续非低疲劳的记录数）
            int longestSession = 0;
            int currentSession = 0;
            foreach (var snapshot in snapshots.OrderBy(s => s.RecordedAt))
            {
                if (snapshot.FatigueValue > 20) // 疲劳 > 20% 认为在工作
                {
                    currentSession++;
                    longestSession = Math.Max(longestSession, currentSession);
                }
                else
                {
                    currentSession = 0;
                }
            }
            
            int longestMins = longestSession * snapshotIntervalMinutes;
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                LongestWorkSession = longestMins;
                OverloadMinutes = overloadMins;
                OverloadPercentage = overloadPct;
                Debug.WriteLine($"[Analytics] Grind统计: 最长连续{longestMins}min, 过载{overloadMins}min ({overloadPct:F1}%)");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading grind statistics: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Phase 6 P1: 加载日节奏图（24小时疲劳曲线）
    /// </summary>
    private async Task LoadDailyRhythmAsync(DateTime date)
    {
        try
        {
            var snapshots = await _databaseService.GetFatigueSnapshotsAsync(date);
            
            if (snapshots.Count == 0)
            {
                App.MainWindow.DispatcherQueue.TryEnqueue(() => DailyRhythmSeries = Array.Empty<ISeries>());
                return;
            }
            
            // 创建疲劳曲线数据
            var fatigueData = snapshots
                .Select(s => new ObservablePoint(
                    s.RecordedAt.Hour + s.RecordedAt.Minute / 60.0,
                    s.FatigueValue))
                .ToList();
            
            var series = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Name = "疲劳值",
                    Values = fatigueData,
                    Fill = new SolidColorPaint(new SKColor(138, 43, 226, 40)),
                    Stroke = new SolidColorPaint(new SKColor(138, 43, 226)) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.5
                }
            };
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                DailyRhythmSeries = series;
                InitializeDailyRhythmAxes();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading daily rhythm: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 初始化日节奏图坐标轴
    /// </summary>
    private void InitializeDailyRhythmAxes()
    {
        var labelPaint = new SolidColorPaint(new SKColor(150, 150, 150))
        {
            SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
        };
        
        DailyRhythmXAxes = new Axis[]
        {
            new Axis
            {
                Name = "时间",
                MinLimit = 0,
                MaxLimit = 24,
                ForceStepToMin = true,
                MinStep = 2,
                Labeler = value => $"{value:F0}:00",
                TextSize = 12,
                LabelsPaint = labelPaint,
                NamePaint = labelPaint
            }
        };
        
        DailyRhythmYAxes = new Axis[]
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
                NamePaint = labelPaint
            }
        };
    }
    
    /// <summary>
    /// Phase 6 P2: 加载周趋势图（7天疲劳对比）
    /// </summary>
    private async Task LoadWeeklyTrendsAsync(DateTime date)
    {
        try
        {
            var weekData = new List<(string Day, double Peak, double Avg)>();
            
            // 加载过去7天的数据
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = date.AddDays(-i);
                var snapshots = await _databaseService.GetFatigueSnapshotsAsync(targetDate);
                
                if (snapshots.Count > 0)
                {
                    double peak = snapshots.Max(s => s.FatigueValue);
                    double avg = snapshots.Average(s => s.FatigueValue);
                    string dayName = targetDate.ToString("MM/dd");
                    weekData.Add((dayName, peak, avg));
                }
                else
                {
                    string dayName = targetDate.ToString("MM/dd");
                    weekData.Add((dayName, 0, 0));
                }
            }
            
            // 创建峰值和平均值系列
            var peakValues = weekData.Select(d => d.Peak).ToArray();
            var avgValues = weekData.Select(d => d.Avg).ToArray();
            var labels = weekData.Select(d => d.Day).ToArray();
            
            var series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "峰值",
                    Values = peakValues,
                    Fill = new SolidColorPaint(new SKColor(255, 140, 0)),
                    MaxBarWidth = 40
                },
                new ColumnSeries<double>
                {
                    Name = "平均值",
                    Values = avgValues,
                    Fill = new SolidColorPaint(new SKColor(138, 43, 226)),
                    MaxBarWidth = 40
                }
            };
            
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                WeeklyTrendsSeries = series;
                InitializeWeeklyTrendsAxes(labels);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading weekly trends: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 初始化周趋势图坐标轴
    /// </summary>
    private void InitializeWeeklyTrendsAxes(string[] labels)
    {
        var labelPaint = new SolidColorPaint(new SKColor(150, 150, 150))
        {
            SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
        };
        
        WeeklyTrendsXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                TextSize = 12,
                LabelsPaint = labelPaint
            }
        };
        
        WeeklyTrendsYAxes = new Axis[]
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
                NamePaint = labelPaint
            }
        };
    }
}
