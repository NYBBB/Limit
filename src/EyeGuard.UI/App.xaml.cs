using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using EyeGuard.UI.ViewModels;
using EyeGuard.Infrastructure.Services;
using EyeGuard.Infrastructure.Monitors;
using EyeGuard.Core.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

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
        
        // 配置 LiveCharts2 全局中文字体
        ConfigureLiveCharts();
        
        // 配置依赖注入
        Services = ConfigureServices();
    }
    
    /// <summary>
    /// 配置 LiveCharts2 使用中文字体
    /// </summary>
    private void ConfigureLiveCharts()
    {
        LiveCharts.Configure(config =>
            config
                .HasGlobalSKTypeface(SKTypeface.FromFamilyName("Microsoft YaHei"))
        );
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
        
        // Limit 3.0: 初始化 ClusterService（加载预设簇）
        var clusterService = Services.GetRequiredService<ClusterService>();
        _ = clusterService.InitializeAsync(); // Fire and forget
        
        // Phase 6: 启动数据聚合服务（聚合过期的热数据）
        var aggregationService = Services.GetRequiredService<DataAggregationService>();
        _ = aggregationService.RunDailyAggregationAsync(); // Fire and forget
        // Limit 3.0: 启动通知服务
        var toastService = Services.GetRequiredService<EyeGuard.UI.Services.ToastNotificationService>();
        toastService.Initialize();

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
        // 使用工厂方法注册单例（因为构造函数是私有的）
        services.AddSingleton(sp => SettingsService.Instance);
        services.AddSingleton<IWindowTracker, WindowTracker>();
        services.AddSingleton<UsageCollectorService>();
        services.AddSingleton<ClusterService>();
        
        // ===== 硬件监控 =====
        services.AddSingleton<GlobalInputMonitor>();
        services.AddSingleton<IInputMonitor>(sp => sp.GetRequiredService<GlobalInputMonitor>());
        services.AddSingleton<AudioDetector>();
        
        // ===== 业务逻辑 =====
        services.AddSingleton<FatigueEngine>();
        // Phase 7: UserActivityManager 使用 DI 注入依赖
        services.AddSingleton<UserActivityManager>(sp => new UserActivityManager(
            sp.GetRequiredService<FatigueEngine>(),
            sp.GetRequiredService<GlobalInputMonitor>(),
            sp.GetRequiredService<AudioDetector>()
        ));
        services.AddSingleton<ForecastService>();
        services.AddSingleton<BreakTaskService>();
        services.AddSingleton<InterventionPolicy>();
        
        // ===== Limit 3.0: Context Monitor =====
        services.AddSingleton<ContextInsightService>();
        services.AddSingleton<IconExtractorService>(); // Phase 4: 本地图标获取
        
        // ===== Phase 6: 数据聚合 =====
        services.AddSingleton<DataAggregationService>();
        
        // ===== UI 服务 =====
        services.AddSingleton<EyeGuard.UI.Services.ToastNotificationService>();
        
        // ===== Limit 3.0 混合架构: WebView2 Bridge =====
        services.AddSingleton<EyeGuard.UI.Bridge.BridgeService>();
        
        // ===== 视图模型 =====
        services.AddTransient(sp => DashboardViewModel.Instance);
        services.AddTransient(sp => DashboardViewModel3.Instance);
        
        return services.BuildServiceProvider();
    }
}
