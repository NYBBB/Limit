namespace EyeGuard.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using EyeGuard.Core.Models;
using EyeGuard.Core.Entities;
using System.Diagnostics;

/// <summary>
/// Context Insight 服务 - Limit 3.0 微文案生成器
/// 根据当前状态自动选择最贴切的洞察文案
/// </summary>
public class ContextInsightService
{
    private readonly DatabaseService _databaseService;
    private readonly FatigueEngine _fatigueEngine;
    private readonly ClusterService _clusterService;

    // 场景触发阈值
    private const double DominanceThreshold = 0.6; // 专注模式：60% 时间在同一应用
    private const int FragmentationSwitchCount = 15; // 碎片化：10分钟内切换15次
    private const double FatigueHighThreshold = 70.0; // 疲劳关联：疲劳值 > 70%
    private const int RecoveryMinutes = 10; // 恢复模式：低负载应用停留 > 10 分钟

    // 统计数据缓存（避免频繁查询数据库）
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private const int CacheSeconds = 30; // Beta 2: 增加缓存时间到 30 秒（减少 DB 查询）
    private string? _currentProcessName;
    private int? _currentClusterId;
    private int _recentSwitchCount = 0;
    private DateTime _lastContextChangeTime = DateTime.Now; // 新增计时器
    private Dictionary<string, double> _hourlyAppUsage = new();

    /// <summary>
    /// 当前上下文信息
    /// </summary>
    public ContextInfo CurrentContext { get; private set; } = new ContextInfo();

    public ContextInsightService(
        DatabaseService databaseService,
        FatigueEngine fatigueEngine,
        ClusterService clusterService)
    {
        _databaseService = databaseService;
        _fatigueEngine = fatigueEngine;
        _clusterService = clusterService;
    }

    /// <summary>
    /// 更新当前上下文信息（由外部调用）
    /// </summary>
    public void UpdateContext(string processName, int? clusterId, string? windowTitle = null)
    {
        if (_currentProcessName != processName)
        {
            _recentSwitchCount++;
            _lastContextChangeTime = DateTime.Now; // 重置计时
        }

        _currentProcessName = processName;
        _currentClusterId = clusterId;

        // 更新公开属性
        CurrentContext = new ContextInfo
        {
            ProcessName = processName,
            WindowTitle = windowTitle,
            ClusterName = clusterId.HasValue ? _clusterService.GetClusterById(clusterId.Value)?.Name : null,
            Duration = DateTime.Now - _lastContextChangeTime
        };
    }

    /// <summary>
    /// 获取当前最合适的洞察（按优先级）
    /// </summary>
    public ContextInsight GetCurrentInsight()
    {
        // 更新统计缓存
        if ((DateTime.Now - _lastCacheUpdate).TotalSeconds > CacheSeconds)
        {
            UpdateStatisticsCache();
        }

        // 按优先级检查各场景（从高到低）
        // 1. 疲劳关联（最紧急）
        if (CheckFatigueHigh())
            return CreateFatigueHighInsight();

        // 2. 碎片化警告
        if (CheckFragmentation())
            return CreateFragmentationInsight();

        // 3. 工作流识别
        if (CheckClusterFlow())
            return CreateClusterFlowInsight();

        // 4. 专注模式
        if (CheckDominance())
            return CreateDominanceInsight();

        // 5. 恢复模式（默认）
        if (CheckRecovery())
            return CreateRecoveryInsight();

        // 默认：返回中性状态
        return CreateNeutralInsight();
    }

    /// <summary>
    /// 更新统计数据缓存（异步非阻塞）
    /// </summary>
    private void UpdateStatisticsCache()
    {
        // ===== Limit 3.0: 避免 UI 死锁 - 异步更新缓存 =====
        _ = Task.Run(async () =>
        {
            try
            {
                // 获取今日的使用记录
                var today = DateTime.Today;
                var allRecords = await _databaseService.GetUsageForDateAsync(today);

                // 过滤出过去 1 小时的数据（简化版：使用今日总数据）
                // TODO: 如果需要精确的 1 小时数据，需要在 DatabaseService 添加时间范围查询
                var totalSeconds = allRecords.Sum(r => r.DurationSeconds);

                _hourlyAppUsage = allRecords
                    .GroupBy(r => r.AppName)
                    .ToDictionary(
                        g => g.Key,
                        g => totalSeconds > 0 ? g.Sum(r => r.DurationSeconds) / (double)totalSeconds : 0
                    );

                _lastCacheUpdate = DateTime.Now;

                Debug.WriteLine($"[ContextInsightService] Cache updated: {_hourlyAppUsage.Count} apps");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContextInsightService] Error updating stats: {ex.Message}");
            }
        });

