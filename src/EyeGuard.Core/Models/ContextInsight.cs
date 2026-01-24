namespace EyeGuard.Core.Models;

/// <summary>
/// 上下文洞察类型（Context Insight Type）
/// </summary>
public enum InsightType
{
    Dominance,      // 专注模式：当前应用在过去 1 小时内占用超过 60%
    Fragmentation,  // 碎片化警告：过去 10 分钟切换窗口超过 15 次
    FatigueHigh,    // 疲劳关联：疲劳值 > 70% 且还在高负载应用中
    ClusterFlow,    // 工作流识别：检测到用户在预定义的 Cluster 内切换
    Recovery        // 摸鱼/恢复：在低负载应用停留超过 10 分钟
}

/// <summary>
/// 上下文洞察模型 - Context Monitor 微文案
/// Limit 3.0: 实时洞察生成器，根据当前数据特征选择贴切文案
/// </summary>
public class ContextInsight
{
    public InsightType Type { get; set; }
    public string Icon { get; set; } = "";
    public string TextCN { get; set; } = "";
    public string TextEN { get; set; } = "";
    public bool IsActive { get; set; }
    
    /// <summary>
    /// 获取当前语言的文案（默认中文）
    /// </summary>
    public string GetText(bool useEnglish = false)
    {
        return useEnglish ? TextEN : TextCN;
    }
}
