using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.DependencyInjection;
using EyeGuard.Infrastructure.Services;

namespace EyeGuard.UI.Bridge;

/// <summary>
/// Bridge 服务类
/// 统一管理 WebView2 通信和命令处理
/// </summary>
public class BridgeService : IDisposable
{
    private readonly IServiceProvider _services;
    private MessageHandler? _messageHandler;
    private bool _isInitialized;

    // ===== Zone B: Cluster Galaxy 数据追踪 =====
    private readonly List<string> _recentApps = new(5);  // 最近使用的应用（用于卫星）
    private string _currentAppName = "";                  // 当前主应用
    private bool _isWindowForeground = false;             // 窗口是否前台（冻结用）
    private bool _isFocusMode = false;                    // 专注/轻松模式
    private object? _lastZoneBData = null;                // 上一次 Zone B 数据（冻结用）

    public BridgeService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// 初始化 Bridge 连接
    /// </summary>
    /// <param name="webView">WebView2 核心对象</param>
    public void Initialize(CoreWebView2 webView)
    {
        if (_isInitialized) return;

        _messageHandler = new MessageHandler(webView);
        RegisterActionHandlers();
        _isInitialized = true;

        System.Diagnostics.Debug.WriteLine("[Bridge] 服务已初始化");
    }

    /// <summary>
    /// 获取消息处理器
    /// </summary>
    public MessageHandler? Handler => _messageHandler;

