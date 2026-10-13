using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Deskair.App.Ui.Markup;

/// <summary>
/// The corner radius of a pill: half its height, so both ends are half circles. A very large radius (the prototype's <c>border-radius: 999px</c>) does not do this in WPF: it is clamped on each
/// axis separately and the ends turn into ellipses as wide as half the pill. Bind a border's CornerRadius to its own ActualHeight through this converter.
/// </summary>
[ValueConversion(typeof(double), typeof(CornerRadius))]
public sealed class PillRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => new CornerRadius(value is double height && !double.IsNaN(height) ? height / 2 : 0);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
