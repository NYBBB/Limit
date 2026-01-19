namespace EyeGuard.Core.Models;

/// <summary>
/// 用户活动状态枚举。
/// </summary>
public enum UserActivityState
{
    /// <summary>
    /// 活跃状态 - 有输入，无音频或有音频都算。
    /// </summary>
    Active,
    
    /// <summary>
    /// 媒体模式 - 有音频播放，但无输入（看视频/听音乐）。
    /// 计时但疲劳增长减缓。
    /// </summary>
    MediaMode,
    
    /// <summary>
    /// 空闲状态 - 无输入超过阈值，开始恢复疲劳。
    /// </summary>
    Idle,
    
    /// <summary>
    /// 离开状态 - 长时间无活动，认为用户离开了。
    /// </summary>
    Away
}
