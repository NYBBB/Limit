namespace EyeGuard.Core.Interfaces;

/// <summary>
/// 疲劳引擎接口 (F2.1)。
/// 负责计算和维护用户疲劳值。
/// </summary>
public interface IFatigueEngine
{
    /// <summary>
    /// 当前疲劳值 (0-100)。
    /// </summary>
    int CurrentFatigue { get; }
    
    /// <summary>
    /// 疲劳值变化时触发。
    /// </summary>
    event EventHandler<int>? FatigueChanged;
    
    /// <summary>
    /// 记录工作时间。
    /// 规则: 工作 1 分钟 → 疲劳值 +1%。
    /// </summary>
    /// <param name="duration">工作时长。</param>
    void RecordWorkTime(TimeSpan duration);
    
    /// <summary>
    /// 记录休息时间。
    /// 规则: 休息 1 分钟 → 疲劳值 -20%。
    /// </summary>
    /// <param name="duration">休息时长。</param>
    void RecordRestTime(TimeSpan duration);
    
    /// <summary>
    /// 重置疲劳值到0。
    /// </summary>
    void Reset();
}
