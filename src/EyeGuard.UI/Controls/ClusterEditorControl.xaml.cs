using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using EyeGuard.Infrastructure.Services;
using EyeGuard.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EyeGuard.UI.Controls;

/// <summary>
/// 应用标签数据模型
/// </summary>
public class AppTagItem
{
    public string ProcessName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string IconGlyph { get; set; } = "\uE74C";
}

/// <summary>
/// Cluster 桶数据模型
/// </summary>
public class ClusterBucket
{
    public Cluster Cluster { get; set; } = new();
    public ObservableCollection<AppTagItem> Apps { get; } = new();
}

/// <summary>
/// Cluster 编辑器控件 - Phase 4
/// 拖拽式应用分类管理
/// </summary>
public sealed partial class ClusterEditorControl : UserControl
{
    private readonly ClusterService _clusterService;
    private readonly DatabaseService _databaseService;
    
    public ClusterEditorControl()
    {
        this.InitializeComponent();
        _clusterService = App.Services.GetRequiredService<ClusterService>();
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        
        this.Loaded += ClusterEditorControl_Loaded;
    }

    // 未分类应用列表
    public ObservableCollection<AppTagItem> UnclassifiedApps { get; } = new();
    
    // Cluster 桶列表
    public List<ClusterBucket> ClusterBuckets { get; } = new();

