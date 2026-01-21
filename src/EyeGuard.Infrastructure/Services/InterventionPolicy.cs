namespace EyeGuard.Infrastructure.Services;

using System;
using System.Diagnostics;
using EyeGuard.Core.Enums;
using EyeGuard.Core.Models;

/// <summary>
/// 干预策略服务 - 根据疲劳状态决定干预级别和消息
/// </summary>
public class InterventionPolicy
{
    // 冷却时间（避免频繁干预）
    private DateTime? _lastNudgeTime;
    private DateTime? _lastSuggestionTime;
    private DateTime? _lastInterventionTime;
    
    // 冷却秒数
    private const int NudgeCooldownSeconds = 60;       // Nudge 1分钟冷却
    private const int SuggestionCooldownSeconds = 180; // Suggestion 3分钟冷却
    private const int InterventionCooldownSeconds = 300; // Intervention 5分钟冷却
    
    // 疲劳阈值
    private const double NudgeThreshold = 40;
    private const double SuggestionThreshold = 60;
    private const double InterventionThreshold = 80;
    
    /// <summary>
    /// 根据疲劳度计算干预级别
    /// </summary>
    public InterventionLevel GetLevel(double fatigueValue)
    {
        if (fatigueValue >= InterventionThreshold)
            return InterventionLevel.Intervention;
        if (fatigueValue >= SuggestionThreshold)
            return InterventionLevel.Suggestion;
        if (fatigueValue >= NudgeThreshold)
            return InterventionLevel.Nudge;
        
        return InterventionLevel.None;
    }
    
    /// <summary>
    /// 评估当前状态，返回干预信息（带冷却检查）
    /// </summary>
    public InterventionState Evaluate(double fatigueValue, ContextState context)
    {
        var level = GetLevel(fatigueValue);
        
        // 检查冷却
        if (!CanTrigger(level))
        {
            return new InterventionState
            {
                Level = InterventionLevel.None,
                Message = ""
            };
        }
        
        // 记录触发时间
        RecordTrigger(level);
        
        // 生成消息
        var (message, actionText) = GenerateMessage(level, fatigueValue, context);
        
        Debug.WriteLine($"[Intervention] Level={level}, Fatigue={fatigueValue:F1}%, Context={context}");
        
        return new InterventionState
        {
            Level = level,
            Message = message,
            ActionText = actionText,
            TriggeredAt = DateTime.Now
        };
    }
    
    /// <summary>
    /// 检查是否可以触发（冷却期已过）
    /// </summary>
    private bool CanTrigger(InterventionLevel level)
    {
        var now = DateTime.Now;
        
        return level switch
        {
            InterventionLevel.Nudge => 
                !_lastNudgeTime.HasValue || 
                (now - _lastNudgeTime.Value).TotalSeconds >= NudgeCooldownSeconds,
                
            InterventionLevel.Suggestion => 
                !_lastSuggestionTime.HasValue || 
                (now - _lastSuggestionTime.Value).TotalSeconds >= SuggestionCooldownSeconds,
                
            InterventionLevel.Intervention => 
                !_lastInterventionTime.HasValue || 
                (now - _lastInterventionTime.Value).TotalSeconds >= InterventionCooldownSeconds,
                
            _ => true
        };
    }
    
    /// <summary>
    /// 记录触发时间
    /// </summary>
    private void RecordTrigger(InterventionLevel level)
    {
        var now = DateTime.Now;
        
        switch (level)
        {
            case InterventionLevel.Nudge:
                _lastNudgeTime = now;
                break;
            case InterventionLevel.Suggestion:
                _lastSuggestionTime = now;
                break;
            case InterventionLevel.Intervention:
                _lastInterventionTime = now;
                break;
        }
    }
    
    /// <summary>
    /// 生成干预消息
    /// </summary>
    private (string Message, string? ActionText) GenerateMessage(
        InterventionLevel level, 
        double fatigueValue, 
        ContextState context)
    {
        return level switch
        {
            InterventionLevel.Nudge => context switch
            {
                ContextState.Work => ("💡 已连续工作一段时间，注意休息", null),
                ContextState.Entertainment => ("😊 休息得不错，精力正在恢复", null),
                _ => ("💡 注意用眼健康", null)
            },
            
            InterventionLevel.Suggestion => context switch
            {
                ContextState.Work => ($"🔔 疲劳度 {fatigueValue:F0}%，建议休息 5-10 分钟", "休息一下"),
                ContextState.Entertainment => ($"📺 虽然在休息，但眼睛也需要放松", "闭眼休息"),
                _ => ($"🔔 疲劳度较高 ({fatigueValue:F0}%)，建议休息", "休息一下")
            },
            
            InterventionLevel.Intervention => (
                $"⚠️ 疲劳度过高 ({fatigueValue:F0}%)！强烈建议立即休息", 
                "开始休息"
            ),
            
            _ => ("", null)
        };
    }
    
    /// <summary>
    /// 重置冷却（用于测试或休息后）
    /// </summary>
    public void ResetCooldowns()
    {
        _lastNudgeTime = null;
        _lastSuggestionTime = null;
        _lastInterventionTime = null;
    }
}
