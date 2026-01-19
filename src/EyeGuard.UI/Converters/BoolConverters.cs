using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace EyeGuard.UI.Converters;

/// <summary>
/// 将bool转换为展开/折叠箭头图标
/// </summary>
public class BoolToChevronConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "\uE70E" : "\uE70D"; // ChevronDown : ChevronRight
        }
        return "\uE76D";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 将bool转换为Visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 将bool转换为"显示更多"/"收起"文本
/// </summary>
public class ShowMoreTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool showAll)
        {
            return showAll ? "收起" : "显示更多";
        }
        return "显示更多";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
