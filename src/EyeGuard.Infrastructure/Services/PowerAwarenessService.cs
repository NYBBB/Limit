using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace EyeGuard.Infrastructure.Services;

/// <summary>
/// 电源感知服务 - Phase 10
/// 使用定时轮询检测电源状态（每3秒），状态变化时触发事件
/// </summary>
public class PowerAwarenessService : IDisposable
{
    private static PowerAwarenessService? _instance;
    public static PowerAwarenessService Instance => _instance ??= new PowerAwarenessService();
    
    private bool _isOnBattery = false;
    private bool _isEcoModeActive = false;
    private Timer? _pollTimer;
    private const int PollIntervalMs = 3000; // 3秒轮询
    
    /// <summary>
    /// 是否使用电池供电
    /// </summary>
    public bool IsOnBattery => _isOnBattery;
    
    /// <summary>
    /// 是否处于省电模式（电池供电且用户未禁用）
    /// </summary>
    public bool IsEcoModeActive => _isEcoModeActive;
    
    /// <summary>
    /// 电源模式变化事件
    /// </summary>
    public event EventHandler<bool>? EcoModeChanged;
    
    // Win32 API
    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;        // 0 = Offline (电池), 1 = Online (交流电), 255 = Unknown
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }
    
    private PowerAwarenessService()
    {
        // 初始化时检测当前电源状态
        CheckPowerStatus();
        
        // 启动轮询定时器
        _pollTimer = new Timer(OnPollTimer, null, PollIntervalMs, PollIntervalMs);
        
        Debug.WriteLine($"[PowerAwareness] Initialized with polling. OnBattery={_isOnBattery}, EcoMode={_isEcoModeActive}");
    }
    
    /// <summary>
    /// 定时器回调：轮询电源状态
    /// </summary>
    private void OnPollTimer(object? state)
    {
        try
        {
            bool wasEcoMode = _isEcoModeActive;
            CheckPowerStatus();
            
            // 如果 Eco 模式状态变化，触发事件
            if (wasEcoMode != _isEcoModeActive)
            {
                Debug.WriteLine($"[PowerAwareness] EcoMode changed: {wasEcoMode} -> {_isEcoModeActive}");
                EcoModeChanged?.Invoke(this, _isEcoModeActive);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerAwareness] Poll error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 检测当前电源状态
    /// </summary>
    private void CheckPowerStatus()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
            {
                // ACLineStatus: 0 = 电池, 1 = 交流电
                _isOnBattery = status.ACLineStatus == 0;
                
                // 检查用户设置是否允许 Eco 模式
                var settings = SettingsService.Instance.Settings;
                _isEcoModeActive = _isOnBattery && settings.EcoModeEnabled;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerAwareness] Error checking power status: {ex.Message}");
            _isOnBattery = false;
            _isEcoModeActive = false;
        }
    }
    
    /// <summary>
    /// 手动刷新电源状态（用于设置变更后）
    /// </summary>
    public void RefreshStatus()
    {
        bool wasEcoMode = _isEcoModeActive;
        CheckPowerStatus();
        
        if (wasEcoMode != _isEcoModeActive)
        {
            EcoModeChanged?.Invoke(this, _isEcoModeActive);
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _pollTimer?.Dispose();
        _pollTimer = null;
    }
}
