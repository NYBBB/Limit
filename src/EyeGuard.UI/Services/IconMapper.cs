namespace EyeGuard.UI.Services;

/// <summary>
/// 图标映射服务 - 将应用名和网站名映射到 Segoe MDL2 图标
/// </summary>
public static class IconMapper
{
    // 应用图标映射 (Segoe MDL2 Assets)
    private static readonly Dictionary<string, string> AppIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- 浏览器 ---
        { "msedge", "\uE774" },      // Globe
        { "chrome", "\uE774" },
        { "firefox", "\uE774" },
        { "brave", "\uE774" },
        { "arc", "\uE774" },
        { "qqbrowser", "\uE774" },
        { "360se", "\uE774" },
        
        // --- IDE & 代码编辑器 ---
        { "devenv", "\uE943" },      // Visual Studio (Code icon)
        { "code", "\uE943" },        // VS Code
        { "rider", "\uE943" },       // JetBrains Rider
        { "idea64", "\uE943" },      // IntelliJ IDEA
        { "pycharm64", "\uE943" },   // PyCharm
        { "webstorm64", "\uE943" },  // WebStorm
        { "goland64", "\uE943" },    // GoLand
        { "sublime_text", "\uE81A" },// Edit (Sublime)
        { "notepad++", "\uE81A" },   // Edit
        { "atom", "\uE81A" },

        // --- 终端 & 系统工具 ---
        { "wt", "\uE756" },          // Windows Terminal (CommandPrompt)
        { "cmd", "\uE756" },
        { "powershell", "\uE756" },
        { "pwsh", "\uE756" },        // PowerShell Core
        { "ubuntu", "\uE756" },      // WSL
        { "putty", "\uE756" },
        { "xshell", "\uE756" },
        { "mobaxterm", "\uE756" },
        { "explorer", "\uE8B7" },    // File Explorer
        { "taskmgr", "\uE9D9" },     // Diagnostic (Task Manager)
        { "regedit", "\uE770" },     // Settings
        { "vmware", "\uE7F8" },      // Devices3 (Server-like)
        { "virtualbox", "\uE7F8" },

        // --- 办公与文档 ---
        { "winword", "\uE8A5" },     // Word
        { "excel", "\uF1E3" },       // Excel
        { "powerpnt", "\uE8FD" },    // PowerPoint
        { "onenote", "\uE70B" },     // OneNote
        { "outlook", "\uE715" },     // Mail
        { "acrobat", "\uEA90" },     // PDF
        { "wps", "\uE8A5" },         // WPS Office
        
        // --- 创意与设计 (Adobe等) ---
        { "photoshop", "\uE790" },   // Color (Brush)
        { "illustrator", "\uE790" }, 
        { "premiere", "\uE714" },    // Video (Play)
        { "afterfx", "\uE714" },
        { "blender", "\uE9ca" },     // 3D Print / Box
        { "unity editor", "\uE9ca" },
        { "figma", "\uE790" },
        
        // --- 通讯 & 社交 ---
        { "wechat", "\uE8BD" },      // Message
        { "qq", "\uE8BD" },
        { "tim", "\uE8BD" },
        { "dingtalk", "\uE902" },    // People
        { "lark", "\uE902" },        // Feishu
        { "feishu", "\uE902" },
        { "teams", "\uE902" },
        { "skype", "\uE902" },
        { "discord", "\uE8BD" },
        { "telegram", "\uE8BD" },
        { "slack", "\uE8BD" },

        // --- 媒体播放 ---
        { "spotify", "\uE8D6" },     // MusicNote
        { "cloudmusic", "\uE8D6" },  // 网易云音乐
        { "qqmusic", "\uE8D6" },     // QQ音乐
        { "potplayer", "\uE714" },   // Play
        { "vlc", "\uE714" },
        { "mpv", "\uE714" },

