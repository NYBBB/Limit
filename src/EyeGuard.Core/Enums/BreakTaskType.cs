namespace EyeGuard.Core.Enums;

/// <summary>
/// 休息任务类型
/// </summary>
public enum BreakTaskType
{
    /// <summary>
    /// 护眼任务 - 20-20-20 法则 (20秒)
    /// 每20分钟看20英尺外的物体20秒
    /// </summary>
    Eye,
    
    /// <summary>
    /// 呼吸放空任务 (30秒)
    /// </summary>
    Breath,
    
    /// <summary>
    /// 站立/走动任务 (60-120秒) - 用于久坐保护
    /// </summary>
    Mobility,
    
    /// <summary>
    /// 肩颈拉伸任务 (30秒)
    /// </summary>
    Stretch
}
