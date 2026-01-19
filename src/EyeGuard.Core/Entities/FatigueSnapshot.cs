using System;
using SQLite;

namespace EyeGuard.Core.Entities;

/// <summary>
/// 疲劳值快照实体 - 用于记录某个时间点的疲劳值，支持历史趋势查看
/// </summary>
[Table("FatigueSnapshots")]
public class FatigueSnapshot
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    /// <summary>
    /// 日期（仅日期部分，用于按日分组）
    /// </summary>
    [Indexed]
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 疲劳值（0-100）
    /// </summary>
    public double FatigueValue { get; set; }
    
    /// <summary>
    /// 实际记录时间（包含时分秒，用于精确绘图）
    /// </summary>
    public DateTime RecordedAt { get; set; }
}