    /// <summary>
    /// 注册所有命令处理器
    /// </summary>
    private void RegisterActionHandlers()
    {
        if (_messageHandler == null) return;

        // 测试命令
        _messageHandler.RegisterHandler(BridgeMessages.TestPing, data =>
        {
            System.Diagnostics.Debug.WriteLine($"[Bridge] 收到 Ping: {data}");
            _messageHandler.SendToJS("TEST_PONG", new { timestamp = DateTime.Now.Ticks });
        });

        // 校准疲劳值
        _messageHandler.RegisterHandler(BridgeMessages.CalibrateFatigue, data =>
        {
            var userActivityManager = _services.GetRequiredService<UserActivityManager>();

            if (data.TryGetProperty("mode", out var modeElement))
            {
                var mode = modeElement.GetString();
                switch (mode)
                {
                    case "tired":
                        userActivityManager.FatigueEngine.CalibrateAsTired();
                        break;
                    case "fresh":
                        userActivityManager.FatigueEngine.CalibrateAsFresh();
                        break;
                    case "rested":
                        userActivityManager.FatigueEngine.ApplyRecoveryCredit(15);
                        break;
                }
            }
        });

        // 切换 Focusing/Chilling 模式
        _messageHandler.RegisterHandler(BridgeMessages.ToggleFocusingMode, data =>
        {
            var userActivityManager = _services.GetRequiredService<UserActivityManager>();
            if (userActivityManager.IsFocusCommitmentActive)
            {
                userActivityManager.StopFocusCommitment(false);
            }
            ToggleFocusMode(); // 切换 UI 状态
            System.Diagnostics.Debug.WriteLine($"[Bridge] Focus Mode toggled");
        });

        // 开始专注承诺
        _messageHandler.RegisterHandler(BridgeMessages.StartFocusCommitment, data =>
        {
            var userActivityManager = _services.GetRequiredService<UserActivityManager>();
            int duration = 30; // 默认
            string taskName = "Focus Session";

            if (data.TryGetProperty("durationMinutes", out var durElem)) duration = durElem.GetInt32();
            if (data.TryGetProperty("taskName", out var taskElem)) taskName = taskElem.GetString() ?? taskName;

            userActivityManager.StartFocusCommitment(duration, taskName);
            _isFocusMode = true; // 同步 UI 状态
            SendZoneBUpdate();
        });

        // 停止专注承诺
        _messageHandler.RegisterHandler(BridgeMessages.StopFocusCommitment, data =>
        {
            var userActivityManager = _services.GetRequiredService<UserActivityManager>();
            userActivityManager.StopFocusCommitment(false);
            _isFocusMode = false; // 同步 UI 状态
            SendZoneBUpdate();
        });

        // 更新设置
        _messageHandler.RegisterHandler(BridgeMessages.SaveSettings, data =>
        {
            var settingsService = _services.GetRequiredService<SettingsService>();
            var settings = settingsService.Settings;

            // 疲劳度设置
            if (data.TryGetProperty("softReminderThreshold", out var softReminderElement))
                settings.SoftReminderThreshold = softReminderElement.GetInt32();

            if (data.TryGetProperty("forceBreakThreshold", out var forceBreakElement))
                settings.ForceBreakThreshold = forceBreakElement.GetInt32();

            if (data.TryGetProperty("idleThresholdSeconds", out var idleElement))
                settings.IdleThresholdSeconds = idleElement.GetInt32();

            // 检测方式
            if (data.TryGetProperty("enableKeyboardMonitor", out var keyboardElement))
                settings.EnableKeyboardMonitor = keyboardElement.GetBoolean();

            if (data.TryGetProperty("enableAudioMonitor", out var audioElement))
                settings.EnableAudioMonitor = audioElement.GetBoolean();

            // 疲劳敏感度
            if (data.TryGetProperty("careSensitivity", out var senseElement))
                settings.CareSensitivity = senseElement.GetInt32();

            // 干预策略
            if (data.TryGetProperty("interventionMode", out var interventionElement))
                settings.InterventionMode = interventionElement.GetInt32();

            // 提醒设置
            if (data.TryGetProperty("enableReminders", out var remindersElement))
                settings.EnableReminders = remindersElement.GetBoolean();

            if (data.TryGetProperty("reminderType", out var reminderTypeElement))
                settings.ReminderType = reminderTypeElement.GetInt32();

            // 高级设置
            if (data.TryGetProperty("showTrayIcon", out var trayElement))
                settings.ShowTrayIcon = trayElement.GetBoolean();

            if (data.TryGetProperty("autoStart", out var autoStartElement))
                settings.AutoStartOnBoot = autoStartElement.GetBoolean();

            if (data.TryGetProperty("snapshotInterval", out var snapshotElement))
                settings.FatigueSnapshotIntervalSeconds = snapshotElement.GetInt32();

            if (data.TryGetProperty("chartInterval", out var chartElement))
                settings.FatigueChartIntervalMinutes = chartElement.GetInt32();

            if (data.TryGetProperty("refreshInterval", out var refreshElement))
                settings.DashboardRefreshIntervalSeconds = refreshElement.GetInt32();

            settingsService.Save();
            System.Diagnostics.Debug.WriteLine($"[Bridge] 设置已保存: IdleThreshold={settings.IdleThresholdSeconds}s, Sensitivity={settings.CareSensitivity}%");
        });

        // 请求设置
        _messageHandler.RegisterHandler(BridgeMessages.RequestSettings, data =>
        {
            var settings = _services.GetRequiredService<SettingsService>().Settings;
            _messageHandler.SendToJS("SETTINGS_LOADED", new
            {
                // 疲劳度设置
                softReminderThreshold = settings.SoftReminderThreshold,
                forceBreakThreshold = settings.ForceBreakThreshold,
                idleThresholdSeconds = settings.IdleThresholdSeconds,

                // 检测方式
                enableKeyboardMonitor = settings.EnableKeyboardMonitor,
                enableAudioMonitor = settings.EnableAudioMonitor,

                // 疲劳敏感度
                careSensitivity = settings.CareSensitivity,

                // 干预策略
                interventionMode = settings.InterventionMode,

                // 提醒设置
                enableReminders = settings.EnableReminders,
                reminderType = settings.ReminderType,

                // 高级设置
                showTrayIcon = settings.ShowTrayIcon,
                autoStart = settings.AutoStartOnBoot,
                snapshotInterval = settings.FatigueSnapshotIntervalSeconds,
                chartInterval = settings.FatigueChartIntervalMinutes,
                refreshInterval = settings.DashboardRefreshIntervalSeconds
            });
        });

        _messageHandler.RegisterHandler(BridgeMessages.RequestRefresh, data =>
        {
            // 触发所有数据更新
            SendAllUpdates();
            // 同时也发送 Cluster 数据
            SendClustersUpdate();
        });

        // 请求 Cluster 数据
        _messageHandler.RegisterHandler(BridgeMessages.RequestClusters, data =>
        {
            SendClustersUpdate();
        });

        // 更新 Cluster 数据
        _messageHandler.RegisterHandler(BridgeMessages.UpdateClusters, async data =>
        {
            var clusterService = _services.GetRequiredService<ClusterService>();

            // ===== 修复：在 await 之前提取所有数据，避免 JsonDocument disposed =====
            var clusterUpdates = new List<(string IdStr, string Name, string Color, List<string> Apps)>();

            if (data.TryGetProperty("clusters", out var clustersElement))
            {
                foreach (var clusterJson in clustersElement.EnumerateArray())
                {
                    string idStr = clusterJson.GetProperty("id").GetString() ?? "";
                    string name = clusterJson.GetProperty("name").GetString() ?? "New Cluster";
                    string color = clusterJson.GetProperty("color").GetString() ?? "#000000";

                    var appsList = new List<string>();
                    if (clusterJson.TryGetProperty("apps", out var appsElement))
                    {
                        foreach (var app in appsElement.EnumerateArray())
                        {
                            var appName = app.GetProperty("name").GetString();
                            if (!string.IsNullOrEmpty(appName))
                            {
                                appsList.Add(appName);
                            }
                        }
                    }

                    clusterUpdates.Add((idStr, name, color, appsList));
                }
            }

            // ===== 现在可以安全地 await =====
            foreach (var (idStr, name, color, appsList) in clusterUpdates)
            {
                bool isExisting = int.TryParse(idStr, out int id);

                EyeGuard.Core.Entities.Cluster? cluster = null;
                if (isExisting)
                {
                    cluster = clusterService.GetClusterById(id);
                }

                if (cluster != null)
                {
                    cluster.Name = name;
                    cluster.Color = color;
                    cluster.AppList = appsList;
                    await clusterService.UpdateClusterAsync(cluster);
                }
                else
                {
                    var newCluster = new EyeGuard.Core.Entities.Cluster
                    {
                        Name = name,
                        Color = color,
                        AppList = appsList,
                        IsSystemPreset = false
                    };
                    await clusterService.AddClusterAsync(newCluster);
                }
            }

            SendClustersUpdate();
        });

        // 请求未分类应用 (Cluster Editor 用)
        _messageHandler.RegisterHandler(BridgeMessages.RequestUnclassifiedApps, data =>
        {
            var usageCollector = _services.GetRequiredService<UsageCollectorService>();
            var clusterService = _services.GetRequiredService<ClusterService>();
            var iconExtractor = _services.GetService<IconExtractorService>();

            // 1. 获取最近使用的应用 (例如最近 50 个)
            // GetTopDrainers 包含了最近活跃的应用
            var recentApps = usageCollector.GetTopDrainers(50);

            // 2. 获取已分类的应用 ID 集合
            var clusters = clusterService.GetAllClusters();
            var classifiedApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in clusters)
            {
                foreach (var app in c.AppList)
                {
                    classifiedApps.Add(app);  // AppList 存储的是 ProcessName (e.g. "Code")
                }
            }

            // 3. 筛选未分类应用
            var unclassifiedList = new List<object>();

            foreach (var app in recentApps)
            {
                // UsageCollector 中的 ProcessName 是 exe 名 (e.g. "Code.exe")
                // ClusterService 的 AppList 也是 exe 名 (e.g. "Code.exe")
                // 但为了保险，我们都处理一下

                string processName = app.ProcessName;
                if (classifiedApps.Contains(processName)) continue;

                // 尝试从缓存获取图标
                string icon = Services.IconMapper.GetMaterialSymbol(processName);
                bool isImage = false;

                if (iconExtractor != null)
                {
                    // 使用安全的 GetProcessPath
                    var processes = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                    if (processes.Length > 0)
                    {
                        var process = processes[0];
                        var exePath = IconExtractorService.GetProcessPath(process.Id);
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            var realIcon = iconExtractor.GetIconBase64(exePath);
                            if (!string.IsNullOrEmpty(realIcon))
                            {
                                icon = realIcon;
                                isImage = true;
                            }
                        }
                        process.Dispose();
                    }
                }

                unclassifiedList.Add(new
                {
                    name = Services.IconMapper.GetFriendlyName(processName),
                    processName = processName,
                    icon = icon,
                    isImage = isImage,
                    usageSeconds = 0 // 暂时不展示时长
                });
            }

            _messageHandler.SendToJS(BridgeMessages.UnclassifiedAppsLoaded, unclassifiedList);
        });

        // 调试状态请求
        _messageHandler.RegisterHandler(BridgeMessages.RequestDebugStatus, data =>
        {
            var activity = _services.GetRequiredService<UserActivityManager>();
            var collector = _services.GetRequiredService<UsageCollectorService>();

            var status = new
            {
                state = activity.CurrentState.ToString(),
                stateDescription = activity.GetStateDescription(),
                idleSeconds = activity.InputMonitor.IdleSeconds,
                audioPlaying = activity.AudioDetector.IsAudioPlaying,
                isFullscreen = activity.IsFullscreen,
                isPassiveConsumption = activity.IsPassiveConsumption,
                fatigue = activity.FatigueEngine.FatigueValue,
                fatigueSlope = activity.FatigueEngine.FatigueSlope,
                sensitivityBias = activity.FatigueEngine.SensitivityBias,
                isCareMode = activity.FatigueEngine.IsCareMode,
                isFlowMode = activity.IsFlowMode,
                isRefocusing = activity.IsRefocusing,
                currentProcessName = activity.CurrentProcessName,
                todayActiveMinutes = activity.TodayActiveSeconds / 60,
                currentSessionMinutes = activity.CurrentSessionSeconds / 60,
                longestSessionMinutes = activity.LongestSessionSeconds / 60,
                fragmentationCount = collector.FragmentationCount
            };

            _messageHandler.SendDebugStatusUpdate(status);
        });

        // 设置疲劳值（调试用）
        _messageHandler.RegisterHandler(BridgeMessages.SetFatigueValue, data =>
        {
            if (data.TryGetProperty("value", out var valueElement))
            {
                var value = valueElement.GetDouble();
                var activity = _services.GetRequiredService<UserActivityManager>();
                activity.FatigueEngine.SetFatigueValue(Math.Clamp(value, 0, 100));
                System.Diagnostics.Debug.WriteLine($"[Bridge] 设置疲劳值: {value}%");
            }
        });

        // Analytics 数据请求
        _messageHandler.RegisterHandler(BridgeMessages.RequestAnalytics, async data =>
        {
            DateTime date = DateTime.Today;
            if (data.TryGetProperty("date", out var dateElement) && dateElement.TryGetDateTime(out var parsedDate))
            {
                date = parsedDate.Date;
            }

            try
            {
                var response = await GetAnalyticsDataAsync(date);
                _messageHandler.SendAnalyticsUpdate(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bridge] Analytics Error: {ex}");
                // 发送错误或空数据
                // _messageHandler.SendAnalyticsUpdate(new { error = ex.Message });
            }
        });
    }

    /// <summary>
    /// 发送所有数据更新
    /// </summary>
    public void SendAllUpdates()
    {
        SendFatigueUpdate();
        SendContextUpdate();
        SendZoneBUpdate();  // Zone B: Cluster Galaxy
        SendDrainersUpdate();
    }

    /// <summary>
    /// 发送疲劳数据更新
    /// </summary>
    public void SendFatigueUpdate()
    {
        if (_messageHandler == null) return;

        var userActivityManager = _services.GetRequiredService<UserActivityManager>();
        var engine = userActivityManager.FatigueEngine;

        var color = engine.FatigueValue switch
        {
            < 40 => "#00f0ff",  // 青色
            < 70 => "#ffaa00",  // 琥珀色
            _ => "#ff2a2a"      // 红色
        };

        var breathRate = engine.FatigueValue switch
        {
            < 30 => 4.0,
            < 50 => 3.0,
            < 70 => 2.0,
            _ => 1.5
        };

        var state = engine.FatigueValue switch
        {
            < 30 => FatigueStates.Fresh,
            < 50 => FatigueStates.Focused,
            < 70 => FatigueStates.Flow,
            < 85 => FatigueStates.Strain,
            _ => FatigueStates.Drain
        };

        if (engine.IsCareMode)
        {
            state = FatigueStates.Care;
            color = "#ff8c00";
        }

        _messageHandler.SendFatigueUpdate(
            engine.FatigueValue,
            state,
            color,
            breathRate,
            engine.IsCareMode
        );
    }

    /// <summary>
    /// 发送上下文数据更新
    /// </summary>
    public void SendContextUpdate()
    {
        if (_messageHandler == null) return;

        var contextService = _services.GetRequiredService<ContextInsightService>();
        var userActivityManager = _services.GetRequiredService<UserActivityManager>();

        var currentContext = contextService.CurrentContext;
        var clusterName = currentContext.ClusterName ?? "Unclassified";
        var appName = currentContext.ProcessName ?? "Unknown";
        var displayName = currentContext.WindowTitle ?? appName;

        // 截断过长的标题
        if (displayName.Length > 30) displayName = displayName.Substring(0, 27) + "...";

        _messageHandler.SendContextUpdate(
            appName,
            displayName,
            clusterName,
            (int)currentContext.Duration.TotalMinutes,
            userActivityManager.IsFocusing
        );
    }

    /// <summary>
    /// Zone B: 发送 Cluster Galaxy 完整数据
    /// 包含：微文案、主星应用、卫星应用、会话时长、专注模式状态
    /// </summary>
    public void SendZoneBUpdate()
    {
        if (_messageHandler == null) return;

        // 获取服务
        var contextService = _services.GetRequiredService<ContextInsightService>();
        var userActivityManager = _services.GetRequiredService<UserActivityManager>();
        var clusterService = _services.GetRequiredService<ClusterService>();
        var iconExtractor = _services.GetService<IconExtractorService>(); // Phase 4: 本地图标提取服务

        var currentContext = contextService.CurrentContext;
        var processName = currentContext.ProcessName ?? "Unknown";

        // 冻结机制：如果窗口在前台，发送上一次的数据
        if (_isWindowForeground && _lastZoneBData != null)
        {
            _messageHandler.SendToJS(BridgeMessages.ZoneBUpdate, _lastZoneBData);
            return;
        }

        // 追踪最近应用（用于卫星显示）
        if (!string.IsNullOrEmpty(processName) && processName != _currentAppName)
        {
            if (!string.IsNullOrEmpty(_currentAppName))
            {
                _recentApps.Insert(0, _currentAppName);
                if (_recentApps.Count > 4) _recentApps.RemoveAt(4);
            }
            _currentAppName = processName;
        }

        // 获取 Cluster 信息
        var cluster = clusterService.GetClusterForProcess(processName);
        var clusterColor = cluster?.Color ?? "#64748b";
        var clusterName = cluster?.Name ?? "Unclassified";

        // 获取微文案
        var insight = contextService.GetCurrentInsight();

        // 构建主应用数据（尝试从缓存获取图标）
        string mainAppIcon = Services.IconMapper.GetMaterialSymbol(processName);
        bool mainAppIsImage = false;

        if (iconExtractor != null)
        {
            // 使用安全的 GetProcessPath 替代 Process.MainModule.FileName
            var process = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName)).FirstOrDefault();
            if (process != null)
            {
                var exePath = IconExtractorService.GetProcessPath(process.Id);
                if (!string.IsNullOrEmpty(exePath))
                {
                    var realIcon = iconExtractor.GetIconBase64(exePath);
                    if (!string.IsNullOrEmpty(realIcon))
                    {
                        mainAppIcon = realIcon;
                        mainAppIsImage = true;
                    }
                }
                process.Dispose();
            }
        }

        var mainApp = new
        {
            name = Services.IconMapper.GetFriendlyName(processName),
            processName = processName,
            icon = mainAppIcon,
            isImage = mainAppIsImage,
            color = clusterColor
        };

        // 构建卫星应用（最近 3 个，去重）
        var satellitesList = new List<object>();
        var candidates = _recentApps.Where(app => app != processName).Distinct().Take(3).ToList();

        foreach (var app in candidates)
        {
            string satIcon = Services.IconMapper.GetMaterialSymbol(app);
            bool satIsImage = false;

            if (iconExtractor != null)
            {
                var process = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(app)).FirstOrDefault();
                if (process != null)
                {
                    var exePath = IconExtractorService.GetProcessPath(process.Id);
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var realIcon = iconExtractor.GetIconBase64(exePath);
                        if (!string.IsNullOrEmpty(realIcon))
                        {
                            satIcon = realIcon;
                            satIsImage = true;
                        }
                    }
                    process.Dispose();
                }
            }

            satellitesList.Add(new
            {
                name = Services.IconMapper.GetFriendlyName(app),
                processName = app,
                icon = satIcon,
                isImage = satIsImage
            });
        }

        // 构建完整 Zone B 数据
        var zoneBData = new
        {
            insight = new
            {
                icon = insight.Icon,
                text = insight.GetText()
            },
            mainApp,
            satellites = satellitesList,
            clusterName,
            clusterColor,
            sessionSeconds = (int)currentContext.Duration.TotalSeconds,
            isFocusMode = _isFocusMode,
            focusCommitment = userActivityManager.IsFocusCommitmentActive ? new
            {
                totalSeconds = userActivityManager.FocusTotalSeconds,
                remainingSeconds = userActivityManager.FocusRemainingSeconds,
                taskName = userActivityManager.FocusTaskName
            } : null
        };

        // 缓存数据（用于冻结机制）
        _lastZoneBData = zoneBData;

        _messageHandler.SendToJS(BridgeMessages.ZoneBUpdate, zoneBData);
    }

    /// <summary>
    /// 设置窗口前台状态（用于冻结机制）
    /// </summary>
    public void SetWindowForeground(bool isForeground)
    {
        _isWindowForeground = isForeground;
    }

    /// <summary>
    /// 切换专注/轻松模式
    /// </summary>
    public void ToggleFocusMode()
    {
        _isFocusMode = !_isFocusMode;
        // 立即发送更新
        SendZoneBUpdate();
    }

    public void SendDrainersUpdate()
    {
        if (_messageHandler == null) return;

        var usageCollector = _services.GetRequiredService<UsageCollectorService>();
        var clusterService = _services.GetRequiredService<ClusterService>();

        // 获取今日高耗能应用 (Top 3)
        var topDrainers = usageCollector.GetTopDrainers(3)
            .Select(d =>
            {
                var cluster = clusterService.GetClusterForProcess(d.ProcessName);

                // 友好名称映射
                string friendlyName = d.ProcessName;
                if (WebsiteRecognizer.IsBrowserProcess(d.ProcessName))
                {
                    friendlyName = WebsiteRecognizer.GetBrowserDisplayName(d.ProcessName);
                }
                else
                {
                    // 移除 .exe 后缀并首字母大写
                    friendlyName = d.ProcessName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(friendlyName))
                        friendlyName = char.ToUpper(friendlyName[0]) + friendlyName.Substring(1).ToLower();
                }

                return new
                {
                    name = friendlyName,
                    impact = (int)(d.ImpactScore * 100), // 归一化为百分比
                    color = cluster?.Color ?? "#64748b", // 默认 Slate-500
                    icon = "apps" // 暂用默认图标
                };
            })
            .ToArray();

        _messageHandler.SendDrainersUpdate(
            topDrainers,
            (int)usageCollector.TotalActiveTime.TotalMinutes,
            usageCollector.FragmentationCount
        );
    }

    /// <summary>
    /// 发送 Cluster 数据更新
    /// </summary>
    public void SendClustersUpdate()
    {
        if (_messageHandler == null) return;

        var clusterService = _services.GetRequiredService<ClusterService>();
        var clusters = clusterService.GetAllClusters();

        // 构造前端需要的 Cluster 格式
        var clustersDto = clusters.Select(c => new
        {
            id = c.Id.ToString(),
            name = c.Name,
            color = c.Color,
            apps = c.AppList.Select(app => new
            {
                id = Guid.NewGuid().ToString(), // 临时生成前端 ID
                name = app,
                icon = "extension" // 默认图标，后续可以根据进程名映射
            }).ToList()
        }).ToList();

        // 获取未分类应用 (Mock for now, should come from UsageCollector)
        var unassignedDto = new List<object>
        {
            new { id = "u1", name = "RandomApp.exe", icon = "apps" }
        };

        _messageHandler.SendToJS(BridgeMessages.ClustersLoaded, new
        {
            clusters = clustersDto,
            unassigned = unassignedDto
        });
    }

    /// <summary>
    /// 获取并聚合分析数据
    /// </summary>
    private async Task<object> GetAnalyticsDataAsync(DateTime date)
    {
        var db = _services.GetRequiredService<DatabaseService>();

        // 1. 获取基础数据
        var hourlyRecords = await db.GetHourlyUsageAsync(date);
        var fatigueSnapshots = await db.GetFatigueSnapshotsAsync(date);

        // 2. 处理 Fatigue Trend
        var fatigueTrend = fatigueSnapshots.Select(s => new
        {
            hour = s.RecordedAt.Hour + s.RecordedAt.Minute / 60.0,
            value = s.FatigueValue
        }).ToList();

        System.Diagnostics.Debug.WriteLine($"[GetAnalyticsData] 日期: {date:yyyy-MM-dd}, 疲劳快照数: {fatigueSnapshots.Count}, 趋势点数: {fatigueTrend.Count}");

        // 3. 处理 Hourly Usage (Top 8 + Others)
        var hourlyUsageSeries = new List<object>();
        var appTotalDurations = hourlyRecords
            .GroupBy(r => r.AppName)
            .Select(g => new { AppName = g.Key, TotalSeconds = g.Sum(r => r.DurationSeconds) })
            .OrderByDescending(x => x.TotalSeconds)
            .ToList();

        var topApps = appTotalDurations.Take(8).Select(x => x.AppName).ToList();
        var colors = new[] { "#8b5cf6", "#06b6d4", "#f59e0b", "#10b981", "#ef4444", "#ec4899", "#6366f1", "#84cc16" };

        // Top 8
        for (int i = 0; i < topApps.Count; i++)
        {
            var appName = topApps[i];
            var data = new double[24];
            foreach (var record in hourlyRecords.Where(r => r.AppName == appName))
            {
                data[record.Hour] = Math.Round(record.DurationSeconds / 60.0, 1);
            }

            // 简单的名称美化逻辑
            var friendlyName = appName;
            if (friendlyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                friendlyName = friendlyName.Substring(0, friendlyName.Length - 4);

            // 首字母大写
            if (!string.IsNullOrEmpty(friendlyName))
                friendlyName = char.ToUpper(friendlyName[0]) + friendlyName.Substring(1);

            hourlyUsageSeries.Add(new
            {
                appName = friendlyName,
                color = colors[i],
                hourlyData = data
            });
        }

        // Others
        var othersData = new double[24];
        foreach (var record in hourlyRecords.Where(r => !topApps.Contains(r.AppName)))
        {
            othersData[record.Hour] += record.DurationSeconds / 60.0;
        }
        if (othersData.Any(d => d > 0))
        {
            for (int i = 0; i < 24; i++) othersData[i] = Math.Round(othersData[i], 1);
            hourlyUsageSeries.Add(new
            {
                appName = "Other",
                color = "#94a3b8", // Slate-400
                hourlyData = othersData
            });
        }

        // 4. 处理 Energy Pie
        var contextAppRules = new Dictionary<string, double>(); // AppName -> Duration
        // 简单按 Context 类型聚合
        var contextDurations = new Dictionary<string, double>
        {
            { "Work/Study", 0 },
            { "Entertainment", 0 },
            { "Communication", 0 },
            { "Other", 0 }
        };

        var contextColors = new Dictionary<string, string>
        {
            { "Work/Study", "#8b5cf6" },
            { "Entertainment", "#f59e0b" },
            { "Communication", "#3b82f6" },
            { "Other", "#cbd5e1" }
        };

        foreach (var group in appTotalDurations)
        {
            // 使用 ContextClassifier
            var context = ContextClassifier.ClassifyApp(group.AppName);
            var contextName = context switch
            {
                EyeGuard.Core.Enums.ContextState.Work => "Work/Study",
                EyeGuard.Core.Enums.ContextState.Entertainment => "Entertainment",
                EyeGuard.Core.Enums.ContextState.Communication => "Communication",
                _ => "Other"
            };
            contextDurations[contextName] += group.TotalSeconds / 60.0;
        }

        var energyPie = contextDurations
            .Where(kv => kv.Value > 1) // 过滤小于1分钟的
            .Select(kv => new
            {
                name = kv.Key,
                value = Math.Round(kv.Value, 0),
                color = contextColors[kv.Key]
            }).OrderByDescending(x => x.value).ToList();

        // 5. Weekly Trends & Heatmap
        var weeklyTrends = new List<object>();
        var heatmapData = new List<object>();
        var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        // 过去7天（包括今天）
        for (int i = 6; i >= 0; i--)
        {
            var targetDate = date.AddDays(-i);
            var daySnapshots = await db.GetFatigueSnapshotsAsync(targetDate);

            // Weekly Trends
            if (daySnapshots.Count > 0)
            {
                weeklyTrends.Add(new
                {
                    day = targetDate.ToString("MM/dd"),
                    peak = Math.Round(daySnapshots.Max(s => s.FatigueValue), 0),
                    average = Math.Round(daySnapshots.Average(s => s.FatigueValue), 0)
                });
            }
            else
            {
                weeklyTrends.Add(new { day = targetDate.ToString("MM/dd"), peak = 0, average = 0 });
            }

            // Heatmap
            // 聚合每小时的平均疲劳
            var hourlyFatigue = daySnapshots
                .GroupBy(s => s.RecordedAt.Hour)
                .Select(g => new { Hour = g.Key, Avg = g.Average(s => s.FatigueValue) })
                .ToList();

            // ECharts Heatmap: dayIndex (0-6), hour (0-23), value
            // dayIndex: 0=Top(Mon/Sun depending on setup)? Let's align with Y-axis labels.
            // 假设 Y轴是日期，从上到下。i=6是今天(最下面?), i=0是7天前(最上面?)
            // 为了简单，我们让 Y 轴为 7 天前 -> 今天
            // chart data: [hour, dayIndex, value]
            int dayIndex = 6 - i; // 0..6

            foreach (var h in hourlyFatigue)
            {
                heatmapData.Add(new
                {
                    dayIndex = dayIndex,
                    hour = h.Hour,
                    value = Math.Round(h.Avg, 0)
                });
            }
        }

        // 6. The Grind & Insights
        int longestSessionMins = 0;
        int overloadMins = 0;
        double overloadPct = 0;

        if (fatigueSnapshots.Count > 0)
        {
            int snapshotInterval = 5; // assume 5 mins
            int overloadCount = fatigueSnapshots.Count(s => s.FatigueValue >= 80);
            overloadMins = overloadCount * snapshotInterval;
            overloadPct = (double)overloadMins / (fatigueSnapshots.Count * snapshotInterval) * 100;

            int currentSession = 0;
            foreach (var s in fatigueSnapshots)
            {
                if (s.FatigueValue > 20)
                {
                    currentSession += snapshotInterval;
                    longestSessionMins = Math.Max(longestSessionMins, currentSession);
                }
                else
                {
                    currentSession = 0;
                }
            }
        }

        // Generate Insight
        string insightText = "No sufficient data for this day.";
        string insightIcon = "🤷";

        if (fatigueSnapshots.Count > 0)
        {
            if (overloadMins > 60) { insightIcon = "🔥"; insightText = "High burnout risk detected! You spent over an hour in overload zone."; }
            else if (longestSessionMins > 120) { insightIcon = "⚠️"; insightText = "Long work sessions detected. Remember to take breaks using the 20-20-20 rule."; }
            else if (energyPie.Count > 0 && energyPie[0].name.Contains("Work")) { insightIcon = "💪"; insightText = "Great focus today! Most of your energy went into productive work."; }
            else { insightIcon = "✨"; insightText = "Balanced energy levels today. Keep it up!"; }
        }

        return new
        {
            date = date,
            insights = new { icon = insightIcon, text = insightText },
            fatigueSnapshots = fatigueTrend, // Reusing trend data
            hourlyUsage = hourlyUsageSeries,
            energyPie = energyPie,
            dailyRhythm = fatigueTrend, // Same as trend but maybe smoothed in frontend
            weeklyTrends = weeklyTrends,
            heatmap = heatmapData,
            timeline = new List<object>(), // Placeholder
            grindStats = new
            {
                longestSession = longestSessionMins,
                overloadMinutes = overloadMins,
                overloadPercentage = Math.Round(overloadPct, 1)
            }
        };
    }

    public void Dispose()
    {
        _messageHandler?.Dispose();
    }
}
