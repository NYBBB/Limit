namespace EyeGuard.Core.Enums;

/// <summary>
/// 疲劳状态等级 - 用于决定干预强度和 UI 显示
/// </summary>
public enum FatigueState
{
    /// <summary>
    /// 精力充沛 (疲劳值 < 30%)
    /// </summary>
    Fresh,
    
    /// <summary>
    /// 略感紧张 (疲劳值 30-60%)
    /// </summary>
    Strained,
    
    /// <summary>
    /// 超负荷 (疲劳值 60-85%)
    /// </summary>
    Overloaded,
    
    /// <summary>
    /// 垃圾时间 - 强行工作 (疲劳值 > 85%)
    /// </summary>
    Grind
}
