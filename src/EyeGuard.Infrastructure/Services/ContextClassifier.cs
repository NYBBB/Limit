namespace EyeGuard.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EyeGuard.Core.Enums;

/// <summary>
/// 上下文分类器 - 根据应用和网站判断当前活动的性质
/// </summary>
public class ContextClassifier
{
    // 应用分类规则：应用进程名 -> ContextState
    private static readonly Dictionary<string, ContextState> AppRules = new()
    {
        // --- 开发与技术 (Work) ---
        { "devenv", ContextState.Work },           // Visual Studio
        { "code", ContextState.Work },             // VS Code
        { "rider", ContextState.Work },            // JetBrains Rider
        { "idea64", ContextState.Work },           // IntelliJ
        { "pycharm64", ContextState.Work },        // PyCharm
        { "webstorm64", ContextState.Work },       // WebStorm
        { "goland64", ContextState.Work },         // GoLand
        { "datagrip64", ContextState.Work },       // DataGrip
        { "clion64", ContextState.Work },          // CLion
        { "studio64", ContextState.Work },         // Android Studio
        { "unity editor", ContextState.Work },     // Unity
        { "unrealeditor", ContextState.Work },     // Unreal Engine
        { "godot", ContextState.Work },            // Godot
        { "sourcetree", ContextState.Work },       // SourceTree
        { "postman", ContextState.Work },          // Postman
        { "docker", ContextState.Work },           // Docker Desktop
        { "windowsterminal", ContextState.Work },  // Windows Terminal
        { "cmd", ContextState.Work },              // CMD
        { "powershell", ContextState.Work },       // PowerShell
        { "pwsh", ContextState.Work },             // PowerShell Core
        { "antigravity", ContextState.Work },      // Antigravity

        // --- 办公与文档 (Work) ---
        { "excel", ContextState.Work },
        { "winword", ContextState.Work },
        { "powerpnt", ContextState.Work },
        { "onenote", ContextState.Work },
        { "wps", ContextState.Work },              // WPS
        { "wpp", ContextState.Work },              // WPS PPT
        { "et", ContextState.Work },               // WPS Excel
        { "notion", ContextState.Work },           // Notion App
        { "obsidian", ContextState.Work },         // Obsidian
        { "typora", ContextState.Work },           // Typora
        { "notepad++", ContextState.Work },
        { "sublime_text", ContextState.Work },
        { "acrobat", ContextState.Work },          // Adobe Acrobat

        // --- 设计与创作 (Work - High Load) ---
        { "photoshop", ContextState.Work },
        { "illustrator", ContextState.Work },
        { "indesign", ContextState.Work },
        { "adobe premiere pro", ContextState.Work },
        { "afterfx", ContextState.Work },
        { "blender", ContextState.Work },
        { "maya", ContextState.Work },
        { "3dsmax", ContextState.Work },
        { "autocad", ContextState.Work },
        { "figma", ContextState.Work },
        { "sketch", ContextState.Work },
        { "obs64", ContextState.Work },            // OBS Studio

        // --- 娱乐与游戏 (Entertainment) ---
        { "spotify", ContextState.Entertainment },
        { "cloudmusic", ContextState.Entertainment }, // 网易云
        { "qqmusic", ContextState.Entertainment },
        { "steam", ContextState.Entertainment },
        { "epicgameslauncher", ContextState.Entertainment },
        { "battlenet", ContextState.Entertainment },
        { "origin", ContextState.Entertainment },
        { "wegame", ContextState.Entertainment },
        { "leagueclient", ContextState.Entertainment }, // LOL
        { "valorant", ContextState.Entertainment },
        { "genshinimpact", ContextState.Entertainment },
        { "minecraft", ContextState.Entertainment },
        { "roblox", ContextState.Entertainment },
        { "vlc", ContextState.Entertainment },
        { "potplayer", ContextState.Entertainment },
        { "mpv", ContextState.Entertainment },

        // --- 社交与通讯 (Communication) ---
        { "wechat", ContextState.Communication },
        { "qq", ContextState.Communication },
        { "tim", ContextState.Communication },
        { "dingtalk", ContextState.Communication }, // 钉钉
        { "lark", ContextState.Communication },     // 飞书
        { "feishu", ContextState.Communication },
        { "teams", ContextState.Communication },
        { "zoom", ContextState.Communication },
        { "skype", ContextState.Communication },
        { "slack", ContextState.Communication },
        { "discord", ContextState.Communication },
        { "telegram", ContextState.Communication },
        { "whatsapp", ContextState.Communication },
        { "outlook", ContextState.Communication },
        { "thunderbird", ContextState.Communication },
        { "foxmail", ContextState.Communication },

        // --- 系统与工具 (Other/Neutral) ---
        { "explorer", ContextState.Other },         // 资源管理器
        { "taskmgr", ContextState.Other },          // 任务管理器
        { "regedit", ContextState.Other },          // 注册表
        { "searchhost", ContextState.Other },       // Windows Search
        { "lockapp", ContextState.Other },          // 锁屏
    };

