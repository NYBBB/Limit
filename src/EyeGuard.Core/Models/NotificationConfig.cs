using System;
using System.Collections.Generic;

namespace EyeGuard.Core.Models;

/// <summary>
/// 通知的配置模型
/// </summary>
public class NotificationConfig
{
    // 干预提醒
    public string InterventionTitle { get; set; } = "疲劳提醒 ({0}%)";
    public string InterventionButtonRest { get; set; } = "休息一下";
    public string InterventionButtonSnooze { get; set; } = "稍后提醒";
    
    // 休息任务
    public string BreakTaskTitle { get; set; } = "该休息了！";
    public string BreakTaskContent { get; set; } = "{0} - {1}秒";
    public string BreakTaskButtonStart { get; set; } = "开始休息";
    public string BreakTaskButtonIgnore { get; set; } = "忽略";
    
    // 默认实例
    public static NotificationConfig Default { get; } = new NotificationConfig();
}
