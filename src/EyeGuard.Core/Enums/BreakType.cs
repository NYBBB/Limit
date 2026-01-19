namespace EyeGuard.Core.Enums;

/// <summary>
/// 休息类型枚举。
/// </summary>
public enum BreakType
{
    /// <summary>
    /// 短休息 - 每15分钟提示20秒，缓解视疲劳。
    /// </summary>
    Micro,
    
    /// <summary>
    /// 长休息 - 每45分钟提示5分钟，站立/走动。
    /// </summary>
    Long
}
