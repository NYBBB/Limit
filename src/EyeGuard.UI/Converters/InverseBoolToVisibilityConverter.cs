using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace EyeGuard.UI.Converters;

/// <summary>
/// 反转布尔值到可见性转换器。
/// true -> Collapsed, false -> Visible
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Collapsed;
        }
        return false;
    }
}
