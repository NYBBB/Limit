namespace EyeGuard.Core.Enums;

/// <summary>
/// 用户当前状态枚举。
/// </summary>
public enum UserState
{
    /// <summary>
    /// 活跃状态 - 用户正在使用电脑。
    /// </summary>
    Active,
    
    /// <summary>
    /// 空闲状态 - 超过指定时间无输入。
    /// </summary>
    Idle,
    
    /// <summary>
    /// 休息状态 - 正在进行休息提醒。
    /// </summary>
    Break
}
