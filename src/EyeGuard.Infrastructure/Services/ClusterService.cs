namespace EyeGuard.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EyeGuard.Core.Entities;

/// <summary>
/// 工作流簇服务 - Limit 3.0
/// 管理 Cluster 的 CRUD 操作，提供应用归类查询。
/// </summary>
public class ClusterService
{
    private readonly DatabaseService _databaseService;
    private List<Cluster> _cachedClusters = new();
    private bool _isInitialized = false;
    
    // 缓存：进程名 -> Cluster Id 映射
    private readonly Dictionary<string, int?> _processClusterCache = new();
    private const int CacheMaxSize = 200;
    
    public ClusterService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }
    
    /// <summary>
    /// 初始化服务，加载簇列表并创建预设
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        await _databaseService.InitializeAsync();
        await LoadClustersAsync();
        await EnsureSystemPresetsAsync();
        _isInitialized = true;
    }
    
    /// <summary>
    /// 从数据库加载所有簇
    /// </summary>
    private async Task LoadClustersAsync()
    {
        _cachedClusters = await _databaseService.GetAllClustersAsync();
        _processClusterCache.Clear();
    }
    
    /// <summary>
    /// 确保系统预设簇存在
    /// </summary>
    private async Task EnsureSystemPresetsAsync()
    {
        var presets = GetSystemPresets();
        
        foreach (var preset in presets)
        {
            var existing = _cachedClusters.FirstOrDefault(c => 
                c.IsSystemPreset && c.Name == preset.Name);
            
            if (existing == null)
            {
                await _databaseService.SaveClusterAsync(preset);
                _cachedClusters.Add(preset);
            }
        }
    }
    
    /// <summary>
    /// 定义系统预设簇
    /// </summary>
    private List<Cluster> GetSystemPresets()
    {
        return new List<Cluster>
        {
            new Cluster
            {
                Name = "Coding",
                Description = "编程开发工作流",
                AppListJson = "[\"devenv\", \"code\", \"rider\", \"idea64\", \"pycharm64\", \"webstorm64\", \"Unity\", \"UnrealEditor\", \"AndroidStudio\"]",
                BrowserKeywordsJson = "[\"GitHub\", \"StackOverflow\", \"API\", \"Docs\", \"Documentation\", \"MDN\", \"npm\", \"NuGet\", \"MSDN\"]",
                IsSystemPreset = true,
                LoadWeight = 1.0,
                Color = "#00B294"  // Teal
            },
            new Cluster
            {
                Name = "Writing",
                Description = "文档写作工作流",
                AppListJson = "[\"WINWORD\", \"EXCEL\", \"POWERPNT\", \"ONENOTE\", \"Notion\", \"Obsidian\", \"Typora\"]",
                BrowserKeywordsJson = "[\"Google Docs\", \"Notion\", \"Confluence\", \"Wiki\"]",
                IsSystemPreset = true,
                LoadWeight = 0.9,
                Color = "#8A2BE2"  // BlueViolet
            },
            new Cluster
            {
                Name = "Meeting",
                Description = "会议沟通工作流",
                AppListJson = "[\"Teams\", \"Zoom\", \"Slack\", \"Discord\", \"WeChat\", \"DingTalk\", \"Lark\", \"TencentMeeting\"]",
                BrowserKeywordsJson = "[\"Meet\", \"Zoom\", \"Teams\"]",
                IsSystemPreset = true,
                LoadWeight = 0.8,
                Color = "#0078D4"  // Blue
            },
            new Cluster
            {
                Name = "Entertainment",
                Description = "娱乐放松",
                AppListJson = "[\"steam\", \"epicgameslauncher\", \"Spotify\", \"PotPlayerMini64\", \"vlc\"]",
                BrowserKeywordsJson = "[\"YouTube\", \"Netflix\", \"Bilibili\", \"Twitch\", \"抖音\", \"哔哩哔哩\"]",
                IsSystemPreset = true,
                LoadWeight = 0.3,
                Color = "#FF6D00"  // Orange
            }
        };
    }
    
    /// <summary>
    /// 获取所有簇
    /// </summary>
    public List<Cluster> GetAllClusters()
    {
        return _cachedClusters.ToList();
    }
    
    /// <summary>
    /// 根据 ID 获取簇
    /// </summary>
    public Cluster? GetClusterById(int id)
    {
        return _cachedClusters.FirstOrDefault(c => c.Id == id);
    }
    
    /// <summary>
    /// 根据名称获取簇
    /// </summary>
    public Cluster? GetClusterByName(string name)
    {
        return _cachedClusters.FirstOrDefault(c => 
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 获取指定进程所属的簇（带缓存）
    /// </summary>
    public Cluster? GetClusterForProcess(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return null;
        
        // 检查缓存
        if (_processClusterCache.TryGetValue(processName, out var cachedId))
        {
            return cachedId.HasValue ? GetClusterById(cachedId.Value) : null;
        }
        
        // 查找匹配的簇
        var cluster = _cachedClusters.FirstOrDefault(c => c.ContainsProcess(processName));
        
        // 更新缓存
        if (_processClusterCache.Count >= CacheMaxSize)
        {
            // 简单的 LRU：清空一半缓存
            var keysToRemove = _processClusterCache.Keys.Take(CacheMaxSize / 2).ToList();
            foreach (var key in keysToRemove)
            {
                _processClusterCache.Remove(key);
            }
        }
        _processClusterCache[processName] = cluster?.Id;
        
        return cluster;
    }
    
    /// <summary>
    /// 根据浏览器标题匹配簇
    /// </summary>
    public Cluster? GetClusterForBrowserTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return null;
        return _cachedClusters.FirstOrDefault(c => c.MatchesBrowserTitle(title));
    }
    
    /// <summary>
    /// 添加新簇
    /// </summary>
    public async Task<Cluster> AddClusterAsync(Cluster cluster)
    {
        cluster.IsSystemPreset = false;
        cluster.CreatedAt = DateTime.Now;
        await _databaseService.SaveClusterAsync(cluster);
        _cachedClusters.Add(cluster);
        _processClusterCache.Clear();
        return cluster;
    }
    
    /// <summary>
    /// 更新簇
    /// </summary>
    public async Task UpdateClusterAsync(Cluster cluster)
    {
        await _databaseService.SaveClusterAsync(cluster);
        var index = _cachedClusters.FindIndex(c => c.Id == cluster.Id);
        if (index >= 0)
        {
            _cachedClusters[index] = cluster;
        }
        _processClusterCache.Clear();
    }
    
    /// <summary>
    /// 删除簇（系统预设不可删除）
    /// </summary>
    public async Task<bool> DeleteClusterAsync(int clusterId)
    {
        var cluster = GetClusterById(clusterId);
        if (cluster == null || cluster.IsSystemPreset)
        {
            return false;
        }
        
        await _databaseService.DeleteClusterAsync(clusterId);
        _cachedClusters.RemoveAll(c => c.Id == clusterId);
        _processClusterCache.Clear();
        return true;
    }
    
    /// <summary>
    /// 将应用添加到指定簇
    /// </summary>
    public async Task AddAppToClusterAsync(int clusterId, string processName)
    {
        var cluster = GetClusterById(clusterId);
        if (cluster == null) return;
        
        cluster.AddApp(processName);
        await UpdateClusterAsync(cluster);
    }
    
    /// <summary>
    /// 从指定簇移除应用
    /// </summary>
    public async Task RemoveAppFromClusterAsync(int clusterId, string processName)
    {
        var cluster = GetClusterById(clusterId);
        if (cluster == null) return;
        
        cluster.RemoveApp(processName);
        await UpdateClusterAsync(cluster);
    }
    
    /// <summary>
    /// 判断两个进程是否属于同一簇（用于计算切换惩罚）
    /// </summary>
    public bool AreInSameCluster(string processName1, string processName2)
    {
        var cluster1 = GetClusterForProcess(processName1);
        var cluster2 = GetClusterForProcess(processName2);
        
        // 都不属于任何簇 = 不算同簇
        if (cluster1 == null || cluster2 == null) return false;
        
        return cluster1.Id == cluster2.Id;
    }
    
    /// <summary>
    /// 获取进程的负载权重（考虑簇定义）
    /// </summary>
    public double GetLoadWeightForProcess(string processName, string? browserTitle = null)
    {
        // 优先检查浏览器标题
        if (!string.IsNullOrEmpty(browserTitle))
        {
            var browserCluster = GetClusterForBrowserTitle(browserTitle);
            if (browserCluster != null)
            {
                return browserCluster.LoadWeight;
            }
        }
        
        // 检查进程名
        var cluster = GetClusterForProcess(processName);
        return cluster?.LoadWeight ?? 0.7; // 默认中等负载
    }
    
    /// <summary>
    /// 恢复所有簇为默认设置（删除所有现有簇并重新创建预设）
    /// </summary>
    public async Task ResetToDefaultAsync()
    {
        // 删除所有现有簇
        foreach (var cluster in _cachedClusters.ToList())
        {
            await _databaseService.DeleteClusterAsync(cluster.Id);
        }
        
        _cachedClusters.Clear();
        _processClusterCache.Clear();
        
        // 重新创建系统预设
        var presets = GetSystemPresets();
        foreach (var preset in presets)
        {
            await _databaseService.SaveClusterAsync(preset);
            _cachedClusters.Add(preset);
        }
        
        System.Diagnostics.Debug.WriteLine("[ClusterService] 已恢复为默认分类设置");
    }
}
