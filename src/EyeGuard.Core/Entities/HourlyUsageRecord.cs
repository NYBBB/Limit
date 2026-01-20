using System;
using SQLite;

namespace EyeGuard.Core.Entities;

/// <summary>
/// 每小时使用记录实体 - 按小时聚合各应用的使用时长，用于分析页面的柱状图展示
/// </summary>
[Table("HourlyUsageRecords")]
public class HourlyUsageRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    /// <summary>
    /// 日期（仅日期部分，用于按日分组）
    /// </summary>
    [Indexed]
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 小时（0-23）
    /// </summary>
    public int Hour { get; set; }
    
    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; set; } = string.Empty;
    
    /// <summary>
    /// 该小时内的使用时长（秒）
    /// </summary>
    public int DurationSeconds { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
