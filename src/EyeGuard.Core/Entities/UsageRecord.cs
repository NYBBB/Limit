using System;
using SQLite;
namespace EyeGuard.Core.Entities;

[Table("UsageRecords")]
public class UsageRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [Indexed]
    public DateTime Date { get; set; } // yyyy-MM-dd
    
    // 应用层信息
    public string AppName { get; set; } // Process Name (e.g., "msedge")
    public string AppPath { get; set; } 
    
    // 网站层信息（可选，仅浏览器使用）
    public string? WebsiteName { get; set; }     // 识别的网站名称 (e.g., "哔哩哔哩")
    public string? WebsiteDomain { get; set; }   // 域名（预留，当前为null）
    public string? PageTitle { get; set; }       // 原始窗口标题（未识别的保留）
    
    public int DurationSeconds { get; set; }
    public DateTime LastActiveTime { get; set; }
}
