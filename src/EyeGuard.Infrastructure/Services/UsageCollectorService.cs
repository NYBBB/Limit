using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EyeGuard.Core.Interfaces;

namespace EyeGuard.Infrastructure.Services;

public class UsageCollectorService : IDisposable
{
    private readonly IWindowTracker _windowTracker;
    private readonly DatabaseService _databaseService;
    private System.Threading.Timer? _timer;
    private WindowInfo? _currentWindow;
    private DateTime _lastTick;

    public UsageCollectorService(IWindowTracker windowTracker, DatabaseService databaseService)
    {
        _windowTracker = windowTracker;
        _databaseService = databaseService;
        _lastTick = DateTime.Now;
    }

    public void Start()
    {
        _windowTracker.ActiveWindowChanged += OnActiveWindowChanged;
        // 每 10 秒批量写入/检查一次数据库
        _timer = new System.Threading.Timer(OnTick, null, 10000, 10000); 
    }

    public void Stop()
    {
        _timer?.Dispose();
        _windowTracker.ActiveWindowChanged -= OnActiveWindowChanged;
        
        // 保存最后的数据
        SaveCurrentUsage();
    }

    private void OnActiveWindowChanged(object? sender, WindowInfo info)
    {
        // 保存上一个窗口的使用时间
        SaveCurrentUsage();
        
        // 更新当前窗口
        _currentWindow = info;
        _lastTick = DateTime.Now;
    }

    private void OnTick(object? state)
    {
        SaveCurrentUsage();
        _lastTick = DateTime.Now; // 重置最后时间
    }

    private void SaveCurrentUsage()
    {
        if (_currentWindow == null) return;
        
        var now = DateTime.Now;
        var duration = (now - _lastTick).TotalSeconds;
        
        if (duration < 1) return; // 忽略太短的时间

        try
        {
            var processName = _currentWindow.ProcessName;
            var windowTitle = _currentWindow.WindowTitle;
            
            // 检测是否为浏览器
            var isBrowser = WebsiteRecognizer.IsBrowserProcess(processName);
            string? websiteName = null;
            string? pageTitle = null;
            
            if (isBrowser)
            {
                // 尝试识别网站
                if (WebsiteRecognizer.TryRecognizeWebsite(windowTitle, out websiteName))
                {
                    // 识别成功，websiteName 已设置
                }
                else
                {
                    // 未识别，保留原始标题（归类到"其他网站"）
                    pageTitle = windowTitle;
                }
            }
            
            // 保存到数据库（支持网站信息）
            _databaseService.UpdateUsageWithWebsiteAsync(
                processName, 
                "", // AppPath 
                websiteName,
                pageTitle,
                (int)duration
            ).Wait();
            
            // 同时更新每小时使用记录（用于分析页面柱状图）
            _databaseService.UpdateHourlyUsageAsync(processName, (int)duration).Wait();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving usage: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