    // 网站分类规则：关键词 -> ContextState
    private static readonly Dictionary<string, ContextState> WebsiteContextRules = new()
    {
        // --- 优先级高：具体子域名 (Work) ---
        { "docs.google", ContextState.Work },
        { "docs.qq", ContextState.Work },
        { "shimo.im", ContextState.Work },        // 石墨文档
        { "wolai", ContextState.Work },
        { "feishu.cn", ContextState.Work },       // 飞书文档部分
        { "飞书", ContextState.Work },
        { "yuque", ContextState.Work },           // 语雀
        { "mail.google", ContextState.Communication },
        { "mail.qq", ContextState.Communication },
        { "mail.163", ContextState.Communication },
        { "outlook.live", ContextState.Communication },

        // --- 学习与知识 (Work/Learning) ---
        { "github.com", ContextState.Work },
        { "github", ContextState.Work },
        { "gitlab.com", ContextState.Work },
        { "gitlab", ContextState.Work },
        { "gitee", ContextState.Work },
        { "stackoverflow.com", ContextState.Work },
        { "stackoverflow", ContextState.Work },
        { "csdn.net", ContextState.Work },
        { "csdn", ContextState.Work },
        { "juejin.cn", ContextState.Work },
        { "juejin", ContextState.Work },          // 掘金
        { "cnblogs.com", ContextState.Work },
        { "cnblogs", ContextState.Work },
        { "jianshu.com", ContextState.Work },
        { "jianshu", ContextState.Work },
        { "segmentfault.com", ContextState.Work },
        { "segmentfault", ContextState.Work },
        { "leetcode.com", ContextState.Work },
        { "leetcode", ContextState.Work },
        { "coursera.org", ContextState.Work },
        { "coursera", ContextState.Work },
        { "udemy.com", ContextState.Work },
        { "udemy", ContextState.Work },
        { "edx.org", ContextState.Work },
        { "edx", ContextState.Work },
        { "bilibili.com/read", ContextState.Work }, // B站专栏
        { "wikipedia.org", ContextState.Work },
        { "wikipedia", ContextState.Work },
        { "chatgpt.com", ContextState.Work },
        { "chatgpt", ContextState.Work },
        { "claude.ai", ContextState.Work },
        { "claude", ContextState.Work },
        { "minimax", ContextState.Work },
        { "gemini.google.com", ContextState.Work },
        { "gemini", ContextState.Work },
        { "poe.com", ContextState.Work },
        { "instructure.com", ContextState.Work }, // Canvas LMS
        { "canvas", ContextState.Work },
        { "notion.so", ContextState.Work },
        { "notion", ContextState.Work },

        // --- 设计 (Work) ---
        { "figma.com", ContextState.Work },
        { "figma", ContextState.Work },
        { "canva.com", ContextState.Work },
        { "canva", ContextState.Work },
        { "dribbble.com", ContextState.Work },
        { "dribbble", ContextState.Work },
        { "behance.net", ContextState.Work },
        { "behance", ContextState.Work },
        { "huaban.com", ContextState.Work },
        { "huaban", ContextState.Work },          // 花瓣

        // --- 视频与直播 (Entertainment) ---
        { "youtube.com", ContextState.Entertainment },
        { "youtube", ContextState.Entertainment },
        { "bilibili.com", ContextState.Entertainment },
        { "bilibili", ContextState.Entertainment },
        { "哔哩哔哩", ContextState.Entertainment },  
        { "netflix.com", ContextState.Entertainment },
        { "netflix", ContextState.Entertainment },
        { "twitch.tv", ContextState.Entertainment },
        { "twitch", ContextState.Entertainment },
        { "douyin.com", ContextState.Entertainment },
        { "douyin", ContextState.Entertainment },
        { "抖音", ContextState.Entertainment },      
        { "kuaishou.com", ContextState.Entertainment },
        { "kuaishou", ContextState.Entertainment },
        { "快手", ContextState.Entertainment },      
        { "iqiyi.com", ContextState.Entertainment },
        { "iqiyi", ContextState.Entertainment },
        { "爱奇艺", ContextState.Entertainment },    
        { "v.qq.com", ContextState.Entertainment },
        { "v.qq", ContextState.Entertainment },
        { "腾讯视频", ContextState.Entertainment },  
        { "youku.com", ContextState.Entertainment },
        { "youku", ContextState.Entertainment },
        { "优酷", ContextState.Entertainment },      
        { "mgtv.com", ContextState.Entertainment },
        { "mgtv", ContextState.Entertainment },
        { "芒果tv", ContextState.Entertainment },    
        { "douyu.com", ContextState.Entertainment },
        { "douyu", ContextState.Entertainment },
        { "斗鱼", ContextState.Entertainment },      
        { "huya.com", ContextState.Entertainment },
        { "huya", ContextState.Entertainment },
        { "虎牙", ContextState.Entertainment },      

        // --- 音乐 (Entertainment) ---
        { "spotify.com", ContextState.Entertainment },
        { "spotify", ContextState.Entertainment },
        { "music.163.com", ContextState.Entertainment },
        { "music.163", ContextState.Entertainment },
        { "网易云", ContextState.Entertainment },    
        { "y.qq.com", ContextState.Entertainment },
        { "y.qq", ContextState.Entertainment },
        { "qq音乐", ContextState.Entertainment },    

        // --- 社交媒体 (Communication/Entertainment) ---
        { "weibo.com", ContextState.Entertainment },
        { "weibo", ContextState.Entertainment },
        { "微博", ContextState.Entertainment },      
        { "twitter.com", ContextState.Communication },
        { "twitter", ContextState.Communication },
        { "x.com", ContextState.Communication },
        { "facebook.com", ContextState.Communication },
        { "facebook", ContextState.Communication },
        { "instagram.com", ContextState.Communication },
        { "instagram", ContextState.Communication },
        { "reddit.com", ContextState.Communication },
        { "reddit", ContextState.Communication },
        { "zhihu.com", ContextState.Communication },
        { "zhihu", ContextState.Communication },
        { "知乎", ContextState.Communication },      
        { "douban.com", ContextState.Communication },
        { "douban", ContextState.Communication },
        { "豆瓣", ContextState.Communication },      
        { "xiaohongshu.com", ContextState.Communication },
        { "xiaohongshu", ContextState.Communication },
        { "小红书", ContextState.Communication },    
        { "tieba.baidu.com", ContextState.Communication },
        { "tieba", ContextState.Communication },
        { "贴吧", ContextState.Communication },             
        { "discord.com", ContextState.Communication },
        { "discord", ContextState.Communication },
        { "slack.com", ContextState.Communication },
        { "slack", ContextState.Communication },
        { "dingtalk.com", ContextState.Communication },
        { "dingtalk", ContextState.Communication },
        { "outlook.office.com", ContextState.Communication },
        { "outlook", ContextState.Communication },   

        // --- 购物 (Other/Neutral) ---
        { "taobao.com", ContextState.Other },
        { "淘宝", ContextState.Other },              
        { "jd.com", ContextState.Other },
        { "京东", ContextState.Other },              
        { "tmall", ContextState.Other },
        { "天猫", ContextState.Other },              
        { "amazon", ContextState.Other },
        { "pinduoduo", ContextState.Other },
        { "拼多多", ContextState.Other },            
    };
    
