using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EyeGuard.Core.Entities;
using EyeGuard.Core.Enums;

namespace EyeGuard.Infrastructure.Services;

/// <summary>
/// 数据聚合服务 - Phase 6
/// 将热数据（7天内）聚合为冷数据（DailyAggregateRecord）
/// </summary>
public class DataAggregationService
{
    private readonly DatabaseService _databaseService;
    private readonly ClusterService _clusterService;
    
    // 配置
    private const int HotDataDays = 7;           // 热数据保留天数
    private const int MinFragmentMinutes = 5;    // 最小碎片时长（分钟）
    
    // 标记是否已执行过今日聚合
    private DateTime _lastAggregationDate = DateTime.MinValue;
    
    public DataAggregationService(DatabaseService databaseService, ClusterService clusterService)
    {
        _databaseService = databaseService;
        _clusterService = clusterService;
    }

    /// <summary>
    /// 执行每日聚合（建议在凌晨或应用启动时执行）
    /// </summary>
    public async Task RunDailyAggregationAsync()
    {
        try
        {
            var today = DateTime.Today;
            
            // 防止同一天重复执行
            if (_lastAggregationDate == today)
            {
                Debug.WriteLine($"[Aggregation] Already aggregated today, skipping.");
                return;
            }
            
            var coldDataCutoff = today.AddDays(-HotDataDays);
            
            Debug.WriteLine($"[Aggregation] Starting aggregation for data before {coldDataCutoff:yyyy-MM-dd}");
            
            // 获取需要聚合的日期列表
            var datesToAggregate = await GetDatesToAggregateAsync(coldDataCutoff);
            
            foreach (var date in datesToAggregate)
            {
                await AggregateDay(date);
            }
            
            // 清理旧的疲劳快照（保留 7 天）
            await _databaseService.DeleteOldFatigueSnapshotsAsync(coldDataCutoff);
            
            _lastAggregationDate = today;
            Debug.WriteLine($"[Aggregation] Completed. Aggregated {datesToAggregate.Count} days.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Aggregation] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取需要聚合的日期列表
    /// </summary>
    private async Task<List<DateTime>> GetDatesToAggregateAsync(DateTime cutoffDate)
    {
        // 获取所有早于 cutoffDate 的 HourlyUsageRecord 日期
        var records = await _databaseService.GetAllHourlyUsageAsync();
        
        return records
            .Where(r => r.Date.Date < cutoffDate)
            .Select(r => r.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    /// <summary>
    /// 聚合指定日期的数据
    /// </summary>
    private async Task AggregateDay(DateTime date)
    {
        try
        {
            // 1. 获取该日的所有 HourlyUsageRecord
            var hourlyRecords = await _databaseService.GetHourlyUsageAsync(date);
            
            if (hourlyRecords == null || hourlyRecords.Count == 0)
            {
                Debug.WriteLine($"[Aggregation] No data for {date:yyyy-MM-dd}, skipping");
                return;
            }
            
            // 2. 获取疲劳快照
            var fatigueSnapshots = await _databaseService.GetFatigueSnapshotsAsync(date);
            
            // 3. 获取休息记录
            var breakTasks = await _databaseService.GetBreakTasksForDateAsync(date);
            
            // 4. 计算聚合指标
            var aggregate = await CalculateAggregateAsync(date, hourlyRecords, fatigueSnapshots, breakTasks);
            
            // 5. 保存聚合记录
            await _databaseService.SaveDailyAggregateAsync(aggregate);
            
            // 6. 删除原始热数据
            await _databaseService.DeleteHourlyUsageByDateAsync(date);
            await _databaseService.DeleteBreakTasksByDateAsync(date);
            
            Debug.WriteLine($"[Aggregation] Aggregated {date:yyyy-MM-dd}: {aggregate.TotalActiveMinutes} mins, Peak={aggregate.PeakFatigue:F0}%, Grade={aggregate.Grade}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Aggregation] Failed to aggregate {date:yyyy-MM-dd}: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算聚合指标
    /// </summary>
    private Task<DailyAggregateRecord> CalculateAggregateAsync(
        DateTime date, 
        List<HourlyUsageRecord> records,
        List<FatigueSnapshot> fatigueSnapshots,
        List<BreakTaskRecord> breakTasks)
    {
        var totalMinutes = records.Sum(r => r.DurationSeconds) / 60;
        
        // ----- 疲劳统计 -----
        double peakFatigue = 0;
        double averageFatigue = 50;
        
        if (fatigueSnapshots.Count > 0)
        {
            peakFatigue = fatigueSnapshots.Max(s => s.FatigueValue);
            averageFatigue = fatigueSnapshots.Average(s => s.FatigueValue);
        }
        
        // ----- 休息统计 -----
        int breakCount = breakTasks.Count(b => b.Result == BreakTaskResult.Completed);
        int skippedBreakCount = breakTasks.Count(b => b.Result == BreakTaskResult.Skipped || b.Result == BreakTaskResult.Snoozed);
        int totalRestMinutes = breakTasks
            .Where(b => b.Result == BreakTaskResult.Completed)
            .Sum(b => b.DurationSeconds) / 60;
        
        // ----- 应用分析 -----
        var appGroups = records
            .GroupBy(r => r.AppName)
            .Select(g => new { App = g.Key, Minutes = g.Sum(r => r.DurationSeconds) / 60 })
            .OrderByDescending(x => x.Minutes)
            .ToList();
        
        var topApp = appGroups.FirstOrDefault();
        
        // ----- Cluster 分布 -----
        var clusterDist = new Dictionary<string, int>();
        
        foreach (var appGroup in appGroups.Take(10))
        {
            var cluster = _clusterService.GetClusterForProcess(appGroup.App);
            var clusterName = cluster?.Name ?? "未分类";
            
            if (clusterDist.ContainsKey(clusterName))
                clusterDist[clusterName] += appGroup.Minutes;
            else
                clusterDist[clusterName] = appGroup.Minutes;
        }
        
        // 保留 Top 5 Clusters
        clusterDist = clusterDist
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        
        // ----- 评分计算 -----
        // 综合考虑：工作时长、疲劳控制、休息完成率
        var grade = CalculateGrade(totalMinutes, peakFatigue, breakCount, skippedBreakCount);
        
        var result = new DailyAggregateRecord
        {
            Date = date.Date,
            TotalActiveMinutes = totalMinutes,
            TotalWorkMinutes = totalMinutes - totalRestMinutes,
            TotalRestMinutes = totalRestMinutes,
            PeakFatigue = peakFatigue,
            AverageFatigue = averageFatigue,
            BreakCount = breakCount,
            SkippedBreakCount = skippedBreakCount,
            TopApp = topApp?.App ?? "",
            TopAppMinutes = topApp?.Minutes ?? 0,
            ClusterDistributionJson = JsonSerializer.Serialize(clusterDist),
            Grade = grade,
            CreatedAt = DateTime.Now
        };
        
        return Task.FromResult(result);
    }
    
    /// <summary>
    /// 计算评分等级 (S/A/B/C)
    /// </summary>
    private string CalculateGrade(int totalMinutes, double peakFatigue, int breakCount, int skippedBreakCount)
    {
        int score = 100;
        
        // 工作时长惩罚
        if (totalMinutes > 600) score -= 30;      // >10h: -30
        else if (totalMinutes > 480) score -= 15; // >8h: -15
        else if (totalMinutes > 360) score -= 5;  // >6h: -5
        
        // 峰值疲劳惩罚
        if (peakFatigue > 90) score -= 25;
        else if (peakFatigue > 80) score -= 15;
        else if (peakFatigue > 70) score -= 5;
        
        // 跳过休息惩罚
        score -= skippedBreakCount * 10;
        
        // 完成休息奖励
        score += breakCount * 5;
        
        return score switch
        {
            >= 90 => "S",
            >= 75 => "A",
            >= 50 => "B",
            _ => "C"
        };
    }

    /// <summary>
    /// 压缩碎片数据（<5分钟的非工作记录合并到"其他"）
    /// </summary>
    public async Task CompressFragmentsAsync(DateTime date)
    {
        var records = await _databaseService.GetHourlyUsageAsync(date);
        
        // 找出碎片记录（<5分钟）
        var fragments = records.Where(r => r.DurationSeconds < MinFragmentMinutes * 60).ToList();
        
        if (fragments.Count <= 1)
            return;
        
        // 将碎片合并为"其他"
        var totalFragmentSeconds = fragments.Sum(r => r.DurationSeconds);
        
        // 删除碎片记录
        foreach (var fragment in fragments)
        {
            await _databaseService.DeleteHourlyUsageByIdAsync(fragment.Id);
        }
        
        // 创建合并记录
        Debug.WriteLine($"[Aggregation] Compressed {fragments.Count} fragments ({totalFragmentSeconds / 60} mins) for {date:yyyy-MM-dd}");
    }
    
    /// <summary>
    /// 获取历史聚合数据（用于 Analytics 页面）
    /// </summary>
    public async Task<List<DailyAggregateRecord>> GetHistoryAggregatesAsync(int days = 30)
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-days);
        
        return await _databaseService.GetDailyAggregatesAsync(startDate, endDate);
    }
}
