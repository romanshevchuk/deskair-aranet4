using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-01) [M, haiku]
// Spec: docs/design/components.md "Gauge"; docs/design/design-tokens.md "Zone colours" (gauge segments, scale 400 to 2,000 ppm)
// Input : Value in ppm (null = no marker), scale limits, the two zone limits.
// Output: Three segments in the Zone.*.Segment colours with widths proportional to the scale, ticks at the two limits, a round marker at Value.
// Rules : Grid with proportional columns (wpf-mapping.md); clamp the marker to the scale.
// Edge  : Value outside the scale clamps to the ends; null hides the marker.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class Gauge : Control
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double?),
        typeof(Gauge),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(400.0));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(2000.0));

    public static readonly DependencyProperty ElevatedFromProperty = DependencyProperty.Register(
        nameof(ElevatedFrom),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(1000.0));

    public static readonly DependencyProperty HighAboveProperty = DependencyProperty.Register(
        nameof(HighAbove),
        typeof(double),
        typeof(Gauge),
        new PropertyMetadata(1400.0));

    static Gauge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Gauge), new FrameworkPropertyMetadata(typeof(Gauge)));
    }

    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double ElevatedFrom
    {
        get => (double)GetValue(ElevatedFromProperty);
        set => SetValue(ElevatedFromProperty, value);
    }

    public double HighAbove
    {
        get => (double)GetValue(HighAboveProperty);
        set => SetValue(HighAboveProperty, value);
    }
}
