using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EyeGuard.Core.Interfaces;
using EyeGuard.Infrastructure.Monitors;

namespace EyeGuard.Infrastructure.Services;

public class UsageCollectorService : IDisposable
{
    private readonly IWindowTracker _windowTracker;
    private readonly DatabaseService _databaseService;
    private readonly GlobalInputMonitor _inputMonitor; // 用于检测空闲状态
    private System.Threading.Timer? _timer;
    private WindowInfo? _currentWindow;
    private DateTime _lastTick;
    
    // 空闲阈值（秒）- 超过此时间认为用户不在使用电脑
    private const int IdleThresholdSeconds = 60;

    public UsageCollectorService(IWindowTracker windowTracker, DatabaseService databaseService)
    {
        _windowTracker = windowTracker;
        _databaseService = databaseService;
        _inputMonitor = new GlobalInputMonitor(); // 初始化输入监测器
        _lastTick = DateTime.Now;
    }

    public void Start()
    {
        _windowTracker.ActiveWindowChanged += OnActiveWindowChanged;
        _inputMonitor.Start(); // 启动输入监测
        // 每 10 秒批量写入/检查一次数据库
        _timer = new System.Threading.Timer(OnTick, null, 10000, 10000); 
    }

    public void Stop()
    {
        _timer?.Dispose();
        _inputMonitor.Stop(); // 停止输入监测
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
        // 检查空闲状态
        _inputMonitor.CheckIdleState();
        
        SaveCurrentUsage();
        _lastTick = DateTime.Now; // 重置最后时间
    }

    private void SaveCurrentUsage()
    {
        if (_currentWindow == null) return;
        
        // ===== Limit 2.0: 空闲检测 - 只记录用户活跃时的使用时长 =====
        if (_inputMonitor.IdleSeconds >= IdleThresholdSeconds)
        {
            // 用户空闲/离开，不记录使用时长
            Debug.WriteLine($"[UsageCollector] User idle ({_inputMonitor.IdleSeconds:F0}s), skipping usage record");
            return;
        }
        
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