    private async void ClusterEditorControl_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }
    
    /// <summary>
    /// 公开的刷新方法，供外部调用（如恢复默认后）
    /// </summary>
    public async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // 确保服务已初始化
            await _clusterService.InitializeAsync();
            
            // 1. 加载所有 Cluster
            var clusters = _clusterService.GetAllClusters();
            ClusterBuckets.Clear();
            ClusterBucketsPanel.Children.Clear();
            
            foreach (var cluster in clusters)
            {
                var bucket = new ClusterBucket { Cluster = cluster };
                
                // 获取该 Cluster 的应用列表 (使用 Cluster.AppList 属性)
                var apps = cluster.AppList;
                foreach (var appName in apps)
                {
                    bucket.Apps.Add(new AppTagItem
                    {
                        ProcessName = appName,
                        DisplayName = Services.IconMapper.GetFriendlyName(appName),
                        IconGlyph = Services.IconMapper.GetAppIcon(appName)
                    });
                }
                
                ClusterBuckets.Add(bucket);
                
                // 创建 Cluster 桶 UI
                var bucketUI = CreateClusterBucketUI(bucket);
                ClusterBucketsPanel.Children.Add(bucketUI);
            }
            
            // 2. 加载未分类应用
            UnclassifiedApps.Clear();
            var allUsedApps = await GetAllUsedAppsAsync();
            var classifiedApps = ClusterBuckets.SelectMany(b => b.Apps.Select(a => a.ProcessName)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            foreach (var appName in allUsedApps.Where(a => !classifiedApps.Contains(a)))
            {
                UnclassifiedApps.Add(new AppTagItem
                {
                    ProcessName = appName,
                    DisplayName = Services.IconMapper.GetFriendlyName(appName),
                    IconGlyph = Services.IconMapper.GetAppIcon(appName)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClusterEditor] LoadData error: {ex.Message}");
        }
    }

    private async Task<List<string>> GetAllUsedAppsAsync()
    {
        // 从数据库获取最近使用过的所有应用（过去7天）
        var allApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        for (int i = 0; i < 7; i++)
        {
            var date = DateTime.Today.AddDays(-i);
            var records = await _databaseService.GetHourlyUsageAsync(date);
            foreach (var record in records)
            {
                allApps.Add(record.AppName);
            }
        }
        
        return allApps.ToList();
    }

    private Border CreateClusterBucketUI(ClusterBucket bucket)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            AllowDrop = true
        };
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // 标题
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new FontIcon { Glyph = GetClusterIcon(bucket.Cluster.Name), FontSize = 18 });
        header.Children.Add(new TextBlock { Text = bucket.Cluster.Name, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        header.Children.Add(new TextBlock 
        { 
            Text = $"({bucket.Apps.Count})", 
            FontSize = 12, 
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetRow(header, 0);
        grid.Children.Add(header);
        
        // 应用标签容器 - 使用 StackPanel 手动添加每个应用
        var appsContainer = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        
        foreach (var app in bucket.Apps)
        {
            var appBorder = CreateAppTagUI(app);
            appsContainer.Children.Add(appBorder);
        }
        
        var scrollViewer = new ScrollViewer { Content = appsContainer, MaxHeight = 200 };
        Grid.SetRow(scrollViewer, 1);
        grid.Children.Add(scrollViewer);
        
        // 拖放处理
        border.DragOver += (s, e) =>
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.Handled = true;
        };
        
        border.Drop += async (s, e) =>
        {
            try
            {
                // 从拖拽数据中获取进程名（使用标准文本格式）
                if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
                {
                    var processName = await e.DataView.GetTextAsync();
                    if (!string.IsNullOrEmpty(processName))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ClusterEditor] Drop: {processName} -> Cluster {bucket.Cluster.Name}");
                        await MoveAppToClusterAsync(processName, bucket.Cluster.Id);
                    }
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClusterEditor] Drop error: {ex.Message}");
            }
        };
        
        border.Child = grid;
        return border;
    }
    
    private Border CreateAppTagUI(AppTagItem app, bool isDraggable = true)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["ControlAltFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6, 8, 6),
            CanDrag = isDraggable,
            Tag = app  // 存储 AppTagItem 以便拖拽时使用
        };
        
        // 添加拖拽事件
        if (isDraggable)
        {
            border.DragStarting += (s, e) =>
            {
                if (s is Border b && b.Tag is AppTagItem item)
                {
                    e.Data.SetText(item.ProcessName);
                    e.Data.Properties.Add("AppProcessName", item.ProcessName);
                }
            };
        }
        
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        stack.Children.Add(new FontIcon { Glyph = app.IconGlyph, FontSize = 14 });
        stack.Children.Add(new TextBlock 
        { 
            Text = app.DisplayName, 
            FontSize = 13, 
            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
            MaxWidth = 150
        });
        
        border.Child = stack;
        return border;
    }

    private ItemsPanelTemplate? CreateWrapPanel()
    {
        // 使用默认布局
        return null;
    }

    private DataTemplate? CreateAppTagTemplate()
    {
        // 使用默认模板
        return null;
    }

    private string GetClusterIcon(string clusterName)
    {
        return clusterName switch
        {
            "Coding" => "\uE943",      // 代码
            "Writing" => "\uE8D2",     // 文档
            "Meeting" => "\uE779",     // 视频
            "Entertainment" => "\uE714", // 娱乐
            "Communication" => "\uE8BD", // 消息
            "Browsing" => "\uE774",    // 浏览器
            _ => "\uE8F1"              // 默认
        };
    }

    private async Task MoveAppToClusterAsync(string processName, int clusterId)
    {
        try
        {
            await _clusterService.AddAppToClusterAsync(clusterId, processName);
            await LoadDataAsync(); // 刷新UI
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClusterEditor] MoveApp error: {ex.Message}");
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.ToLower();
        // 简单过滤（实际实现需要更新 ItemsSource）
        foreach (var item in UnclassifiedApps)
        {
            // 过滤逻辑
        }
    }

    private void AppTag_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is Border border && border.DataContext is AppTagItem app)
        {
            args.Data.SetText(app.ProcessName);
            args.Data.Properties.Add("AppProcessName", app.ProcessName);
        }
    }
    
    private void UnclassifiedArea_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
    }
    
    private async void UnclassifiedArea_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                var processName = await e.DataView.GetTextAsync();
                if (!string.IsNullOrEmpty(processName))
                {
                    System.Diagnostics.Debug.WriteLine($"[ClusterEditor] Drop: {processName} -> Unclassified");
                    await RemoveAppFromAllClustersAsync(processName);
                }
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClusterEditor] Unclassified Drop error: {ex.Message}");
        }
    }
    
    private async Task RemoveAppFromAllClustersAsync(string processName)
    {
        try
        {
            // 从所有 Cluster 移除该应用
            foreach (var bucket in ClusterBuckets)
            {
                if (bucket.Apps.Any(a => a.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)))
                {
                    await _clusterService.RemoveAppFromClusterAsync(bucket.Cluster.Id, processName);
                }
            }
            await LoadDataAsync(); // 刷新 UI
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClusterEditor] RemoveApp error: {ex.Message}");
        }
    }
}
