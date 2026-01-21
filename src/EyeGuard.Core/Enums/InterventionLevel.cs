namespace EyeGuard.Core.Enums;

/// <summary>
/// 干预级别 - 根据疲劳程度决定干预强度
/// </summary>
public enum InterventionLevel
{
    /// <summary>
    /// 无需干预 - 疲劳度 0-40%
    /// </summary>
    None,
    
    /// <summary>
    /// 轻推 - 疲劳度 40-60%
    /// 微小提示，不打断工作（图标变色、状态栏小字）
    /// </summary>
    Nudge,
    
    /// <summary>
    /// 建议 - 疲劳度 60-80%
    /// 明显提示，建议休息（Dashboard 卡片、声音提示）
    /// </summary>
    Suggestion,
    
    /// <summary>
    /// 干预 - 疲劳度 80%+
    /// 强力提醒，触发休息任务
    /// </summary>
    Intervention
}
