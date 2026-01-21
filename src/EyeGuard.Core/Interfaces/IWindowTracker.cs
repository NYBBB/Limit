namespace EyeGuard.Core.Interfaces;

/// <summary>
/// 窗口信息记录。
/// </summary>
/// <param name="ProcessName">进程名称 (如 chrome.exe)。</param>
/// <param name="WindowTitle">窗口原始标题。</param>
/// <param name="SanitizedTitle">敏感词脱敏后的标题。</param>
/// <param name="Url">浏览器 URL（仅浏览器有效，通过 UI Automation 获取）。</param>
public record WindowInfo(
    string ProcessName, 
    string WindowTitle, 
    string SanitizedTitle,
    string? Url = null
);

/// <summary>
/// 窗口追踪接口 (F1.3)。
/// 负责记录当前激活窗口信息。
/// </summary>
public interface IWindowTracker : IDisposable
{
    /// <summary>
    /// 当前激活窗口变化时触发。
    /// </summary>
    event EventHandler<WindowInfo>? ActiveWindowChanged;
    
    /// <summary>
    /// 获取当前激活窗口信息。
    /// </summary>
    WindowInfo? GetActiveWindow();
    
    /// <summary>
    /// 启动追踪。
    /// </summary>
    void Start();
    
    /// <summary>
    /// 停止追踪。
    /// </summary>
    void Stop();
}
