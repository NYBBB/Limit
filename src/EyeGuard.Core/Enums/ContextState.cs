namespace EyeGuard.Core.Enums;

/// <summary>
/// 上下文状态 - 用于分类当前活动的性质
/// </summary>
public enum ContextState
{
    /// <summary>
    /// 工作/学习 (高负荷) - LoadWeight = 1.0
    /// 例如: VS Code, Office, Terminal, 技术文档网站
    /// </summary>
    Work,
    
    /// <summary>
    /// 娱乐/休闲 (低负荷) - LoadWeight = 0.3
    /// 例如: YouTube, Netflix, 游戏, 音乐网站
    /// </summary>
    Entertainment,
    
    /// <summary>
    /// 社交/沟通 (中等负荷) - LoadWeight = 0.6
    /// 例如: Discord, Slack, 微信, 邮件
    /// </summary>
    Communication,
    
    /// <summary>
    /// 其他/未分类 - LoadWeight = 0.8
    /// </summary>
    Other
}
