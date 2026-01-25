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
    private readonly ContextInsightService _contextInsightService;
    private readonly ClusterService _clusterService;
    private System.Threading.Timer? _timer;
    private WindowInfo? _currentWindow;
    private DateTime _lastTick;

    // Limit 3.0 性能优化: 信号量控制并发写入，避免数据库锁定
    private readonly System.Threading.SemaphoreSlim _dbSemaphore = new(1, 1);

    // 空闲阈值（秒）- 超过此时间认为用户不在使用电脑
    private const int IdleThresholdSeconds = 60;

    public UsageCollectorService(
        IWindowTracker windowTracker,
        DatabaseService databaseService,
        ContextInsightService contextInsightService,
        ClusterService clusterService,
        GlobalInputMonitor inputMonitor)  // P1 修复：通过 DI 注入，避免独立实例
    {
        _windowTracker = windowTracker;
        _databaseService = databaseService;
        _contextInsightService = contextInsightService;
        _clusterService = clusterService;
        _inputMonitor = inputMonitor;  // P1 修复：使用注入的单例
        _lastTick = DateTime.Now;
    }

    public void Start()
    {
        _windowTracker.ActiveWindowChanged += OnActiveWindowChanged;
        // 注意: _inputMonitor 由 UserActivityManager 管理启动/停止
        // 每 10 秒批量写入/检查一次数据库
        _timer = new System.Threading.Timer(OnTick, null, 10000, 10000);
    }

    public void Stop()
    {
        _timer?.Dispose();
        // 注意: _inputMonitor 由 UserActivityManager 管理启动/停止
        _windowTracker.ActiveWindowChanged -= OnActiveWindowChanged;

        // 保存最后的数据
        SaveCurrentUsage();
    }

    private void OnActiveWindowChanged(object? sender, WindowInfo info)
    {
        FragmentationCount++; // 增加上下文切换计数

        // 保存上一个窗口的使用时间
        SaveCurrentUsage();

        // 更新当前窗口
        _currentWindow = info;
        _lastTick = DateTime.Now;

        // 更新上下文洞察服务
        var cluster = _clusterService.GetClusterForProcess(info.ProcessName);
        _contextInsightService.UpdateContext(info.ProcessName, cluster?.Id, info.WindowTitle);
    }

    private void OnTick(object? state)
    {
        // ===== Limit 3.0 Beta 2: 移除重复调用（已在 UserActivityManager.Tick 中调用）=====
        // _inputMonitor.CheckIdleState(); // ❌ 删除

        if (_timer != null)
        {
            // 异步触发统计更新，不等待
            _ = UpdateStatisticsAsync();
        }

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

        // 捕获当前窗口信息（避免异步任务中访问可变状态）
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

        // ===== Limit 3.0 性能优化: Fire-and-forget 异步写入，不阻塞主线程 =====
        _ = Task.Run(async () =>
        {
            // 使用信号量确保写入顺序，避免数据库并发冲突
            await _dbSemaphore.WaitAsync();
            try
            {
                // 保存到数据库（支持网站信息）
                await _databaseService.UpdateUsageWithWebsiteAsync(
                    processName,
                    "", // AppPath
                    websiteName,
                    pageTitle,
                    (int)duration
                );

                // 同时更新每小时使用记录（用于分析页面柱状图）
                await _databaseService.UpdateHourlyUsageAsync(processName, (int)duration);

                Debug.WriteLine($"[UsageCollector] Saved usage: {processName} ({duration:F1}s)");
            }
            catch (Exception ex)
            {
                // 记录异常但不中断流程
                Debug.WriteLine($"[UsageCollector] Error saving usage: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        });
    }

    public void Dispose()
    {
        Stop();
        _dbSemaphore?.Dispose();
    }

    // Limit 3.0: 提供给 Bridge 的统计接口
    // 注意：为避免频繁 IO，这里使用内部缓存

    /// <summary>今日总活跃时间（TimeSpan）</summary>
    public TimeSpan TotalActiveTime
    {
        get => TimeSpan.FromMinutes(_dailyActiveMinutes);
    }

    private double _dailyActiveMinutes = 0;

    /// <summary>今日碎片化次数（上下文切换次数）</summary>
    public int FragmentationCount { get; private set; } = 0;

    /// <summary>
    /// 获取今日高耗能应用排行
    /// </summary>
    public IEnumerable<(string ProcessName, double ImpactScore)> GetTopDrainers(int count)
    {
        // 从缓存返回，避免阻塞 UI
        return _cachedTopDrainers.Take(count);
    }

    private List<(string ProcessName, double ImpactScore)> _cachedTopDrainers = new();

    // 定期更新统计数据的辅助方法
    private async Task UpdateStatisticsAsync()
    {
        try
        {
            var today = DateTime.Today;

            // 1. 获取今日 TOP 使用
            var top = await _databaseService.GetTopUsageAsync(today, 5);

            // 2. 获取今日总时间
            var all = await _databaseService.GetUsageForDateAsync(today);
            var totalSeconds = all.Sum(x => x.DurationSeconds);

            _dailyActiveMinutes = totalSeconds / 60.0;

            // 3. 更新缓存
            if (totalSeconds > 0)
            {
                _cachedTopDrainers = top.Select(t => (
                    t.AppName,
                    (double)t.DurationSeconds / totalSeconds
                )).ToList();
            }
            else
            {
                _cachedTopDrainers.Clear();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UsageCollector] Stats update error: {ex.Message}");
        }
    }
}