        // --- 游戏 & 启动器 ---
        { "steam", "\uE7FC" },       // Game
        { "epicgameslauncher", "\uE7FC" },
        { "battlenet", "\uE7FC" },
        { "origin", "\uE7FC" },
        { "wegame", "\uE7FC" },
        { "minecraft", "\uE9CA" },   // Box
        { "roblox", "\uE9CA" },
        { "league of legends", "\uE7FC" },
        { "valorant", "\uE7FC" },
    };
    
    // 网站图标映射
    private static readonly Dictionary<string, string> WebsiteIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- AI ---
        { "ChatGPT", "\uE945" },      // Robot
        { "Claude AI", "\uE945" },
        { "Google Gemini", "\uE945" },
        { "GitHub Copilot", "\uE945" },
        { "文心一言", "\uE945" },

        // --- 视频 ---
        { "哔哩哔哩", "\uE714" },     // Play
        { "YouTube", "\uE714" },
        { "Netflix", "\uE714" },
        { "Twitch", "\uE93E" },       // Streaming/Camera
        { "抖音网页版", "\uE714" },

        // --- 社交 ---
        { "微信网页版", "\uE8BD" },   // Message
        { "知乎", "\uF575" },         // Question (Brain/Thought) - fallback to chat \uE8BD if not available
        { "微博", "\uE8BD" },
        { "小红书", "\uE8BD" },
        { "X (Twitter)", "\uE8BD" },
        { "Discord", "\uE8BD" },
        { "Reddit", "\uE8BD" },
        { "LinkedIn", "\uE902" },     // People

        // --- 开发 ---
        { "GitHub", "\uE907" },       // Git/Source (Use generic code or folder if generic git icon missing) -> \uE943 Code
        { "GitLab", "\uE943" },
        { "Stack Overflow", "\uE943" },
        { "CSDN", "\uE943" },

        // --- 云服务 ---
        { "阿里云", "\uE753" },       // Cloud
        { "腾讯云", "\uE753" },
        { "AWS Console", "\uE753" },
        { "Azure Portal", "\uE753" },
        
        // --- 设计 ---
        { "Figma", "\uE790" },        // Color/Brush
        { "Canva", "\uE790" },
        { "Dribbble", "\uE790" },

        // --- 搜索 ---
        { "Google", "\uE721" },       // Search
        { "百度", "\uE721" },
        { "Bing", "\uE721" },
        { "维基百科", "\uE8F4" },     // Library/Book

        // --- 电商 ---
        { "淘宝/天猫", "\uE7BF" },    // ShoppingCart
        { "京东", "\uE7BF" },
        { "Amazon", "\uE7BF" },

        // --- 邮箱 ---
        { "Gmail", "\uE715" },        // Mail
        { "Outlook Mail", "\uE715" },
        { "QQ邮箱", "\uE715" },
        
        // 阅读/笔记
        { "微信读书", "\uE8F4" },    // Library
        { "豆瓣", "\uE8F4" },
        { "Notion", "\uE70B" },      // Note
        
        // 办公工具
        { "飞书", "\uE902" },        // People
        { "钉钉", "\uE902" },
        { "Confluence", "\uE8A5" },  // Page
        { "Jira", "\uE73A" },        // Tasks

        
    };
    
    /// <summary>
    /// 获取应用图标（Segoe MDL2 Glyph）
    /// </summary>
    public static string GetAppIcon(string appName)
    {
        // 先尝试完全匹配
        if (AppIcons.TryGetValue(appName, out var icon)) return icon;

        // 模糊匹配逻辑 (Fallback)
        var lowerName = appName.ToLowerInvariant();
        if (lowerName.Contains("visualstudio")) return "\uE943"; // VS
        if (lowerName.Contains("rider")) return "\uE943";
        if (lowerName.Contains("idea")) return "\uE943";
        if (lowerName.Contains("python")) return "\uE943";
        if (lowerName.Contains("node")) return "\uE756";
        if (lowerName.Contains("player")) return "\uE714";
        if (lowerName.Contains("music")) return "\uE8D6";
        if (lowerName.Contains("game")) return "\uE7FC";
        
        return "\uE8FC"; // 默认：App通用图标
    }
    
    /// <summary>
    /// 获取网站图标（Segoe MDL2 Glyph）
    /// </summary>
    public static string GetWebsiteIcon(string websiteName)
    {
        return WebsiteIcons.TryGetValue(websiteName, out var icon) ? icon : "\uE774"; // 默认：网页图标
    }
    
    /// <summary>
    /// 是否为浏览器应用
    /// </summary>
    public static bool IsBrowser(string appName)
    {
        // 扩展的浏览器判断列表
        var name = appName.ToLowerInvariant();
        return name.Contains("edge") || 
               name.Contains("chrome") || 
               name.Contains("firefox") || 
               name.Contains("browser") || 
               name.Contains("explorer") && !name.Equals("explorer") || // 排除资源管理器
               name.Contains("safari") ||
               name.Contains("opera") ||
               name.Contains("brave") ||
               name.Contains("arc") ||
               name.Contains("vivaldi");
    }
    
    /// <summary>
    /// 获取友好的应用名称（用于显示）
    /// </summary>
    public static string GetFriendlyName(string appName)
    {
        // 应用名称映射表
        var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 浏览器
            { "msedge", "Edge 浏览器" },
            { "chrome", "Chrome 浏览器" },
            { "firefox", "Firefox 浏览器" },
            { "brave", "Brave 浏览器" },
            { "arc", "Arc 浏览器" },
            { "qqbrowser", "QQ浏览器" },
            { "360se", "360浏览器" },
            
            // IDE
            { "devenv", "Visual Studio" },
            { "code", "VS Code" },
            { "rider", "Rider" },
            { "idea64", "IntelliJ IDEA" },
            { "pycharm64", "PyCharm" },
            { "webstorm64", "WebStorm" },
            { "goland64", "GoLand" },
            { "sublime_text", "Sublime Text" },
            { "notepad++", "Notepad++" },
            
            // 终端
            { "wt", "Windows Terminal" },
            { "cmd", "命令提示符" },
            { "powershell", "PowerShell" },
            { "pwsh", "PowerShell Core" },
            { "ubuntu", "Ubuntu (WSL)" },
            
            // 办公
            { "winword", "Microsoft Word" },
            { "excel", "Microsoft Excel" },
            { "powerpnt", "PowerPoint" },
            { "onenote", "OneNote" },
            { "outlook", "Outlook" },
            { "wps", "WPS Office" },
            
            // 通讯
            { "wechat", "微信" },
            { "qq", "QQ" },
            { "tim", "TIM" },
            { "dingtalk", "钉钉" },
            { "lark", "飞书" },
            { "feishu", "飞书" },
            { "teams", "Teams" },
            { "discord", "Discord" },
            
            // 媒体
            { "potplayer", "PotPlayer" },
            { "vlc", "VLC Player" },
            { "spotify", "Spotify" },
            { "cloudmusic", "网易云音乐" },
            { "qqmusic", "QQ音乐" },
            
            // 其他
            { "explorer", "文件资源管理器" },
            { "steam", "Steam" },
            { "unity editor", "Unity" },
        };
        
        // 先尝试完全匹配
        if (friendlyNames.TryGetValue(appName, out var friendlyName))
            return friendlyName;
        
        // 如果没有映射，返回首字母大写的原名
        return appName.Length > 0 
            ? char.ToUpper(appName[0]) + appName.Substring(1) 
            : appName;
    }
}
