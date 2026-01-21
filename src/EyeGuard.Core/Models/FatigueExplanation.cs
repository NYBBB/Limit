namespace EyeGuard.Core.Models;

/// <summary>
/// 疲劳变化解释记录 - 用于 UI 显示 "为什么累" 和调试
/// </summary>
public record FatigueExplanation
{
    /// <summary>
    /// 基础曲线贡献值
    /// </summary>
    public double BaseCurve { get; init; }
    
    /// <summary>
    /// 上下文负荷权重 (Work=1.0, Entertainment=0.3)
    /// </summary>
    public double LoadWeight { get; init; }
    
    /// <summary>
    /// 恢复加成 (来自 Idle/Away/BreakTask)
    /// </summary>
    public double RecoveryCredit { get; init; }
    
    /// <summary>
    /// 本次疲劳变化量
    /// </summary>
    public double FatigueDelta { get; init; }
    
    /// <summary>
    /// 原因代码 (用于 UI 显示)
    /// </summary>
    public string ReasonCode { get; init; } = string.Empty;
    
    /// <summary>
    /// 原因描述 (用户友好)
    /// </summary>
    public string ReasonText { get; init; } = string.Empty;
}
