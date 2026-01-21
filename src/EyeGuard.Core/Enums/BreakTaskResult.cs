namespace EyeGuard.Core.Enums;

/// <summary>
/// 休息任务结果
/// </summary>
public enum BreakTaskResult
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed,
    
    /// <summary>
    /// 已推迟
    /// </summary>
    Snoozed,
    
    /// <summary>
    /// 已跳过
    /// </summary>
    Skipped
}
