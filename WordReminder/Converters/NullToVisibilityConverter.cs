using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WordReminder.Converters;

/// <summary>
/// 将 null 转换为 Visibility（非 null=Visible, null=Collapsed）
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
