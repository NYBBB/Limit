using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using EyeGuard.UI.ViewModels;
using EyeGuard.Infrastructure.Services;
using EyeGuard.Infrastructure.Monitors;
using EyeGuard.Core.Interfaces;

namespace EyeGuard.UI;

/// <summary>
/// EyeGuard 应用程序主类。
/// 负责初始化依赖注入容器和管理应用生命周期。
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 获取依赖注入服务提供者。
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;
    
    /// <summary>
    /// 获取主窗口实例。
    /// </summary>
    public static MainWindow MainWindow { get; private set; } = null!;

    /// <summary>
    /// 初始化应用程序。
    /// </summary>
    public App()
    {
        InitializeComponent();
        
        // 配置依赖注入
        Services = ConfigureServices();
    }

    /// <summary>
    /// 应用程序启动时调用。
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 启动数据收集服务
        var collector = Services.GetRequiredService<UsageCollectorService>();
        collector.Start();
        
        // 启动窗口追踪
        var tracker = Services.GetRequiredService<IWindowTracker>();
        tracker.Start();

        // 创建并显示主窗口
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    /// <summary>
    /// 配置依赖注入服务。
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // ===== 核心服务 =====
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IWindowTracker, WindowTracker>();
        services.AddSingleton<UsageCollectorService>();
        
        // TODO: 注册输入监测服务
        // services.AddSingleton<IInputMonitor, GlobalInputHook>();
        
        // TODO: 注册疲劳引擎
        // services.AddSingleton<IFatigueEngine, FatigueEngine>();
        
        // TODO: 注册休息调度器
        // services.AddSingleton<IBreakScheduler, BreakScheduler>();
        
        // ===== 视图模型 =====
        services.AddTransient<DashboardViewModel>();
        // services.AddTransient<SettingsViewModel>();
        
        return services.BuildServiceProvider();
    }
}
