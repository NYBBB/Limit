namespace EyeGuard.Infrastructure.Services;

using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

/// <summary>
/// 用户设置数据模型。
/// 所有值都从 JSON 文件加载/保存，其他服务直接读取 SettingsService.Instance.Settings
/// </summary>
public class UserSettings
{
    // ===== 模式设置 =====
    /// <summary>是否为智能模式（否则为简单模式）</summary>
    /// <remarks>使用处: RulesPage 模式切换</remarks>
    public bool IsSmartMode { get; set; } = true;
    
    // ===== 简单模式设置 =====
    /// <summary>短休息间隔（分钟）</summary>
    /// <remarks>使用处: 简单模式休息提醒逻辑</remarks>
    public int MicroBreakIntervalMinutes { get; set; } = 15;
    
    /// <summary>短休息时长（秒）</summary>
    /// <remarks>使用处: 简单模式休息弹窗时长</remarks>
    public int MicroBreakDurationSeconds { get; set; } = 20;
    
    /// <summary>长休息间隔（分钟）</summary>
    /// <remarks>使用处: 简单模式休息提醒逻辑</remarks>
    public int LongBreakIntervalMinutes { get; set; } = 45;
    
    /// <summary>长休息时长（分钟）</summary>
    /// <remarks>使用处: 简单模式休息弹窗时长</remarks>
    public int LongBreakDurationMinutes { get; set; } = 5;
    
    // ===== 智能模式设置 =====
    /// <summary>轻度提醒疲劳阈值（%）</summary>
    /// <remarks>使用处: InterventionPolicy L2 触发条件</remarks>
    public int SoftReminderThreshold { get; set; } = 40;
    
    /// <summary>强制休息疲劳阈值（%）</summary>
    /// <remarks>使用处: InterventionPolicy L4 触发条件</remarks>
    public int ForceBreakThreshold { get; set; } = 80;
    
    /// <summary>空闲判定时间（秒）</summary>
    /// <remarks>使用处: UserActivityManager.Tick() 判断是否进入空闲状态</remarks>
    public int IdleThresholdSeconds { get; set; } = 60;
    
    /// <summary>是否启用键盘监控</summary>
    /// <remarks>使用处: GlobalInputMonitor 键盘钩子</remarks>
    public bool EnableKeyboardMonitor { get; set; } = true;
    
    /// <summary>是否启用音频检测</summary>
    /// <remarks>使用处: AudioDetector 音频播放检测</remarks>
    public bool EnableAudioMonitor { get; set; } = true;
    
    // ===== 敏感度设置 =====
    /// <summary>Care Mode 敏感度（0-100）</summary>
    /// <remarks>使用处: FatigueEngine 疲劳增长速率调节</remarks>
    public int CareSensitivity { get; set; } = 50;
    
    // ===== 干预策略设置 =====
    /// <summary>干预模式（0=礼貌, 1=平衡, 2=强制）</summary>
    /// <remarks>使用处: InterventionPolicy 弹窗行为配置</remarks>
    public int InterventionMode { get; set; } = 1;
    
    // ===== 提醒设置 =====
    /// <summary>是否启用休息提醒</summary>
    /// <remarks>使用处: InterventionPolicy 总开关</remarks>
    public bool EnableReminders { get; set; } = true;
    
    /// <summary>提醒类型（0=全屏弹窗, 1=通知横幅）</summary>
    /// <remarks>使用处: InterventionPolicy 弹窗类型</remarks>
    public int ReminderType { get; set; } = 0;
    
    // ===== 常规设置 =====
    /// <summary>是否显示系统托盘图标</summary>
    /// <remarks>使用处: MainWindow 托盘图标显示</remarks>
    public bool ShowTrayIcon { get; set; } = true;
    
    /// <summary>是否开机自启动</summary>
    /// <remarks>使用处: 开机启动注册表/任务计划</remarks>
    public bool AutoStartOnBoot { get; set; } = false;
    
    /// <summary>是否启用电池省电模式</summary>
    /// <remarks>使用处: PowerAwarenessService</remarks>
    public bool EcoModeEnabled { get; set; } = true;
    
    // ===== 数据持久化设置 =====
    /// <summary>疲劳快照保存间隔（秒）</summary>
    /// <remarks>使用处: 后台持久化服务</remarks>
    public int FatigueSnapshotIntervalSeconds { get; set; } = 60;
    
    /// <summary>疲劳趋势图表绘制间隔（分钟）</summary>
    /// <remarks>使用处: DashboardViewModel3 快照保存</remarks>
    public int FatigueChartIntervalMinutes { get; set; } = 5;
    
    /// <summary>Dashboard 数据刷新间隔（秒）</summary>
    /// <remarks>使用处: DashboardViewModel3 应用列表更新</remarks>
    public int DashboardRefreshIntervalSeconds { get; set; } = 60;
    
    /// <summary>离开判定时间（秒）</summary>
    /// <remarks>使用处: UserActivityManager.Tick() 判断是否离开</remarks>
    public int AwayThresholdSeconds { get; set; } = 300;
    
    /// <summary>
    /// 重置所有设置为默认值
    /// </summary>
    public void ResetToDefault()
    {
        IsSmartMode = true;
        MicroBreakIntervalMinutes = 15;
        MicroBreakDurationSeconds = 20;
        LongBreakIntervalMinutes = 45;
        LongBreakDurationMinutes = 5;
        SoftReminderThreshold = 40;
        ForceBreakThreshold = 80;
        IdleThresholdSeconds = 60;
        EnableKeyboardMonitor = true;
        EnableAudioMonitor = true;
        CareSensitivity = 50;
        InterventionMode = 1;
        EnableReminders = true;
        ReminderType = 0;
        ShowTrayIcon = true;
        AutoStartOnBoot = false;
        EcoModeEnabled = true;
        FatigueSnapshotIntervalSeconds = 60;
        FatigueChartIntervalMinutes = 5;
        DashboardRefreshIntervalSeconds = 60;
        AwayThresholdSeconds = 300;
    }
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
