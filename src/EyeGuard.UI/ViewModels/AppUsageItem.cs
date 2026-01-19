using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EyeGuard.UI.ViewModels;

/// <summary>
/// 应用使用项 - 支持树形结构
/// </summary>
public partial class AppUsageItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public double Percentage { get; set; }
    
    // 图标
    public string IconGlyph { get; set; } = "\uE8FC"; // 默认：应用图标
    
    // 树形结构支持
    public bool IsBrowser { get; set; }
    
    [ObservableProperty]
    private bool _isExpanded = false; // 默认折叠
    
    public ObservableCollection<AppUsageItem> Children { get; set; } = new();
    
    // 网站细分
    public bool IsWebsite => !string.IsNullOrEmpty(WebsiteName);
    public string? WebsiteName { get; set; }
    
    // 是否有子项
    public bool HasChildren => Children.Count > 0;
}
