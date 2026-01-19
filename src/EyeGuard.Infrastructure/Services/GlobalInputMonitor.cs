namespace EyeGuard.Infrastructure.Services;

using System;
using System.Diagnostics;
using EyeGuard.Core.Interfaces;
using EyeGuard.Infrastructure.Native;

/// <summary>
/// 全局输入监测服务。
/// 使用 Windows Low-Level Hooks 监听键盘和鼠标事件。
/// </summary>
public class GlobalInputMonitor : IInputMonitor
{
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    
    // 必须保持委托引用，防止被 GC 回收
    private readonly NativeMethods.LowLevelKeyboardProc _keyboardProc;
    private readonly NativeMethods.LowLevelMouseProc _mouseProc;
    
    private DateTime _lastInputTime = DateTime.Now;
    private readonly object _lock = new();
    
    private bool _disposed;

    /// <summary>
    /// 监测器是否正在运行。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 空闲阈值（秒）。超过此时间无输入则认为用户空闲。
    /// </summary>
    public int IdleThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// 最后一次输入的时间。
    /// </summary>
    public DateTime LastInputTime
    {
        get { lock (_lock) return _lastInputTime; }
        private set { lock (_lock) _lastInputTime = value; }
    }

    /// <summary>
    /// 当前是否处于空闲状态。
    /// </summary>
    public bool IsIdle => (DateTime.Now - LastInputTime).TotalSeconds > IdleThresholdSeconds;

    /// <summary>
    /// 自上次输入以来的空闲时长（秒）。
    /// </summary>
    public double IdleSeconds => (DateTime.Now - LastInputTime).TotalSeconds;

    /// <summary>
    /// 检测到用户输入时触发。
    /// </summary>
    public event EventHandler<InputEventArgs>? InputDetected;

    /// <summary>
    /// 用户状态从活跃变为空闲时触发。
    /// </summary>
    public event EventHandler? IdleStarted;

    /// <summary>
    /// 用户状态从空闲恢复为活跃时触发。
    /// </summary>
    public event EventHandler? IdleEnded;

    private bool _wasIdle = false;

    public GlobalInputMonitor()
    {
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;
    }

    /// <summary>
    /// 启动输入监测。
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;
        
        _keyboardHookId = SetKeyboardHook(_keyboardProc);
        _mouseHookId = SetMouseHook(_mouseProc);
        
        _lastInputTime = DateTime.Now;
        IsRunning = true;
        
        Debug.WriteLine("[InputMonitor] Started - Keyboard and Mouse hooks installed");
    }

    /// <summary>
    /// 停止输入监测。
    /// </summary>
    public void Stop()
    {
        if (!IsRunning) return;
        
        if (_keyboardHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
        
        if (_mouseHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
        
        IsRunning = false;
        Debug.WriteLine("[InputMonitor] Stopped - Hooks removed");
    }

    /// <summary>
    /// 检查并触发空闲状态事件。
    /// 应该由外部定时器调用。
    /// </summary>
    public void CheckIdleState()
    {
        bool currentlyIdle = IsIdle;
        
        if (currentlyIdle && !_wasIdle)
        {
            // 刚刚进入空闲状态
            _wasIdle = true;
            IdleStarted?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine($"[InputMonitor] User became IDLE after {IdleThresholdSeconds}s of inactivity");
        }
        else if (!currentlyIdle && _wasIdle)
        {
            // 刚刚从空闲恢复
            _wasIdle = false;
            IdleEnded?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine("[InputMonitor] User is now ACTIVE");
        }
    }

    private IntPtr SetKeyboardHook(NativeMethods.LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, 
            proc,
            NativeMethods.GetModuleHandle(curModule.ModuleName), 
            0);
    }

    private IntPtr SetMouseHook(NativeMethods.LowLevelMouseProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, 
            proc,
            NativeMethods.GetModuleHandle(curModule.ModuleName), 
            0);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
            {
                OnInputDetected("Keyboard");
            }
        }
        return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            // 只对点击和滚轮响应，忽略移动（减少事件噪音）
            if (msg == NativeMethods.WM_LBUTTONDOWN || 
                msg == NativeMethods.WM_RBUTTONDOWN || 
                msg == NativeMethods.WM_MBUTTONDOWN ||
                msg == NativeMethods.WM_MOUSEWHEEL)
            {
                OnInputDetected("Mouse");
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void OnInputDetected(string inputType)
    {
        LastInputTime = DateTime.Now;
        InputDetected?.Invoke(this, new InputEventArgs { InputType = inputType });
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~GlobalInputMonitor()
    {
        Dispose();
    }
}
