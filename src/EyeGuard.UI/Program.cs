using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace EyeGuard.UI;

/// <summary>
/// 程序入口点。
/// 自定义入口以便在启动时配置依赖注入容器。
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 初始化 COM 组件
        WinRT.ComWrappersSupport.InitializeComWrappers();
        
        // 启动应用程序
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            
            _ = new App();
        });
    }
}
