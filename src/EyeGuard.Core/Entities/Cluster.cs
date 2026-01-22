namespace EyeGuard.Core.Entities;

using SQLite;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// 工作流簇实体 - Limit 3.0
/// 用于定义一组相关应用，簇内切换无惩罚。
/// 例如：Coding Cluster = Unity + Visual Studio + Chrome (StackOverflow)
/// </summary>
public class Cluster
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    /// <summary>
    /// 簇名称，如 "Coding"、"Writing"、"Meeting"
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 簇描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 包含的应用进程名列表（JSON 序列化存储）
    /// </summary>
    public string AppListJson { get; set; } = "[]";
    
    /// <summary>
    /// 包含的浏览器关键词列表（用于自动归类浏览器页面，JSON 序列化存储）
    /// 例如：["Docs", "API", "StackOverflow", "GitHub"]
    /// </summary>
    public string BrowserKeywordsJson { get; set; } = "[]";
    
    /// <summary>
    /// 是否为系统预设（预设不可删除）
    /// </summary>
    public bool IsSystemPreset { get; set; } = false;
    
    /// <summary>
    /// 簇的负载权重（影响疲劳增长速率）
    /// 工作类 = 1.0，娱乐类 = 0.3
    /// </summary>
    public double LoadWeight { get; set; } = 1.0;
    
    /// <summary>
    /// 显示颜色（Hex 格式）
    /// </summary>
    public string Color { get; set; } = "#8A2BE2";
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // ===== 便捷方法 =====
    
    /// <summary>
    /// 获取应用进程名列表
    /// </summary>
    [Ignore]
    public List<string> AppList
    {
        get => JsonSerializer.Deserialize<List<string>>(AppListJson) ?? new List<string>();
        set => AppListJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// 获取浏览器关键词列表
    /// </summary>
    [Ignore]
    public List<string> BrowserKeywords
    {
        get => JsonSerializer.Deserialize<List<string>>(BrowserKeywordsJson) ?? new List<string>();
        set => BrowserKeywordsJson = JsonSerializer.Serialize(value);
    }
    
    /// <summary>
    /// 检查指定进程是否属于此簇
    /// </summary>
    public bool ContainsProcess(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        var apps = AppList;
        return apps.Any(app => 
            app.Equals(processName, StringComparison.OrdinalIgnoreCase) ||
            processName.Contains(app, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 检查浏览器页面标题是否匹配此簇的关键词
    /// </summary>
    public bool MatchesBrowserTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return false;
        var keywords = BrowserKeywords;
        return keywords.Any(kw => 
            title.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 添加应用到簇
    /// </summary>
    public void AddApp(string processName)
    {
        var apps = AppList;
        if (!apps.Contains(processName, StringComparer.OrdinalIgnoreCase))
        {
            apps.Add(processName);
            AppList = apps;
        }
    }
    
    /// <summary>
    /// 从簇中移除应用
    /// </summary>
    public void RemoveApp(string processName)
    {
        var apps = AppList;
        apps.RemoveAll(a => a.Equals(processName, StringComparison.OrdinalIgnoreCase));
        AppList = apps;
    }
}
