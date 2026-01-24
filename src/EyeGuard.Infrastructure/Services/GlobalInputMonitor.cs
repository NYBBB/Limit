using System;
using System.Runtime.InteropServices;
using EyeGuard.Core.Interfaces;

namespace EyeGuard.Infrastructure.Services;

/// <summary>
/// 全局输入监测器 - Limit 3.0 性能优化版
/// 使用 GetLastInputInfo (被动轮询) 替代全局钩子，解决卡顿问题
/// </summary>
public class GlobalInputMonitor : IInputMonitor, IDisposable
{
    private DateTime _lastCheckTime = DateTime.Now;
    private bool _wasIdle = false;
    
    /// <summary>
    /// 空闲阈值（秒）。超过此时间无输入则认为用户空闲。
    /// </summary>
    public int IdleThresholdSeconds { get; set; } = 60;
    
    /// <summary>
    /// 监测器是否正在运行。
    /// </summary>
    public bool IsRunning { get; private set; }
    
    /// <summary>
    /// 自上次输入以来的空闲时长（秒）
    /// </summary>
    public double IdleSeconds { get; private set; }
    
    /// <summary>
    /// 当前是否处于空闲状态
    /// </summary>
    public bool IsIdle => IdleSeconds > IdleThresholdSeconds;
    
    /// <summary>
    /// 最后一次输入的时间（计算值）
    /// </summary>
    public DateTime LastInputTime => DateTime.Now.AddSeconds(-IdleSeconds);
    
    /// <summary>
    /// 检测到用户输入时触发
    /// </summary>
    public event EventHandler<InputEventArgs>? InputDetected;
    
    /// <summary>
    /// 用户状态从活跃变为空闲时触发
    /// </summary>
    public event EventHandler? IdleStarted;
    
    /// <summary>
    /// 用户状态从空闲恢复为活跃时触发
    /// </summary>
    public event EventHandler? IdleEnded;
    
    public GlobalInputMonitor()
    {
        // 无需初始化钩子
    }
    
    /// <summary>
    /// 启动输入监测
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        System.Diagnostics.Debug.WriteLine("[InputMonitor] Started - Using GetLastInputInfo (passive polling)");
    }
    
    /// <summary>
    /// 停止输入监测
    /// </summary>
    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        System.Diagnostics.Debug.WriteLine("[InputMonitor] Stopped");
    }
    
    /// <summary>
    /// 检查空闲状态（每秒调用一次）
    /// Limit 3.0: 使用 GetLastInputInfo 被动轮询（不卡顿）
    /// </summary>
    public void CheckIdleState()
    {
        if (!IsRunning) return;
        
        try
        {
            var lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            
            if (GetLastInputInfo(ref lastInputInfo))
            {
                // 获取系统启动后的毫秒数
                uint currentTickCount = GetTickCount();
                
                // 计算空闲时间（毫秒）
                uint idleMilliseconds = currentTickCount - lastInputInfo.dwTime;
                
                // 更新空闲秒数
                double previousIdleSeconds = IdleSeconds;
                IdleSeconds = idleMilliseconds / 1000.0;
                
                // 检测输入事件（空闲时间减少 = 有新输入）
                if (IdleSeconds < previousIdleSeconds - 0.5) // 0.5秒容差，避免误触发
                {
                    InputDetected?.Invoke(this, new InputEventArgs { InputType = "Input" });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InputMonitor] Error: {ex.Message}");
        }
        
        // 检查空闲状态变化
        bool currentlyIdle = IsIdle;
        
        if (currentlyIdle && !_wasIdle)
        {
            // 刚刚进入空闲状态
            _wasIdle = true;
            IdleStarted?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"[InputMonitor] User became IDLE after {IdleThresholdSeconds}s of inactivity");
        }
        else if (!currentlyIdle && _wasIdle)
        {
            // 刚刚从空闲恢复
            _wasIdle = false;
            IdleEnded?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("[InputMonitor] User is now ACTIVE");
        }
    }
    
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
    
    // ===== Win32 API =====
    
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
    
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    
    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();
}
