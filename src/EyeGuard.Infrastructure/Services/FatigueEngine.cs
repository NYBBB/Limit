namespace EyeGuard.Infrastructure.Services;

using System;
using System.Collections.Generic;
using EyeGuard.Core.Enums;
using EyeGuard.Core.Models;

/// <summary>
/// 疲劳值算法引擎。
/// 实现非线性疲劳曲线：疲劳越高增长越快，恢复时疲劳高恢复快、疲劳低恢复慢。
/// Limit 2.0 升级：支持可解释因子、Slope 计算和 FatigueState 离散态。
/// </summary>
public class FatigueEngine
{
    // ===== EMA Slope 计算参数 =====
    private const int SlopeHistorySize = 60;  // 记录最近 60 次变化 (约 1 分钟)
    private readonly Queue<double> _deltaHistory = new();
    private double _lastFatigueValue = 0;
    
    /// <summary>
    /// 当前疲劳值 (0-100)。
    /// </summary>
    public double FatigueValue { get; private set; } = 0;

    /// <summary>
    /// 疲劳变化斜率 (%/分钟)，正值表示增长，负值表示恢复。
    /// 使用 EMA 平滑计算。
    /// </summary>
    public double FatigueSlope { get; private set; } = 0;
    
    /// <summary>
    /// 当前疲劳状态（离散态）
    /// </summary>
    public FatigueState CurrentFatigueState { get; private set; } = FatigueState.Fresh;
    
    /// <summary>
    /// 当前上下文负荷权重 (1.0=工作, 0.3=娱乐)
    /// </summary>
    public double LoadWeight { get; set; } = 1.0;
    
    /// <summary>
    /// 最后一次疲劳变化的解释
    /// </summary>
    public FatigueExplanation? LastExplanation { get; private set; }

    /// <summary>
    /// 基础每分钟疲劳增加率。
    /// </summary>
    public double BaseFatigueIncreasePerMinute { get; set; } = 1.0;

    /// <summary>
    /// 基础每分钟疲劳恢复率。
    /// </summary>
    public double BaseFatigueDecreasePerMinute { get; set; } = 2.0;
    
    /// <summary>
    /// 调试用：直接设置疲劳值
    /// </summary>
    public void SetFatigueValue(double value)
    {
        FatigueValue = Math.Clamp(value, 0, 100);
        UpdateSlopeAndState(0); // 不影响斜率计算
    }

    /// <summary>
    /// 媒体模式下疲劳增加倍率 (看视频时也会眼睛疲劳，但比工作少)。
    /// </summary>
    public double MediaModeFatigueMultiplier { get; set; } = 0.3;

    /// <summary>
    /// 计算当前疲劳增加速度（非线性：疲劳越高，增长越快）。
    /// </summary>
    /// <returns>每秒疲劳增加量</returns>
    public double GetFatigueIncreaseRate()
    {
        // 公式: 基础速度 × (1 + 当前疲劳/100) × LoadWeight
        double multiplier = 1.0 + FatigueValue / 100.0;
        return (BaseFatigueIncreasePerMinute * multiplier * LoadWeight) / 60.0;
    }

    /// <summary>
    /// 计算当前疲劳恢复速度（非线性：疲劳越高，恢复越快）。
    /// </summary>
    /// <returns>每秒疲劳恢复量</returns>
    public double GetFatigueDecreaseRate()
    {
        // 公式: 基础速度 × (当前疲劳/50)
        double multiplier = Math.Max(0.2, FatigueValue / 50.0);
        return (BaseFatigueDecreasePerMinute * multiplier) / 60.0;
    }

    /// <summary>
    /// 增加疲劳值（工作状态调用）。
    /// </summary>
    /// <param name="seconds">经过的秒数</param>
    /// <param name="isMediaMode">是否为媒体模式</param>
    /// <param name="reasonCode">原因代码（可选）</param>
    public void IncreaseFatigue(double seconds = 1, bool isMediaMode = false, string? reasonCode = null)
    {
        double baseCurve = GetFatigueIncreaseRate();
        double effectiveRate = baseCurve;
        double actualLoadWeight = LoadWeight;
        
        if (isMediaMode)
        {
            effectiveRate *= MediaModeFatigueMultiplier;
            actualLoadWeight = MediaModeFatigueMultiplier;
        }
        
        double delta = effectiveRate * seconds;
        double oldValue = FatigueValue;
        FatigueValue = Math.Min(100, FatigueValue + delta);
        
        // 记录解释
        LastExplanation = new FatigueExplanation
        {
            BaseCurve = baseCurve * 60, // 转换为每分钟
            LoadWeight = actualLoadWeight,
            RecoveryCredit = 0,
            FatigueDelta = delta,
            ReasonCode = reasonCode ?? (isMediaMode ? "MEDIA_MODE" : "ACTIVE_WORK"),
            ReasonText = isMediaMode ? "媒体模式（低负荷）" : "正在工作（高负荷）"
        };
        
        UpdateSlopeAndState(delta);
    }

