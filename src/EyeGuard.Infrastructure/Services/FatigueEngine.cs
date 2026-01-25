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

    // ===== Limit 3.0: 主观校准系统 (Empathy Engine) =====
    private DateTime _lastBiasDecayTime = DateTime.Now;
    private const double BiasDecayRatePerHour = 0.05;  // 每小时衰减 5%
    private const double MaxSensitivityBias = 0.5;     // 最大敏感度偏差 ±50%
    private const double TiredCalibrationBoost = 0.20; // "我很累" +20%
    private const double FreshCalibrationDrop = 0.15;  // "我没那么累" -15%

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

    // ===== Limit 3.0: 主观校准属性 =====

    /// <summary>
    /// 敏感度偏差 (-0.5 ~ +0.5)，正值表示用户感觉更累，负值表示用户感觉更轻松。
    /// 影响疲劳增长速率：effectiveRate = baseRate * (1 + SensitivityBias)
    /// </summary>
    public double SensitivityBias { get; private set; } = 0;

    /// <summary>
    /// Care Mode 关怀模式是否激活。
    /// 激活时 UI 显示柔和光晕，干预阈值降低。
    /// </summary>
    public bool IsCareMode { get; private set; } = false;

    /// <summary>
    /// Care Mode 激活时的颜色（柔和橙色）
    /// </summary>
    public string CareModeColor => "#FF8C00";

    /// <summary>
    /// 最后一次疲劳变化的解释
    /// </summary>
    public FatigueExplanation? LastExplanation { get; private set; }

    /// <summary>
    /// 基础每分钟疲劳增加率。
    /// Beta 2 (A1): 修正为 1.2，对应 90 分钟次昨夜节律周期
    /// </summary>
    public double BaseFatigueIncreasePerMinute { get; set; } = 1.2;

    /// <summary>
    /// 基础每分钟疲劳恢复率。
    /// Beta 2 (A1): 匹配增长速度
    /// </summary>
    public double BaseFatigueDecreasePerMinute { get; set; } = 0.8;

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
        // 先执行敏感度衰减
        ApplyBiasDecay();

        // Limit 3.0 公式: 基础速度 * (1 + 当前疲劳/100) * LoadWeight * (1 + SensitivityBias)
        double multiplier = 1.0 + FatigueValue / 100.0;
        double sensitivityMultiplier = 1.0 + SensitivityBias;

        // Beta 2 (A4): 生物钟权重
        double circadianMultiplier = GetCircadianMultiplier();

        return (BaseFatigueIncreasePerMinute * multiplier * LoadWeight * sensitivityMultiplier * circadianMultiplier) / 60.0;
    }

    /// <summary>
    /// Beta 2 (A4): 计算生物钟权重（昨夜节律）
    /// </summary>
    private double GetCircadianMultiplier()
    {
        int hour = DateTime.Now.Hour;

        return hour switch
        {
            >= 9 and <= 11 => 0.9,   // 黄金时间（上午）
            >= 13 and <= 15 => 1.3,  // 午后低谷
            >= 22 or <= 6 => 1.5,    // 深夜加班
            _ => 1.0                  // 其他时段
        };
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

        // Limit 3.0: 重置主观校准状态
        SensitivityBias = 0;
        IsCareMode = false;
        _lastBiasDecayTime = DateTime.Now;
    }

    // ===== Limit 3.0: 主观校准方法 =====

    /// <summary>
    /// 用户反馈"我很累" - 触发 Care Mode，敏感度 +20%
    /// </summary>
    public void CalibrateAsTired()
    {
        SensitivityBias = Math.Min(MaxSensitivityBias, SensitivityBias + TiredCalibrationBoost);
        IsCareMode = true;
        _lastBiasDecayTime = DateTime.Now;

        LastExplanation = new FatigueExplanation
        {
            BaseCurve = 0,
            LoadWeight = 0,
            RecoveryCredit = 0,
            FatigueDelta = 0,
            ReasonCode = "CALIBRATE_TIRED",
            ReasonText = $"用户反馈疲劳，敏感度提升至 {SensitivityBias:P0}"
        };
    }

    /// <summary>
    /// 用户反馈"我没那么累" - 敏感度 -15%
    /// </summary>
    public void CalibrateAsFresh()
    {
        SensitivityBias = Math.Max(-MaxSensitivityBias, SensitivityBias - FreshCalibrationDrop);

        // 如果敏感度降到0以下，退出 Care Mode
        if (SensitivityBias <= 0)
        {
            IsCareMode = false;
        }
        _lastBiasDecayTime = DateTime.Now;

        LastExplanation = new FatigueExplanation
        {
            BaseCurve = 0,
            LoadWeight = 0,
            RecoveryCredit = 0,
            FatigueDelta = 0,
            ReasonCode = "CALIBRATE_FRESH",
            ReasonText = $"用户反馈精力充沛，敏感度调整至 {SensitivityBias:P0}"
        };
    }

    /// <summary>
    /// 用户反馈"我刚休息过" - 直接扣减疲劳值
    /// </summary>
    public void CalibrateAfterRest(double reduction = 15)
    {
        double delta = -reduction;
        FatigueValue = Math.Max(0, FatigueValue + delta);

        LastExplanation = new FatigueExplanation
        {
            BaseCurve = 0,
            LoadWeight = 0,
            RecoveryCredit = reduction,
            FatigueDelta = delta,
            ReasonCode = "CALIBRATE_RESTED",
            ReasonText = "用户反馈刚休息过"
        };

        UpdateSlopeAndState(delta);
    }

    /// <summary>
    /// 应用敏感度偏差的时间衰减（每小时 5%）
    /// </summary>
    private void ApplyBiasDecay()
    {
        if (SensitivityBias == 0) return;

        var now = DateTime.Now;
        var elapsed = now - _lastBiasDecayTime;

        // 每小时衰减一次
        if (elapsed.TotalHours >= 1)
        {
            double hoursElapsed = elapsed.TotalHours;
            double decayAmount = BiasDecayRatePerHour * hoursElapsed;

            // 向 0 衰减
            if (SensitivityBias > 0)
            {
                SensitivityBias = Math.Max(0, SensitivityBias - decayAmount);
            }
            else
            {
                SensitivityBias = Math.Min(0, SensitivityBias + decayAmount);
            }

            // 如果敏感度衰减到 0，退出 Care Mode
            if (SensitivityBias <= 0)
            {
                IsCareMode = false;
            }

            _lastBiasDecayTime = now;
        }
    }

    /// <summary>
    /// 获取当前敏感度状态描述
    /// </summary>
    public string GetSensitivityDescription()
    {
        if (IsCareMode)
        {
            return $"关怀模式 (敏感度 +{SensitivityBias:P0})";
        }
        else if (SensitivityBias > 0)
        {
            return $"敏感度偏高 (+{SensitivityBias:P0})";
        }
        else if (SensitivityBias < 0)
        {
            return $"敏感度偏低 ({SensitivityBias:P0})";
        }
        return "标准模式";
    }

    /// <summary>
    /// 设置疲劳值（别名方法，为保持向后兼容）。
    /// </summary>
    public void SetFatigue(double value) => SetFatigueValue(value);

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
