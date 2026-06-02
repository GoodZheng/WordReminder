using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WordReminder.Converters;

/// <summary>
/// 将集合是否为空转换为 Visibility
/// 参数 "Invert" 反转逻辑：空=Collapsed, 非空=Visible
/// 默认：空=Visible, 非空=Collapsed
/// </summary>
public class CollectionEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isEmpty = true;
        if (value is System.Collections.IEnumerable collection)
        {
            // 检查集合是否为空
            foreach (var item in collection)
            {
                isEmpty = false;
                break;
            }
        }

        var invert = parameter?.ToString() == "Invert";
        if (invert)
        {
            // 反转：空=Collapsed, 非空=Visible
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            // 默认：空=Visible, 非空=Collapsed
            return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
