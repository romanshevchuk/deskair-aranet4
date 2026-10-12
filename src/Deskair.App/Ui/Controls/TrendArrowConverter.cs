using System.Globalization;
using System.Windows.Data;
using Deskair.Domain.Analytics.Trend;

namespace Deskair.App.Ui.Controls;

/// <summary>Turns a trend into its arrow: down for falling, up for rising, right for steady.</summary>
public sealed class TrendArrowConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            TrendDirection.Falling => "↓",
            TrendDirection.Rising => "↑",
            _ => "→"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
