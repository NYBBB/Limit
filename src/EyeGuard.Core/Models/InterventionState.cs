namespace EyeGuard.Core.Models;

/// <summary>
/// 干预状态 - 描述当前的干预信息
/// </summary>
public record InterventionState
{
    /// <summary>
    /// 干预级别
    /// </summary>
    public required Enums.InterventionLevel Level { get; init; }
    
    /// <summary>
    /// 提示消息
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// 操作按钮文本（可选）
    /// </summary>
    public string? ActionText { get; init; }
    
    /// <summary>
    /// 触发时间
    /// </summary>
    public DateTime TriggeredAt { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 是否需要显示
    /// </summary>
    public bool ShouldShow => Level != Enums.InterventionLevel.None;
}
