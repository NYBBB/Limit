using System.Text.RegularExpressions;

namespace EyeGuard.Infrastructure.Services;

/// <summary>
/// 网站识别引擎 - 通过窗口标题识别网站
/// </summary>
public class WebsiteRecognizer
{
    // 内置常见网站规则库
    private static readonly Dictionary<string, string> WebsiteRules = new()
    {
        // --- AI & 生产力 (新增重点) ---
        { "chatgpt|openai", "ChatGPT" },
        { "claude", "Claude AI" },
        { "gemini|bard", "Google Gemini" },
        { "wenxin|yiyan|文心一言", "文心一言" },
        { "poe", "Poe AI" },
        { "github copilot", "GitHub Copilot" },
        { "overleaf", "Overleaf" },

        // --- 视频/直播 ---
        { "bilibili|哔哩哔哩|_bilibili", "哔哩哔哩" },
        { "youtube", "YouTube" },
        { "netflix", "Netflix" },
        { "twitch", "Twitch" },
        { "douyin|抖音", "抖音网页版" },
        { "kuaishou|快手", "快手" },
        { "爱奇艺|iqiyi", "爱奇艺" },
        { "腾讯视频|v.qq.com", "腾讯视频" },
        { "优酷|youku", "优酷" },
        { "mgtv|芒果tv", "芒果TV" },

        // --- 社交/社区 ---
        { "weixin|wx.qq.com|微信", "微信网页版" },
        { "twitter|x.com|推特", "X (Twitter)" },
        { "facebook", "Facebook" },
        { "instagram", "Instagram" },
        { "linkedin|领英", "LinkedIn" },
        { "zhihu|知乎", "知乎" },
        { "weibo|微博", "微博" },
        { "xiaohongshu|小红书", "小红书" },
        { "tieba|贴吧", "百度贴吧" },
        { "discord", "Discord" },
        { "telegram", "Telegram" },
        { "reddit", "Reddit" },

        // --- 开发/技术 ---
        { "github", "GitHub" },
        { "gitlab", "GitLab" },
        { "gitee|码云", "Gitee" },
        { "stack overflow|stackoverflow", "Stack Overflow" },
        { "csdn", "CSDN" },
        { "博客园|cnblogs", "博客园" },
        { "掘金|juejin", "掘金" },
        { "jianshu|简书", "简书" },
        { "oschina|开源中国", "OSChina" },
        { "w3school|runoob|菜鸟教程", "编程教程" },

        // --- 设计/创意 ---
        { "figma", "Figma" },
        { "canva|可画", "Canva" },
        { "dribbble", "Dribbble" },
        { "behance", "Behance" },
        { "pinterest", "Pinterest" },
        { "huaban|花瓣", "花瓣网" },

        // --- 云服务/工具 ---
        { "aliyun|阿里云", "阿里云" },
        { "tencent cloud|腾讯云", "腾讯云" },
        { "aws", "AWS Console" },
        { "azure", "Azure Portal" },
        { "cloudflare", "Cloudflare" },
        { "vercel", "Vercel" },

        // --- 电商/购物 ---
        { "taobao|tmall|淘宝|天猫", "淘宝/天猫" },
        { "jd.com|京东", "京东" },
        { "pinduoduo|拼多多", "拼多多" },
        { "amazon|亚马逊", "Amazon" },
        { "ebay", "eBay" },
        { "闲鱼", "闲鱼" },

        // --- 资讯/搜索/阅读 ---
        { "baidu|百度", "百度" },
        { "google", "Google" },
        { "bing", "Bing" },
        { "weread|微信读书", "微信读书" },
        { "douban|豆瓣", "豆瓣" },
        { "wikipedia|维基百科", "维基百科" },
        { "toutiao|头条", "今日头条" },
        { "thepaper|澎湃", "澎湃新闻" },
        { "36kr", "36氪" },

        // --- 办公/协作 ---
        { "feishu|飞书", "飞书" },
        { "dingtalk|钉钉", "钉钉" },
        { "notion", "Notion" },
        { "wolai", "我来 wolai" },
        { "confluence", "Confluence" },
        { "jira", "Jira" },
        { "trello", "Trello" },
        { "slack", "Slack" },
        { "docs.qq.com|腾讯文档", "腾讯文档" },
        { "docs.google.com", "Google Docs" },

        // --- 邮箱 ---
        { "mail.google.com|gmail", "Gmail" },
        { "outlook.live.com|outlook", "Outlook Mail" },
        { "mail.qq.com|qq邮箱", "QQ邮箱" },
        { "mail.163.com|网易邮箱", "网易邮箱" },
    };
    
    // 已知浏览器进程名
    private static readonly HashSet<string> BrowserProcesses = new()
    {
        // 国际主流
        "msedge", "chrome", "firefox", "brave", "opera", "vivaldi", "safari",
        "arc", "tor", "waterfox", "chromium",
        
        // 国产/壳浏览器
        "360se",          // 360安全浏览器
        "360chrome",      // 360极速浏览器
        "qqbrowser",      // QQ浏览器
        "sogouexplorer",  // 搜狗浏览器
        "liebao",         // 猎豹浏览器
        "maxthon",        // 傲游
        "theworld",       // 世界之窗
        "2345explorer",   // 2345浏览器
        "quark",          // 夸克 (PC版)
        "ucbrowser"       // UC浏览器
    };
    
    /// <summary>
    /// 检测是否为浏览器进程
    /// </summary>
    public static bool IsBrowserProcess(string processName)
    {
        return BrowserProcesses.Contains(processName.ToLowerInvariant());
    }
    
    /// <summary>
    /// 从窗口标题识别网站
    /// </summary>
    /// <param name="windowTitle">窗口标题</param>
    /// <param name="websiteName">识别出的网站名称（如果成功）</param>
    /// <returns>是否成功识别</returns>
    public static bool TryRecognizeWebsite(string windowTitle, out string? websiteName)
    {
        websiteName = null;
        
        if (string.IsNullOrWhiteSpace(windowTitle))
            return false;
        
        // 移除浏览器后缀 (e.g., "- Microsoft Edge")
        var cleanTitle = Regex.Replace(windowTitle, 
            @"\s*-\s*(Microsoft\s*Edge|Google\s*Chrome|Firefox|Opera|Safari|Brave|Vivaldi|Arc|360.*|QQ.*|Sogou.*)$", 
            "", RegexOptions.IgnoreCase);
        
        // 遍历规则库
        foreach (var rule in WebsiteRules)
        {
            var patterns = rule.Key.Split('|');
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(cleanTitle, pattern, RegexOptions.IgnoreCase))
                {
                    websiteName = rule.Value;
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取浏览器显示名称
    /// </summary>
    public static string GetBrowserDisplayName(string processName)
    {
        return processName.ToLowerInvariant() switch
        {
            "msedge" => "Microsoft Edge",
            "chrome" => "Google Chrome",
            "firefox" => "Firefox",
            "brave" => "Brave",
            "opera" => "Opera",
            "arc" => "Arc Browser",
            "360se" => "360安全浏览器",
            "360chrome" => "360极速浏览器",
            "qqbrowser" => "QQ浏览器",
            "sogouexplorer" => "搜狗浏览器",
            _ => processName // 默认返回进程名
        };
    }
}
