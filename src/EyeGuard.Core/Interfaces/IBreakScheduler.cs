using EyeGuard.Core.Enums;

namespace EyeGuard.Core.Interfaces;

/// <summary>
/// 休息事件参数。
/// </summary>
public class BreakEventArgs : EventArgs
{
    /// <summary>
    /// 休息类型。
    /// </summary>
    public required BreakType BreakType { get; init; }
    
    /// <summary>
    /// 建议休息时长。
    /// </summary>
    public required TimeSpan Duration { get; init; }
}

/// <summary>
/// 休息调度器接口 (F2.2, F2.3)。
/// 负责管理休息提醒时机和遮罩控制。
/// </summary>
public interface IBreakScheduler : IDisposable
{
    /// <summary>
    /// 距离下次休息的时间。
    /// </summary>
    TimeSpan TimeToNextBreak { get; }
    
    /// <summary>
    /// 下次休息类型。
    /// </summary>
    BreakType NextBreakType { get; }
    
    /// <summary>
    /// 调度器是否正在运行。
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 需要休息时触发。
    /// </summary>
    event EventHandler<BreakEventArgs>? BreakRequired;
    
    /// <summary>
    /// 休息结束时触发。
    /// </summary>
    event EventHandler? BreakCompleted;
    
    /// <summary>
    /// 启动调度器。
    /// </summary>
    void Start();
    
    /// <summary>
    /// 停止调度器。
    /// </summary>
    void Stop();
    
    /// <summary>
    /// 跳过当前休息。
    /// </summary>
    void Skip();
    
    /// <summary>
    /// 推迟休息指定时长。
    /// </summary>
    /// <param name="duration">推迟时长 (默认5分钟)。</param>
    void Snooze(TimeSpan? duration = null);
    
    /// <summary>
    /// 重置计时器 (用于检测到用户输入后)。
    /// </summary>
    void ResetTimer();
}
