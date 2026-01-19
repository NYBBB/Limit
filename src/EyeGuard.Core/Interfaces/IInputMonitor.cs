namespace EyeGuard.Core.Interfaces;

/// <summary>
/// 输入检测事件参数。
/// </summary>
public class InputEventArgs : EventArgs
{
    /// <summary>
    /// 输入类型 (Mouse/Keyboard)。
    /// </summary>
    public required string InputType { get; init; }
    
    /// <summary>
    /// 事件发生时间。
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// 输入监测接口 (F1.1)。
/// 负责监听全局键盘/鼠标事件。
/// </summary>
public interface IInputMonitor : IDisposable
{
    /// <summary>
    /// 当检测到用户输入时触发。
    /// </summary>
    event EventHandler<InputEventArgs>? InputDetected;
    
    /// <summary>
    /// 监测器是否正在运行。
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 启动监测。
    /// </summary>
    void Start();
    
    /// <summary>
    /// 停止监测。
    /// </summary>
    void Stop();
}
