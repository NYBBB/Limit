using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EyeGuard.Core.Interfaces;
using EyeGuard.Infrastructure.Automation;
using EyeGuard.Infrastructure.Services;

namespace EyeGuard.Infrastructure.Monitors;

/// <summary>
/// 窗口追踪器实现。
/// 使用 Windows API 获取当前激活窗口信息。
/// Limit 3.0 性能优化: PID 缓存 + URL 防抖机制
/// </summary>
public class WindowTracker : IWindowTracker
{
    private System.Threading.Timer? _checkTimer;
    private WindowInfo? _lastWindowInfo;

    // ===== Limit 3.0 性能优化: PID 缓存 =====
    // PID 缓存：窗口句柄 -> (PID, 缓存时间)
    private Dictionary<IntPtr, (int Pid, DateTime CachedAt)> _pidCache = new();
    private const int PidCacheSeconds = 10; // PID 缓存时长（秒）

    // ===== Limit 3.0 性能优化: URL 防抖 =====
    // URL 缓存：窗口句柄 -> (URL, 缓存时间)
    private Dictionary<IntPtr, (string Url, DateTime CachedAt)> _urlCache = new();
    private const int UrlCacheSeconds = 30; // Beta 2: 增加缓存时间到 30 秒

    // URL 防抖：窗口句柄 -> 首次稳定时间
    private Dictionary<IntPtr, DateTime> _windowStableTime = new();
    private const int DebounceSeconds = 5; // Beta 2: 增加防抖时间到 5 秒

    // Beta 2: 防止并发 URL 获取
    private HashSet<IntPtr> _urlFetchingSet = new();

    public event EventHandler<WindowInfo>? ActiveWindowChanged;

    public void Start()
    {
        // 先立即捕获当前窗口并触发事件
        CheckActiveWindow(null);

        // 每秒检查一次当前窗口
        _checkTimer = new System.Threading.Timer(CheckActiveWindow, null, 1000, 1000);
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

                // ===== Limit 3.0 性能优化: PID 缓存 =====
                GetWindowThreadProcessId(hwnd, out uint pid);

                // 先尝试从缓存获取 Process
                Process? process = null;
                if (_pidCache.TryGetValue(hwnd, out var cachedPid))
                {
                    // 检查缓存是否过期
                    if ((DateTime.Now - cachedPid.CachedAt).TotalSeconds < PidCacheSeconds)
                    {
                        try
                        {
                            process = Process.GetProcessById(cachedPid.Pid);
                        }
                        catch
                        {
                            // 进程可能已退出，清除缓存
                            _pidCache.Remove(hwnd);
                        }
                    }
                }

                // 缓存未命中或已过期，重新获取
                if (process == null)
                {
                    process = Process.GetProcessById((int)pid);
                    _pidCache[hwnd] = ((int)pid, DateTime.Now);
                    // Beta 2: 减少日志输出（仅在调试时开启）
                    // Debug.WriteLine($"[WindowTracker] PID cached: {process.ProcessName} (PID: {pid})");
                }

                // 简单的敏感词过滤
                string sanitized = SanitizeTitle(title);

                // ===== Limit 3.0 性能优化: URL 防抖 + 缓存 =====
                string? url = null;
                if (WebsiteRecognizer.IsBrowserProcess(process.ProcessName))
                {
                    url = GetCachedOrFetchUrlWithDebounce(hwnd);
                }

                return new WindowInfo(process.ProcessName, title, sanitized, url);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowTracker] Error getting window info: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Limit 3.0: 带防抖的 URL 获取（过滤快速切换）
    /// </summary>
    private string? GetCachedOrFetchUrlWithDebounce(IntPtr hwnd)
    {
        // 1. 检查 URL 缓存
        if (_urlCache.TryGetValue(hwnd, out var cached))
        {
            if ((DateTime.Now - cached.CachedAt).TotalSeconds < UrlCacheSeconds)
            {
                // 缓存有效，直接返回
                return cached.Url;
            }
        }

        // 2. 防抖逻辑：检查窗口是否稳定停留
        if (!_windowStableTime.TryGetValue(hwnd, out var stableTime))
        {
            // 首次访问此窗口，记录时间
            _windowStableTime[hwnd] = DateTime.Now;
            // 返回缓存的旧值（如果有）或 null
            return cached.Url;
        }

        var stableDuration = (DateTime.Now - stableTime).TotalSeconds;
        if (stableDuration < DebounceSeconds)
        {
            // 窗口停留时间不足，暂不获取 URL（过滤 Alt+Tab 快速切换）
            Debug.WriteLine($"[WindowTracker] URL debouncing: {stableDuration:F1}s < {DebounceSeconds}s");
            return cached.Url;
        }

        // 3. 窗口已稳定，异步获取 URL（不阻塞主线程）
        // Beta 2: 添加并发锁，防止重复获取
        if (!_urlCache.ContainsKey(hwnd) || (DateTime.Now - cached.CachedAt).TotalSeconds >= UrlCacheSeconds)
        {
            // 检查是否正在获取
            lock (_urlFetchingSet)
            {
                if (_urlFetchingSet.Contains(hwnd))
                {
                    // 已经在获取中，直接返回缓存
                    return cached.Url;
                }
                _urlFetchingSet.Add(hwnd);
            }

            _ = Task.Run(() =>
            {
                try
                {
                    var url = BrowserUrlExtractor.GetBrowserUrl(hwnd);
                    if (url != null)
                    {
                        _urlCache[hwnd] = (url, DateTime.Now);
                        Debug.WriteLine($"[WindowTracker] URL fetched: {url}");
                    }
                }
                finally
                {
                    lock (_urlFetchingSet)
                    {
                        _urlFetchingSet.Remove(hwnd);
                    }
                }
            });
        }

        // 返回缓存值（如果有）
        return cached.Url;
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
        _pidCache.Clear();
        _urlCache.Clear();
        _windowStableTime.Clear();
    }

    // P/Invoke
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
