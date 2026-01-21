namespace EyeGuard.Infrastructure.Services;

using System;
using EyeGuard.Core.Enums;

/// <summary>
/// 精力预测服务 - 计算 TimeToThreshold (TTE) 和多场景预测
/// </summary>
public class ForecastService
{
    private readonly FatigueEngine _fatigueEngine;
    
    /// <summary>
    /// 高效区阈值 (默认 85%)
    /// </summary>
    public double ThresholdHighEfficiency { get; set; } = 85;
    
    /// <summary>
    /// 到达阈值的剩余时间
    /// </summary>
    public TimeSpan TimeToThreshold { get; private set; } = TimeSpan.MaxValue;
    
    /// <summary>
    /// 是否正在恢复中
    /// </summary>
    public bool IsRecovering { get; private set; }
    
    /// <summary>
    /// 是否已超过阈值（进入 Grind 模式）
    /// </summary>
    public bool IsOverThreshold { get; private set; }
    
    /// <summary>
    /// 超过阈值的时间（秒）
    /// </summary>
    public int OverThresholdSeconds { get; private set; }
    
    public ForecastService(FatigueEngine fatigueEngine)
    {
        _fatigueEngine = fatigueEngine;
    }
    
    /// <summary>
    /// 更新预测计算
    /// </summary>
    public void Update()
    {
        double fatigue = _fatigueEngine.FatigueValue;
        double slope = _fatigueEngine.FatigueSlope;
        
        // 判断是否已超阈值
        IsOverThreshold = fatigue >= ThresholdHighEfficiency;
        
        if (IsOverThreshold)
        {
            // 已超阈值，累计 Grind 时间
            OverThresholdSeconds++;
            TimeToThreshold = TimeSpan.Zero;
            IsRecovering = slope < 0;
            return;
        }
        else
        {
            OverThresholdSeconds = 0;
        }
        
        // 判断是否在恢复
        IsRecovering = slope <= 0;
        
        if (IsRecovering || slope < 0.01)
        {
            // 恢复中或增长极慢
            TimeToThreshold = TimeSpan.MaxValue;
            return;
        }
        
        // 计算 TTE: (Threshold - Current) / slope
        double remaining = ThresholdHighEfficiency - fatigue;
        double minutesToThreshold = remaining / slope;
        
        // 限制最大显示时间为 4 小时
        if (minutesToThreshold > 240)
        {
            TimeToThreshold = TimeSpan.MaxValue;
        }
        else
        {
            TimeToThreshold = TimeSpan.FromMinutes(minutesToThreshold);
        }
    }
    
    /// <summary>
    /// 获取倒计时显示文本
    /// </summary>
    public string GetCountdownText()
    {
        if (IsOverThreshold)
        {
            int overMinutes = OverThresholdSeconds / 60;
            return $"⚠️ 已超负荷 {overMinutes} 分钟";
        }
        
        if (IsRecovering)
        {
            return "恢复中 ✓";
        }
        
        if (TimeToThreshold == TimeSpan.MaxValue || TimeToThreshold.TotalMinutes > 120)
        {
            return "> 2 小时";
        }
        
        int totalMinutes = (int)TimeToThreshold.TotalMinutes;
        if (totalMinutes >= 60)
        {
            int hours = totalMinutes / 60;
            int mins = totalMinutes % 60;
            return $"{hours}小时{mins}分";
        }
        
        return $"{totalMinutes} 分钟";
    }
    
    /// <summary>
    /// 获取倒计时副标题
    /// </summary>
    public string GetCountdownSubtitle()
    {
        if (IsOverThreshold)
        {
            return "建议立即休息";
        }
        
        if (IsRecovering)
        {
            return "疲劳正在下降";
        }
        
        return "后进入低效区";
    }
    
    /// <summary>
    /// 估算切换到低负荷模式后的 TTE
    /// </summary>
    /// <param name="lowLoadWeight">低负荷权重 (默认 0.3)</param>
    public TimeSpan EstimateTTELowLoad(double lowLoadWeight = 0.3)
    {
        double fatigue = _fatigueEngine.FatigueValue;
        double slope = _fatigueEngine.FatigueSlope;
        
        if (fatigue >= ThresholdHighEfficiency || slope <= 0)
        {
            return TimeSpan.MaxValue;
        }
        
        // 按比例调整 slope
        double adjustedSlope = slope * lowLoadWeight;
        
        if (adjustedSlope < 0.01)
        {
            return TimeSpan.MaxValue;
        }
        
        double remaining = ThresholdHighEfficiency - fatigue;
        double minutesToThreshold = remaining / adjustedSlope;
        
        if (minutesToThreshold > 240)
        {
            return TimeSpan.MaxValue;
        }
        
        return TimeSpan.FromMinutes(minutesToThreshold);
    }
    
    /// <summary>
    /// 获取延长方案建议文本
    /// </summary>
    public string? GetExtensionSuggestionText()
    {
        if (IsRecovering || IsOverThreshold)
        {
            return null;
        }
        
        var currentTTE = TimeToThreshold;
        var lowLoadTTE = EstimateTTELowLoad();
        
        if (currentTTE == TimeSpan.MaxValue || lowLoadTTE == TimeSpan.MaxValue)
        {
            return null;
        }
        
        // 只有当低负荷模式显著延长时间时才显示建议
        if (lowLoadTTE.TotalMinutes > currentTTE.TotalMinutes * 1.5)
        {
            int extendedMinutes = (int)lowLoadTTE.TotalMinutes;
            if (extendedMinutes >= 60)
            {
                int hours = extendedMinutes / 60;
                int mins = extendedMinutes % 60;
                return $"💡 切换到媒体模式可延长至 {hours}小时{mins}分";
            }
            return $"💡 切换到媒体模式可延长至 {extendedMinutes} 分钟";
        }
        
        return null;
    }
}
