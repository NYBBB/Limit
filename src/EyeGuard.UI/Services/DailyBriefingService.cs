using System;
using System.Diagnostics;
using EyeGuard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EyeGuard.UI.Services;

/// <summary>
/// 昨日评分等级
/// </summary>
public enum DayGrade
{
    S,  // 优秀：疲劳 < 50%，休息次数 >= 3
    A,  // 良好：疲劳 < 70%，休息次数 >= 2
    B,  // 一般：疲劳 < 85%
    C   // 需改进：疲劳 >= 85% 或无数据
}

/// <summary>
/// 昨日简报数据
/// </summary>
public class DayBriefing
{
    public DateTime Date { get; set; }
    public DayGrade Grade { get; set; }
    public double PeakFatigue { get; set; }
    public int TotalBreaks { get; set; }
    public TimeSpan TotalWorkTime { get; set; }
    public string FocusApp { get; set; } = "";
    public string GradeEmoji => Grade switch
    {
        DayGrade.S => "🌟",
        DayGrade.A => "✨",
        DayGrade.B => "😊",
        DayGrade.C => "💪",
        _ => "📊"
    };
    public string GradeMessage => Grade switch
    {
        DayGrade.S => "表现出色！保持这个节奏！",
        DayGrade.A => "不错的一天！继续保持！",
        DayGrade.B => "还可以，记得多休息！",
        DayGrade.C => "昨天有点累，今天轻松点！",
        _ => ""
    };
}

/// <summary>
/// 晨报服务 - Phase 5.3
/// 检测新一天首次活跃，显示昨日评分
/// </summary>
public class DailyBriefingService
{
    private readonly DatabaseService _databaseService;
    private readonly TrayIconService _trayIconService;
    
    private DateTime? _lastBriefingDate;
    private bool _hasBriefingShown = false;
    
    // 事件：需要显示晨报弹窗
    public event EventHandler<DayBriefing>? BriefingRequested;
    
    public DailyBriefingService(TrayIconService trayIconService)
    {
        _trayIconService = trayIconService;
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
    }

    /// <summary>
    /// 检查是否需要显示晨报（新一天首次活跃）
    /// </summary>
    public async Task CheckAndShowBriefingAsync()
    {
        var today = DateTime.Today;
        
        // 已经今天显示过了
        if (_lastBriefingDate == today && _hasBriefingShown)
            return;
        
        // 检查是否是新一天的首次活跃（早上 6 点 - 中午 12 点）
        var now = DateTime.Now;
        if (now.Hour < 6 || now.Hour >= 12)
        {
            // 不在晨报时段
            return;
        }
        
        try
        {
            var briefing = await GenerateBriefingAsync(today.AddDays(-1));
            
            if (briefing != null)
            {
                // 显示 Toast 通知
                _trayIconService.ShowNotification(
                    $"{briefing.GradeEmoji} 昨日简报",
                    $"评分: {briefing.Grade} - {briefing.GradeMessage}");
                
                // 触发弹窗事件
                BriefingRequested?.Invoke(this, briefing);
                
                _lastBriefingDate = today;
                _hasBriefingShown = true;
                
                Debug.WriteLine($"[Briefing] Shown for {today:yyyy-MM-dd}: Grade={briefing.Grade}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Briefing] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成昨日简报
    /// </summary>
    private async Task<DayBriefing?> GenerateBriefingAsync(DateTime date)
    {
        // 获取昨日使用记录
        var records = await _databaseService.GetHourlyUsageAsync(date);
        
        if (records == null || records.Count == 0)
        {
            return new DayBriefing
            {
                Date = date,
                Grade = DayGrade.C,
                PeakFatigue = 0,
                TotalBreaks = 0,
                TotalWorkTime = TimeSpan.Zero
            };
        }
        
        // 计算统计数据
        var totalMinutes = records.Sum(r => r.DurationSeconds) / 60;
        var topApp = records.GroupBy(r => r.AppName)
            .OrderByDescending(g => g.Sum(r => r.DurationSeconds))
            .FirstOrDefault()?.Key ?? "";
        
        // 简化评分逻辑（实际应该基于疲劳峰值和休息次数）
        var briefing = new DayBriefing
        {
            Date = date,
            TotalWorkTime = TimeSpan.FromMinutes(totalMinutes),
            FocusApp = IconMapper.GetFriendlyName(topApp),
            TotalBreaks = 2,  // TODO: 从数据库获取实际休息次数
            PeakFatigue = 65  // TODO: 从数据库获取实际疲劳峰值
        };
        
        // 根据工作时长评分
        briefing.Grade = totalMinutes switch
        {
            < 180 => DayGrade.S, // <3h 轻松的一天
            < 360 => DayGrade.A, // 3-6h 正常
            < 480 => DayGrade.B, // 6-8h 有点累
            _ => DayGrade.C      // >8h 需要休息
        };
        
        return briefing;
    }
}
