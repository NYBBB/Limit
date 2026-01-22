using System;
using SQLite;

namespace EyeGuard.Core.Entities;

/// <summary>
/// 每日聚合记录实体 - 用于冷数据存储，压缩历史数据
/// Phase 6: 数据架构优化
/// </summary>
[Table("DailyAggregateRecords")]
public class DailyAggregateRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    /// <summary>
    /// 日期
    /// </summary>
    [Indexed]
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 总活跃时间（分钟）
    /// </summary>
    public int TotalActiveMinutes { get; set; }
    
    /// <summary>
    /// 总工作时间（分钟）
    /// </summary>
    public int TotalWorkMinutes { get; set; }
    
    /// <summary>
    /// 总休息时间（分钟）
    /// </summary>
    public int TotalRestMinutes { get; set; }
    
    /// <summary>
    /// 峰值疲劳度 (0-100)
    /// </summary>
    public double PeakFatigue { get; set; }
    
    /// <summary>
    /// 平均疲劳度 (0-100)
    /// </summary>
    public double AverageFatigue { get; set; }
    
    /// <summary>
    /// 休息次数
    /// </summary>
    public int BreakCount { get; set; }
    
    /// <summary>
    /// 跳过休息次数
    /// </summary>
    public int SkippedBreakCount { get; set; }
    
    /// <summary>
    /// 最常用应用（Top 1）
    /// </summary>
    public string TopApp { get; set; } = "";
    
    /// <summary>
    /// 最常用应用使用时间（分钟）
    /// </summary>
    public int TopAppMinutes { get; set; }
    
    /// <summary>
    /// Cluster 分布 JSON（{ "Coding": 120, "Writing": 60, ... }）
    /// </summary>
    public string ClusterDistributionJson { get; set; } = "{}";
    
    /// <summary>
    /// 评分等级 (S/A/B/C)
    /// </summary>
    public string Grade { get; set; } = "B";
    
    /// <summary>
    /// 记录创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