    /// <summary>
    /// 根据应用名称分类
    /// </summary>
    public static ContextState ClassifyApp(string processName)
    {
        var lowerName = processName.ToLowerInvariant();
        
        // 1. 精确匹配
        if (AppRules.TryGetValue(lowerName, out var state))
        {
            return state;
        }

        // 2. 模糊匹配 (Fallback)
        // 比如 "idea64.exe" 没命中的话，"idea" 可能会命中
        if (lowerName.Contains("visualstudio")) return ContextState.Work;
        if (lowerName.Contains("jetbrains")) return ContextState.Work;
        if (lowerName.Contains("player")) return ContextState.Entertainment;
        
        // 默认为其他
        return ContextState.Other;
    }
    
    /// <summary>
    /// 根据网站名称、窗口标题或 URL 分类
    /// </summary>
    public static ContextState ClassifyWebsite(string? websiteName, string? windowTitle, string? url)
    {
        // Limit 2.0: 提取域名（如果有 URL）
        string? domain = null;
        if (!string.IsNullOrEmpty(url))
        {
            domain = ExtractDomain(url);
        }
        
        // 统一策略：拼接域名 + 友好名称 + 标题，一次性匹配所有规则
        // 这样既能匹配 "chatgpt.com"，也能匹配 "哔哩哔哩" 或 "bilibili"
        var combinedText = $"{domain ?? ""} {websiteName ?? ""} {windowTitle ?? ""}".ToLowerInvariant();
        
        // 按关键词长度倒序遍历，确保长匹配优先（如 "docs.google" 优先于 "google"）
        var sortedRules = WebsiteContextRules
            .OrderByDescending(x => x.Key.Length);

        foreach (var rule in sortedRules)
        {
            if (combinedText.Contains(rule.Key.ToLowerInvariant()))
            {
                return rule.Value;
            }
        }
        
        // 默认浏览器行为视为"其他"
        return ContextState.Other;
    }
    
