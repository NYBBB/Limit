namespace EyeGuard.Infrastructure.Services;

using System;

/// <summary>
/// 疲劳值算法引擎。
/// 实现非线性疲劳曲线：疲劳越高增长越快，恢复时疲劳高恢复快、疲劳低恢复慢。
/// </summary>
public class FatigueEngine
{
    /// <summary>
    /// 当前疲劳值 (0-100)。
    /// </summary>
    public double FatigueValue { get; private set; } = 0;

    /// <summary>
    /// 基础每分钟疲劳增加率。
    /// </summary>
    public double BaseFatigueIncreasePerMinute { get; set; } = 1.0;

    /// <summary>
    /// 基础每分钟疲劳恢复率。
    /// </summary>
    public double BaseFatigueDecreasePerMinute { get; set; } = 2.0;

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
        // 公式: 基础速度 × (1 + 当前疲劳/100)
        // 疲劳0%: 每分钟+1%, 疲劳50%: 每分钟+1.5%, 疲劳80%: 每分钟+1.8%
        double multiplier = 1.0 + FatigueValue / 100.0;
        return (BaseFatigueIncreasePerMinute * multiplier) / 60.0; // 转换为每秒
    }

    /// <summary>
    /// 计算当前疲劳恢复速度（非线性：疲劳越高，恢复越快）。
    /// </summary>
    /// <returns>每秒疲劳恢复量</returns>
    public double GetFatigueDecreaseRate()
    {
        // 公式: 基础速度 × (当前疲劳/50)
        // 疲劳100%: 每分钟-4%, 疲劳50%: 每分钟-2%, 疲劳20%: 每分钟-0.8%
        double multiplier = Math.Max(0.2, FatigueValue / 50.0); // 最低0.2倍
        return (BaseFatigueDecreasePerMinute * multiplier) / 60.0; // 转换为每秒
    }

    /// <summary>
    /// 增加疲劳值（工作状态调用）。
    /// </summary>
    /// <param name="seconds">经过的秒数</param>
    /// <param name="isMediaMode">是否为媒体模式</param>
    public void IncreaseFatigue(double seconds = 1, bool isMediaMode = false)
    {
        double rate = GetFatigueIncreaseRate();
        if (isMediaMode)
        {
            rate *= MediaModeFatigueMultiplier;
        }
        FatigueValue = Math.Min(100, FatigueValue + rate * seconds);
    }

    /// <summary>
    /// 减少疲劳值（休息状态调用）。
    /// </summary>
    /// <param name="seconds">经过的秒数</param>
    public void DecreaseFatigue(double seconds = 1)
    {
        double rate = GetFatigueDecreaseRate();
        FatigueValue = Math.Max(0, FatigueValue - rate * seconds);
    }

    /// <summary>
    /// 重置疲劳值。
    /// </summary>
    public void Reset()
    {
        FatigueValue = 0;
    }

    /// <summary>
    /// 设置疲劳值。
    /// </summary>
    public void SetFatigue(double value)
    {
        FatigueValue = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// 获取当前疲劳等级描述。
    /// </summary>
    public string GetFatigueLevel()
    {
        return FatigueValue switch
        {
            < 20 => "精力充沛",
            < 40 => "状态良好",
            < 60 => "略感疲劳",
            < 80 => "比较疲劳",
            _ => "非常疲劳"
        };
    }

    /// <summary>
    /// 预估恢复到指定疲劳值需要的时间（分钟）。
    /// </summary>
    public double EstimateRecoveryTime(double targetFatigue = 20)
    {
        if (FatigueValue <= targetFatigue) return 0;
        
        // 简化计算：使用平均恢复速度
        double avgFatigue = (FatigueValue + targetFatigue) / 2;
        double avgRate = (BaseFatigueDecreasePerMinute * avgFatigue / 50.0);
        
        return (FatigueValue - targetFatigue) / avgRate;
    }

    /// <summary>
    /// 获取推荐休息时长（秒）。
    /// 根据当前疲劳度动态计算。
    /// </summary>
    public int GetRecommendedBreakSeconds()
    {
        // 疲劳度与推荐休息时间的关系：
        // 0-20%: 不需要休息
        // 20-40%: 20秒短休息
        // 40-60%: 60秒（1分钟）
        // 60-80%: 180秒（3分钟）
        // 80-100%: 300秒（5分钟）
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
