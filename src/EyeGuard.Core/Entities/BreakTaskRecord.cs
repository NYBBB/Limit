using System;
using SQLite;
using EyeGuard.Core.Enums;

namespace EyeGuard.Core.Entities;

/// <summary>
/// 休息任务记录实体 - 用于记录和追踪休息任务的完成情况
/// </summary>
[Table("BreakTaskRecords")]
public class BreakTaskRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    /// <summary>
    /// 任务创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 任务类型
    /// </summary>
    public BreakTaskType TaskType { get; set; }
    
    /// <summary>
    /// 任务时长（秒）
    /// </summary>
    public int DurationSeconds { get; set; }
    
    /// <summary>
    /// 触发原因
    /// </summary>
    public string TriggerReason { get; set; } = string.Empty;
    
    /// <summary>
    /// 完成时间（null 表示未完成）
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// 任务结果
    /// </summary>
    public BreakTaskResult Result { get; set; } = BreakTaskResult.Pending;
    
    /// <summary>
    /// 恢复加成值（完成任务后获得的疲劳恢复量）
    /// </summary>
    public double RecoveryCredit { get; set; }
    
    /// <summary>
    /// 任务触发时的疲劳值
    /// </summary>
    public double FatigueAtTrigger { get; set; }
}
