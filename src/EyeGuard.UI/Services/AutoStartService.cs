namespace EyeGuard.UI.Services;

using System;
using System.Diagnostics;
using Microsoft.Win32;

/// <summary>
/// 开机自启动服务
/// </summary>
public class AutoStartService
{
    private const string AppName = "Limit";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    
    /// <summary>
    /// 启用开机自启动
    /// </summary>
    public static bool Enable()
    {
        try
        {
            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // .NET 应用的实际可执行文件路径
            var exePath = appPath.Replace(".dll", ".exe");
            
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key != null)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
                Debug.WriteLine($"[AutoStart] Enabled: {exePath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoStart] Failed to enable: {ex.Message}");
        }
        return false;
    }
    
    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    public static bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key != null)
            {
                key.DeleteValue(AppName, false);
                Debug.WriteLine("[AutoStart] Disabled");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoStart] Failed to disable: {ex.Message}");
        }
        return false;
    }
    
    /// <summary>
    /// 检查是否已启用开机自启动
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            if (key != null)
            {
                var value = key.GetValue(AppName);
                return value != null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoStart] Failed to check status: {ex.Message}");
        }
        return false;
    }
}