    /// <summary>
    /// 从 URL 提取域名
    /// </summary>
    private static string ExtractDomain(string url)
    {
        try
        {
            // 如果URL没有协议，添加默认协议
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }
            
            var uri = new Uri(url);
            return uri.Host.ToLowerInvariant();
        }
        catch
        {
            return "";
        }
    }
    
    /// <summary>
    /// 综合分类（应用 + 网站 + URL）- Limit 2.0
    /// </summary>
    /// <param name="processName">进程名</param>
    /// <param name="websiteName">识别出的网站名（仅浏览器有效）</param>
    /// <param name="windowTitle">窗口标题</param>
    /// <param name="url">浏览器 URL（Limit 2.0 新增）</param>
    public static ContextState Classify(string processName, string? websiteName, string? windowTitle, string? url = null)
    {
        // 如果是浏览器，根据 URL/网站/标题分类
        if (WebsiteRecognizer.IsBrowserProcess(processName))
        {
            return ClassifyWebsite(websiteName, windowTitle, url);
        }
        
        // 否则根据应用分类
        return ClassifyApp(processName);
    }
    
    /// <summary>
    /// 获取 ContextState 对应的 LoadWeight
    /// </summary>
    public static double GetLoadWeight(ContextState state)
    {
        return state switch
        {
            // Work: 烧脑，全速积累疲劳
            ContextState.Work => 1.0,           
            // Communication: 比较费神 (Context switch high)，打折不多
            ContextState.Communication => 0.7,  
            // Entertainment: 放松模式，疲劳积累很慢 (Limit 2.0 核心)
            ContextState.Entertainment => 0.3,  
            // Other: 中性操作 (如整理文件)，略低于工作
            ContextState.Other => 0.6,          
            // 容错
            _ => 0.8
        };
    }
    
    /// <summary>
    /// 获取 ContextState 的显示名称
    /// </summary>
    public static string GetContextName(ContextState state)
    {
        return state switch
        {
            ContextState.Work => "工作/学习",
            ContextState.Entertainment => "娱乐/休闲",
            ContextState.Communication => "社交/沟通",
            ContextState.Other => "其他",
            _ => "未知"
        };
    }
}