        // 重置切换计数（每5秒重置一次，保持最近10分钟的近似值）
        if (_recentSwitchCount > 100) _recentSwitchCount = 0;
    }

    // ===== 场景检测逻辑 =====

    /// <summary>
    /// 场景 1: 专注模式检测
    /// </summary>
    private bool CheckDominance()
    {
        if (string.IsNullOrEmpty(_currentProcessName)) return false;

        if (_hourlyAppUsage.TryGetValue(_currentProcessName, out var usage))
        {
            return usage > DominanceThreshold;
        }

        return false;
    }

    /// <summary>
    /// 场景 2: 碎片化警告检测
    /// </summary>
    private bool CheckFragmentation()
    {
        // 简化版：检查最近的切换次数
        return _recentSwitchCount >= FragmentationSwitchCount;
    }

    /// <summary>
    /// 场景 3: 疲劳关联检测
    /// </summary>
    private bool CheckFatigueHigh()
    {
        var fatigue = _fatigueEngine.FatigueValue;
        var isHighLoad = _currentProcessName != null &&
                        ContextClassifier.GetLoadWeight(
                            ContextClassifier.ClassifyApp(_currentProcessName)
                        ) >= 1.0;

        return fatigue > FatigueHighThreshold && isHighLoad;
    }

    /// <summary>
    /// 场景 4: 工作流识别检测
    /// </summary>
    private bool CheckClusterFlow()
    {
        return _currentClusterId.HasValue && _currentClusterId.Value > 0;
    }

    /// <summary>
    /// 场景 5: 恢复模式检测
    /// </summary>
    private bool CheckRecovery()
    {
        if (string.IsNullOrEmpty(_currentProcessName)) return false;

        var loadWeight = ContextClassifier.GetLoadWeight(
            ContextClassifier.ClassifyApp(_currentProcessName)
        );

        // 低负载应用（如视频、音乐）
        return loadWeight < 0.5;
    }

    // ===== 文案生成方法 =====

    private ContextInsight CreateDominanceInsight()
    {
        var usage = _hourlyAppUsage.TryGetValue(_currentProcessName!, out var val)
            ? (int)(val * 100)
            : 0;

        var variants = new[]
        {
            ($"过去 1 小时：{usage}% 时间都在 {_currentProcessName}", $"Past hour: {usage}% on {_currentProcessName}"),
            ($"深度工作：连续专注中", "Deep Work: Uninterrupted focus"),
            ($"沉浸状态：{_currentProcessName} 占据主导", $"Flow State: {_currentProcessName} dominates")
        };

        var selected = variants[Random.Shared.Next(variants.Length)];

        return new ContextInsight
        {
            Type = InsightType.Dominance,
            Icon = "🎯",
            TextCN = selected.Item1,
            TextEN = selected.Item2,
            IsActive = true
        };
    }

    private ContextInsight CreateFragmentationInsight()
    {
        var variants = new[]
        {
            ($"切换频繁：10 分钟内打开了 {_recentSwitchCount} 个应用", $"High switching: {_recentSwitchCount} apps in 10 mins"),
            ("注意力正在因切换而流失", "Attention is fragmented"),
            ("建议单任务模式", "Try single-tasking")
        };

        var selected = variants[Random.Shared.Next(variants.Length)];

        return new ContextInsight
        {
            Type = InsightType.Fragmentation,
            Icon = "🌪️",
            TextCN = selected.Item1,
            TextEN = selected.Item2,
            IsActive = true
        };
    }

    private ContextInsight CreateFatigueHighInsight()
    {
        var fatigue = (int)_fatigueEngine.FatigueValue;

        var variants = new[]
        {
            ($"效率正在下降，错误率可能上升", "Efficiency dropping. Error rate likely up"),
            ($"疲劳值 {fatigue}%：建议休息", $"Fatigue {fatigue}%: Take a break"),
            ("无效死磕：疲劳值高，产出值低", "Grinding: Fatigue high, output low")
        };

        var selected = variants[Random.Shared.Next(variants.Length)];

        return new ContextInsight
        {
            Type = InsightType.FatigueHigh,
            Icon = "📉",
            TextCN = selected.Item1,
            TextEN = selected.Item2,
            IsActive = true
        };
    }

    private ContextInsight CreateClusterFlowInsight()
    {
        var cluster = _clusterService.GetClusterById(_currentClusterId!.Value);
        var clusterName = cluster?.Name ?? "工作流";

        var variants = new[]
        {
            ($"工作流：{clusterName} 循环中", $"Workflow: {clusterName} Loop"),
            ($"当前上下文：{clusterName}", $"Context: {clusterName}"),
            ($"簇内切换：保持专注", "Cluster switching: Stay focused")
        };

        var selected = variants[Random.Shared.Next(variants.Length)];

        return new ContextInsight
        {
            Type = InsightType.ClusterFlow,
            Icon = "🔗",
            TextCN = selected.Item1,
            TextEN = selected.Item2,
            IsActive = true
        };
    }

    private ContextInsight CreateRecoveryInsight()
    {
        var variants = new[]
        {
            ("正在回血中...", "Recharging energy..."),
            ("被动模式：疲劳积累已暂停", "Passive Mode: Fatigue paused"),
            ("恢复精力中", "Recovering stamina")
        };

        var selected = variants[Random.Shared.Next(variants.Length)];

        return new ContextInsight
        {
            Type = InsightType.Recovery,
            Icon = "🔋",
            TextCN = selected.Item1,
            TextEN = selected.Item2,
            IsActive = true
        };
    }

    private ContextInsight CreateNeutralInsight()
    {
        return new ContextInsight
        {
            Type = InsightType.Recovery,
            Icon = "💻",
            TextCN = "正常工作中",
            TextEN = "Working normally",
            IsActive = false
        };
    }
}

/// <summary>
/// 上下文信息 DTO
/// </summary>
public class ContextInfo
{
    public string? ProcessName { get; set; }
    public string? WindowTitle { get; set; }
    public string? ClusterName { get; set; }
    public TimeSpan Duration { get; set; }
}