    /// <summary>
    /// 减少疲劳值（休息状态调用）。
    /// </summary>
    /// <param name="seconds">经过的秒数</param>
    /// <param name="recoveryMultiplier">恢复加成倍率（如完成任务加成）</param>
    /// <param name="reasonCode">原因代码（可选）</param>
    public void DecreaseFatigue(double seconds = 1, double recoveryMultiplier = 1.0, string? reasonCode = null)
    {
        double baseCurve = GetFatigueDecreaseRate();
        double effectiveRate = baseCurve * recoveryMultiplier;
        double delta = -effectiveRate * seconds;
        double oldValue = FatigueValue;
        FatigueValue = Math.Max(0, FatigueValue + delta);
        
        // 记录解释
        LastExplanation = new FatigueExplanation
        {
            BaseCurve = baseCurve * 60,
            LoadWeight = 0,
            RecoveryCredit = recoveryMultiplier,
            FatigueDelta = delta,
            ReasonCode = reasonCode ?? "RECOVERY",
            ReasonText = recoveryMultiplier > 1 ? "加速恢复中" : "正在恢复"
        };
        
        UpdateSlopeAndState(delta);
    }
    
    /// <summary>
    /// 应用任务完成的恢复加成
    /// </summary>
    /// <param name="recoveryCredit">恢复值</param>
    public void ApplyRecoveryCredit(double recoveryCredit)
    {
        double delta = -recoveryCredit;
        FatigueValue = Math.Max(0, FatigueValue + delta);
        
        LastExplanation = new FatigueExplanation
        {
            BaseCurve = 0,
            LoadWeight = 0,
            RecoveryCredit = recoveryCredit,
            FatigueDelta = delta,
            ReasonCode = "TASK_COMPLETED",
            ReasonText = "完成休息任务"
        };
        
        UpdateSlopeAndState(delta);
    }

    /// <summary>
    /// 更新 Slope 和 FatigueState
    /// </summary>
    private void UpdateSlopeAndState(double delta)
    {
        // 记录 delta 到历史队列
        _deltaHistory.Enqueue(delta);
        if (_deltaHistory.Count > SlopeHistorySize)
        {
            _deltaHistory.Dequeue();
        }
        
        // 计算 EMA slope (%/分钟)
        if (_deltaHistory.Count > 0)
        {
            double sum = 0;
            foreach (var d in _deltaHistory)
            {
                sum += d;
            }
            // 平均每秒变化 * 60 = 每分钟变化
            FatigueSlope = (sum / _deltaHistory.Count) * 60;
        }
        
        // 更新离散状态
        CurrentFatigueState = FatigueValue switch
        {
            < 30 => FatigueState.Fresh,
            < 60 => FatigueState.Strained,
            < 85 => FatigueState.Overloaded,
            _ => FatigueState.Grind
        };
    }

    /// <summary>
    /// 重置疲劳值。
    /// </summary>
    public void Reset()
    {
        FatigueValue = 0;
        FatigueSlope = 0;
        CurrentFatigueState = FatigueState.Fresh;
        _deltaHistory.Clear();
        LastExplanation = null;
    }

    /// <summary>
    /// 设置疲劳值。
    /// </summary>
    public void SetFatigue(double value)
    {
        FatigueValue = Math.Clamp(value, 0, 100);
        UpdateSlopeAndState(0);
    }

    /// <summary>
    /// 获取当前疲劳等级描述。
    /// </summary>
    public string GetFatigueLevel()
    {
        return CurrentFatigueState switch
        {
            FatigueState.Fresh => "精力充沛",
            FatigueState.Strained => "略感疲劳",
            FatigueState.Overloaded => "比较疲劳",
            FatigueState.Grind => "严重透支",
            _ => "状态未知"
        };
    }
    
    /// <summary>
    /// 获取 FatigueState 对应的颜色代码
    /// </summary>
    public string GetFatigueStateColor()
    {
        return CurrentFatigueState switch
        {
            FatigueState.Fresh => "#00C853",      // 绿色
            FatigueState.Strained => "#FFD600",   // 黄色
            FatigueState.Overloaded => "#FF6D00", // 橙色
            FatigueState.Grind => "#FF1744",      // 红色
            _ => "#808080"
        };
    }

    /// <summary>
    /// 预估恢复到指定疲劳值需要的时间（分钟）。
    /// </summary>
    public double EstimateRecoveryTime(double targetFatigue = 20)
    {
        if (FatigueValue <= targetFatigue) return 0;
        
        double avgFatigue = (FatigueValue + targetFatigue) / 2;
        double avgRate = (BaseFatigueDecreasePerMinute * avgFatigue / 50.0);
        
        return (FatigueValue - targetFatigue) / avgRate;
    }

    /// <summary>
    /// 获取推荐休息时长（秒）。
    /// </summary>
    public int GetRecommendedBreakSeconds()
    {
        return FatigueValue switch
        {
            < 20 => 0,
            < 40 => 20,
            < 50 => 30,
            < 60 => 60,
            < 70 => 120,
            < 80 => 180,
            < 90 => 240,
            _ => 300
        };
    }

    /// <summary>
    /// 获取推荐休息时长的友好文本。
    /// </summary>
    public string GetRecommendedBreakText()
    {
        int seconds = GetRecommendedBreakSeconds();
        if (seconds == 0) return "无需休息 ✓";
        if (seconds < 60) return $"建议休息 {seconds} 秒";
        return $"建议休息 {seconds / 60} 分钟";
    }
}
