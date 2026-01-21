namespace EyeGuard.Infrastructure.Services;

using System;
using System.Collections.Generic;
using EyeGuard.Core.Enums;
using EyeGuard.Core.Entities;

/// <summary>
/// 休息任务服务 - 管理休息任务的生成、结算和统计
/// </summary>
public class BreakTaskService
{
    private readonly FatigueEngine _fatigueEngine;
    
    // 冷却计时器：忽略任务后的冷却时间
    private DateTime? _cooldownUntil = null;
    
    /// <summary>
    /// 当前待处理的任务
    /// </summary>
    public BreakTaskRecord? CurrentTask { get; private set; }
    
    /// <summary>
    /// 久坐保护阈值（秒），连续工作超过此时长触发 MobilityTask
    /// </summary>
    public int MobilityTaskThresholdSeconds { get; set; } = 20 * 60; // 测试用 1 分钟
    
    /// <summary>
    /// 忽略任务后的冷却时间（秒）
    /// </summary>
    public int CooldownSeconds { get; set; } = 5 * 60; // 5 分钟
    
    /// <summary>
    /// 重置连续工作计时器的回调（由 ViewModel 设置）
    /// </summary>
    public Action? ResetSessionTimer { get; set; }
    
    /// <summary>
    /// 任务生成事件
    /// </summary>
    public event EventHandler<BreakTaskRecord>? TaskGenerated;
    
    /// <summary>
    /// 任务完成事件
    /// </summary>
    public event EventHandler<BreakTaskRecord>? TaskCompleted;
    
    public BreakTaskService(FatigueEngine fatigueEngine)
    {
        _fatigueEngine = fatigueEngine;
    }
    
    /// <summary>
    /// 生成休息任务
    /// </summary>
    public BreakTaskRecord GenerateTask(BreakTaskType type, string reason)
    {
        var task = new BreakTaskRecord
        {
            CreatedAt = DateTime.Now,
            TaskType = type,
            DurationSeconds = GetDefaultDuration(type),
            TriggerReason = reason,
            FatigueAtTrigger = _fatigueEngine.FatigueValue,
            Result = BreakTaskResult.Pending
        };
        
        CurrentTask = task;
        TaskGenerated?.Invoke(this, task);
        
        return task;
    }
    
    /// <summary>
    /// 结算任务
    /// </summary>
    public double SettleTask(BreakTaskRecord task, BreakTaskResult result)
    {
        task.CompletedAt = DateTime.Now;
        task.Result = result;
        
        double recoveryCredit = 0;
        
        if (result == BreakTaskResult.Completed)
        {
            // 根据任务类型和当前疲劳值计算恢复加成
            recoveryCredit = CalculateRecoveryCredit(task);
            task.RecoveryCredit = recoveryCredit;
            
            // 应用恢复加成到疲劳引擎
            _fatigueEngine.ApplyRecoveryCredit(recoveryCredit);
            
            // 重置连续工作计时器
            ResetSessionTimer?.Invoke();
        }
        else if (result == BreakTaskResult.Skipped || result == BreakTaskResult.Snoozed)
        {
            // 设置冷却期，防止立即重新触发
            _cooldownUntil = DateTime.Now.AddSeconds(CooldownSeconds);
            
            // 也重置计时器（视为新的工作周期开始）
            ResetSessionTimer?.Invoke();
        }
        
        if (CurrentTask == task)
        {
            CurrentTask = null;
        }
        
        TaskCompleted?.Invoke(this, task);
        
        return recoveryCredit;
    }
    
    /// <summary>
    /// 检查是否应该触发久坐保护任务
    /// </summary>
    public BreakTaskRecord? CheckMobilityTaskTrigger(int continuousActiveSeconds)
    {
        // 已有待处理任务时不触发新任务
        if (CurrentTask != null)
        {
            return null;
        }
        
        // 冷却期内不触发
        if (_cooldownUntil.HasValue && DateTime.Now < _cooldownUntil.Value)
        {
            return null;
        }
        
        if (continuousActiveSeconds >= MobilityTaskThresholdSeconds)
        {
            return GenerateTask(
                BreakTaskType.Mobility, 
                $"连续工作超过 {MobilityTaskThresholdSeconds / 60} 分钟"
            );
        }
        
        return null;
    }
    
    /// <summary>
    /// 基于疲劳状态检查是否应该触发休息任务
    /// </summary>
    public BreakTaskRecord? CheckFatigueBasedTaskTrigger()
    {
        if (CurrentTask != null)
        {
            return null;
        }
        
        var fatigueValue = _fatigueEngine.FatigueValue;
        var state = _fatigueEngine.CurrentFatigueState;
        
        // 根据疲劳状态触发不同类型的任务
        return state switch
        {
            FatigueState.Grind => GenerateTask(
                BreakTaskType.Mobility, 
                "疲劳值过高，建议立即休息"
            ),
            FatigueState.Overloaded when fatigueValue > 75 => GenerateTask(
                BreakTaskType.Stretch, 
                "疲劳值较高，建议放松肩颈"
            ),
            _ => null
        };
    }
    
    /// <summary>
    /// 获取任务类型的默认时长
    /// </summary>
    public static int GetDefaultDuration(BreakTaskType type)
    {
        return type switch
        {
            BreakTaskType.Eye => 20,     // 20 秒
            BreakTaskType.Breath => 30,  // 30 秒
            BreakTaskType.Mobility => 60, // 60 秒
            BreakTaskType.Stretch => 30,  // 30 秒
            _ => 30
        };
    }
    
    /// <summary>
    /// 获取任务类型的显示名称
    /// </summary>
    public static string GetTaskTypeName(BreakTaskType type)
    {
        return type switch
        {
            BreakTaskType.Eye => "👁️ 护眼放松",
            BreakTaskType.Breath => "🧘 呼吸放空",
            BreakTaskType.Mobility => "🚶 站立走动",
            BreakTaskType.Stretch => "💪 肩颈拉伸",
            _ => "休息任务"
        };
    }
    
    /// <summary>
    /// 获取任务类型的描述
    /// </summary>
    public static string GetTaskTypeDescription(BreakTaskType type)
    {
        return type switch
        {
            BreakTaskType.Eye => "看向远处 20 秒，缓解眼部疲劳",
            BreakTaskType.Breath => "深呼吸放松，清空思绪",
            BreakTaskType.Mobility => "站起来活动一下，促进血液循环",
            BreakTaskType.Stretch => "转动脖子，活动肩膀",
            _ => "休息一下"
        };
    }
    
    /// <summary>
    /// 计算恢复加成
    /// </summary>
    private double CalculateRecoveryCredit(BreakTaskRecord task)
    {
        // 基础恢复值
        double baseCredit = task.TaskType switch
        {
            BreakTaskType.Eye => 3,
            BreakTaskType.Breath => 4,
            BreakTaskType.Mobility => 8,
            BreakTaskType.Stretch => 5,
            _ => 3
        };
        
        // 疲劳越高，恢复效果越好（边际效用递增）
        double fatigueMultiplier = 1.0 + (task.FatigueAtTrigger / 100.0) * 0.5;
        
        return baseCredit * fatigueMultiplier;
    }
}
