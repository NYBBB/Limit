using CommunityToolkit.Mvvm.ComponentModel;
using EyeGuard.Core.Entities;
using EyeGuard.Infrastructure.Services;
using EyeGuard.UI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace EyeGuard.UI.ViewModels;

/// <summary>
/// 分析页面 ViewModel - 负责加载和展示历史数据、24小时柱状图等
/// </summary>
public partial class AnalyticsViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    
    [ObservableProperty]
    private ISeries[] _hourlyUsageSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _yAxes = Array.Empty<Axis>();
    
    public AnalyticsViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        
        // 初始化坐标轴
        InitializeAxes();
        
        // 异步加载数据
        LoadTodayHourlyDataAsync();
    }
    
    private void InitializeAxes()
    {
        // 创建支持中文的字体
        var labelPaint = new SolidColorPaint(new SKColor(150, 150, 150))
        {
            SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
        };
        
        XAxes = new Axis[]
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
        
        YAxes = new Axis[]
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
    }
    
    /// <summary>
    /// 加载今日每小时使用数据并生成堆叠柱状图
    /// </summary>
    private async void LoadTodayHourlyDataAsync()
    {
        try
        {
            var records = await _databaseService.GetHourlyUsageAsync(DateTime.Today);
            
            if (records.Count == 0)
            {
                Debug.WriteLine("[Analytics] 今日暂无每小时使用记录");
                return;
            }
            
            // 1. 计算全天各应用总时长，找出 Top 8（覆盖更多每小时的重要应用）
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
                new SKColor(138, 43, 226),   // 紫色
                new SKColor(0, 122, 204),     // 蓝色
                new SKColor(255, 140, 0),     // 橙色
                new SKColor(34, 139, 34),     // 绿色
                new SKColor(220, 20, 60),     // 深红
                new SKColor(255, 215, 0),     // 金色
                new SKColor(0, 191, 255),     // 深天蓝
                new SKColor(255, 105, 180)    // 粉色
            };
            
            for (int i = 0; i < top8Apps.Count; i++)
            {
                var appName = top8Apps[i];
                var values = new double[24];
                
                // 填充该应用在各小时的使用时长（转为分钟）
                foreach (var record in records.Where(r => r.AppName == appName))
                {
                    values[record.Hour] = record.DurationSeconds / 60.0;
                }
                
                seriesList.Add(new StackedColumnSeries<double>
                {
                    Name = IconMapper.GetFriendlyName(appName),  // 使用友好名称
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
            
            // 如果有"其他"数据，添加一个灰色 Series
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
            
            // 4. 更新到 UI
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                HourlyUsageSeries = seriesList.ToArray();
                Debug.WriteLine($"[Analytics] 已加载 {records.Count} 条每小时记录，生成 {seriesList.Count} 个 Series");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading hourly data: {ex.Message}");
        }
    }
}
