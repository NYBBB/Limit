namespace EyeGuard.UI.Bridge;

/// <summary>
/// Bridge 消息类型常量定义
/// 与前端 src/bridge/index.ts 保持同步
/// </summary>
public static class BridgeMessages
{
    // ============ C# → JS 事件 ============

    /// <summary>疲劳数据更新</summary>
    public const string FatigueUpdate = "FATIGUE_UPDATE";

    /// <summary>上下文数据更新</summary>
    public const string ContextUpdate = "CONTEXT_UPDATE";

    /// <summary>消耗排行更新</summary>
    public const string DrainersUpdate = "DRAINERS_UPDATE";

    /// <summary>设置数据更新</summary>
    public const string SettingsUpdate = "SETTINGS_UPDATE";

    /// <summary>热力图数据更新</summary>
    public const string HeatmapUpdate = "HEATMAP_UPDATE";

    /// <summary>趋势数据更新</summary>
    public const string TrendUpdate = "TREND_UPDATE";

    // ============ JS → C# 命令 ============

    /// <summary>开始专注模式</summary>
    public const string StartFocus = "START_FOCUS";

    /// <summary>停止专注模式</summary>
    public const string StopFocus = "STOP_FOCUS";

    /// <summary>校准疲劳值</summary>
    public const string CalibrateFatigue = "CALIBRATE_FATIGUE";

    /// <summary>更新设置</summary>
    public const string UpdateSettings = "UPDATE_SETTINGS";

    /// <summary>导航到页面</summary>
    public const string Navigate = "NAVIGATE";

    /// <summary>请求数据刷新</summary>
    public const string RequestRefresh = "REQUEST_REFRESH";

    /// <summary>切换 Focusing/Chilling 模式</summary>
    public const string ToggleFocusingMode = "TOGGLE_FOCUSING_MODE";

    /// <summary>测试 Ping (开发用)</summary>
    public const string TestPing = "TEST_PING";

    // Analytics
    public const string RequestAnalytics = "REQUEST_ANALYTICS";
    public const string RequestDebugStatus = "REQUEST_DEBUG_STATUS";
    public const string SetFatigueValue = "SET_FATIGUE_VALUE";

    public const string RequestSettings = "REQUEST_SETTINGS";
    public const string SaveSettings = "SAVE_SETTINGS";

    // Cluster 相关
    public const string RequestClusters = "REQUEST_CLUSTERS";
    public const string UpdateClusters = "UPDATE_CLUSTERS";
    public const string ClustersLoaded = "CLUSTERS_LOADED";

    // 未分类应用相关
    public const string RequestUnclassifiedApps = "REQUEST_UNCLASSIFIED_APPS";
    public const string UnclassifiedAppsLoaded = "UNCLASSIFIED_APPS_LOADED";

    // Zone B 相关 (Cluster Galaxy)
    /// <summary>Zone B 完整数据更新</summary>
    public const string ZoneBUpdate = "ZONE_B_UPDATE";
    /// <summary>切换专注/轻松模式</summary>
    public const string ToggleFocusMode = "TOGGLE_FOCUS_MODE";
    /// <summary>开始专注承诺</summary>
    public const string StartFocusCommitment = "START_FOCUS_COMMITMENT";
    /// <summary>停止专注承诺</summary>
    public const string StopFocusCommitment = "STOP_FOCUS_COMMITMENT";
}

/// <summary>
/// 疲劳状态枚举
/// 与前端 types/index.ts 保持同步
/// </summary>
public static class FatigueStates
{
    public const string Fresh = "FRESH";
    public const string Focused = "FOCUSED";
    public const string Flow = "FLOW";
    public const string Strain = "STRAIN";
    public const string Drain = "DRAIN";
    public const string Care = "CARE";
}

/// <summary>
/// 应用簇类型
/// 与前端 types/index.ts 保持同步
/// </summary>
public static class ClusterTypes
{
    public const string Coding = "coding";
    public const string Writing = "writing";
    public const string Meeting = "meeting";
    public const string Research = "research";
    public const string Creative = "creative";
    public const string Media = "media";
    public const string Social = "social";
    public const string Other = "other";
}
