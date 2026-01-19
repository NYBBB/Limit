using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using EyeGuard.Core.Interfaces;

namespace EyeGuard.Infrastructure.Monitors;

/// <summary>
/// 窗口追踪器实现。
/// 使用 Windows API 获取当前激活窗口信息。
/// </summary>
public class WindowTracker : IWindowTracker
{
    private System.Threading.Timer? _checkTimer;
    private WindowInfo? _lastWindowInfo;
    
    public event EventHandler<WindowInfo>? ActiveWindowChanged;

    public void Start()
    {
        // 每秒检查一次当前窗口
        _checkTimer = new System.Threading.Timer(CheckActiveWindow, null, 0, 1000);
    }

    public void Stop()
    {
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    public WindowInfo? GetActiveWindow()
    {
        return GetForegroundWindowInfo();
    }

    private void CheckActiveWindow(object? state)
    {
        var currentWindow = GetForegroundWindowInfo();
        if (currentWindow == null) return;

        bool changed = _lastWindowInfo == null || 
                       currentWindow.ProcessName != _lastWindowInfo.ProcessName ||
                       currentWindow.WindowTitle != _lastWindowInfo.WindowTitle;

        if (changed)
        {
            _lastWindowInfo = currentWindow;
            ActiveWindowChanged?.Invoke(this, currentWindow);
        }
    }

    private WindowInfo? GetForegroundWindowInfo()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            // 获取窗口标题
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            if (GetWindowText(hwnd, Buff, nChars) > 0)
            {
                string title = Buff.ToString();
                
                // 获取进程ID
                GetWindowThreadProcessId(hwnd, out uint pid);
                var process = Process.GetProcessById((int)pid);
                
                // 简单的敏感词过滤（实际上应该更复杂）
                string sanitized = SanitizeTitle(title);
                
                return new WindowInfo(process.ProcessName, title, sanitized);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting window info: {ex.Message}");
        }

        return null;
    }
    
    private string SanitizeTitle(string title)
    {
        // TODO: 实现更完善的脱敏逻辑
        if (title.Contains("密码") || title.Contains("Password"))
            return "***";
        return title;
    }
    
    public void Dispose()
    {
        Stop();
    }

    // P/Invoke
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
