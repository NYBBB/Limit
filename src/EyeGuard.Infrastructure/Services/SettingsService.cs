namespace EyeGuard.Infrastructure.Services;

using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

/// <summary>
/// 用户设置数据模型。
/// </summary>
public class UserSettings
{
    // ===== 模式设置 =====
    public bool IsSmartMode { get; set; } = true;
    
    // ===== 简单模式设置 =====
    public int MicroBreakIntervalMinutes { get; set; } = 15;
    public int MicroBreakDurationSeconds { get; set; } = 20;
    public int LongBreakIntervalMinutes { get; set; } = 45;
    public int LongBreakDurationMinutes { get; set; } = 5;
    
    // ===== 智能模式设置 =====
    public int SoftReminderThreshold { get; set; } = 40;
    public int ForceBreakThreshold { get; set; } = 80;
    public int IdleThresholdSeconds { get; set; } = 60;
    public bool EnableKeyboardMonitor { get; set; } = true;
    public bool EnableAudioMonitor { get; set; } = true;
    
    // ===== 提醒设置 =====
    public bool EnableReminders { get; set; } = true;
    public int ReminderType { get; set; } = 0; // 0=全屏弹窗, 1=通知横幅
    
    // ===== 数据持久化设置 =====
    /// <summary>
    /// 疲劳快照保存间隔（秒），用于后台持久化
    /// </summary>
    public int FatigueSnapshotIntervalSeconds { get; set; } = 60;
    
    /// <summary>
    /// 疲劳趋势图表绘制间隔（分钟），控制图表数据点密度
    /// </summary>
    public int FatigueChartIntervalMinutes { get; set; } = 5;
    
    /// <summary>
    /// Dashboard 数据刷新间隔（秒），控制应用使用列表更新频率
    /// </summary>
    public int DashboardRefreshIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// 设置服务 - 负责保存和加载用户配置。
/// 使用 JSON 文件存储在用户 AppData 目录。
/// </summary>
public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();
    
    private readonly string _settingsFilePath;
    private UserSettings _settings;
    
    /// <summary>
    /// 当前设置。
    /// </summary>
    public UserSettings Settings => _settings;
    
    /// <summary>
    /// 设置变更事件。
    /// </summary>
    public event EventHandler? SettingsChanged;

    private SettingsService()
    {
        // 设置文件路径: %LOCALAPPDATA%\EyeGuard\settings.json
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var eyeGuardPath = Path.Combine(appDataPath, "EyeGuard");
        
        // 确保目录存在
        Directory.CreateDirectory(eyeGuardPath);
        
        _settingsFilePath = Path.Combine(eyeGuardPath, "settings.json");
        _settings = new UserSettings();
        
        // 尝试加载已有设置
        Load();
    }

    /// <summary>
    /// 加载设置。
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<UserSettings>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                    Debug.WriteLine($"[SettingsService] Loaded settings from {_settingsFilePath}");
                }
            }
            else
            {
                Debug.WriteLine("[SettingsService] No settings file found, using defaults");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Error loading settings: {ex.Message}");
            _settings = new UserSettings();
        }
    }

    /// <summary>
    /// 保存设置。
    /// </summary>
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json);
            Debug.WriteLine($"[SettingsService] Saved settings to {_settingsFilePath}");
            
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新设置并保存。
    /// </summary>
    public void UpdateAndSave(Action<UserSettings> updateAction)
    {
        updateAction(_settings);
        Save();
    }
}
