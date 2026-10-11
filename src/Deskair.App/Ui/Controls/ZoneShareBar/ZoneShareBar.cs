using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-11) [S, haiku]
// Spec: docs/design/components.md "Stats card" (zone share)
// Input : The share of time in each zone as fractions that add up to at most 1.
// Output: A stacked bar with segments proportional to the fractions in the Zone.*.Segment colours.
// Rules : Rounded ends; the Segment.Bg track shows through when the sum is below 1.
// Edge  : All zero shows only the track.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class ZoneShareBar : Control
{
    public static readonly DependencyProperty GoodProperty = DependencyProperty.Register(
        nameof(Good),
        typeof(double),
        typeof(ZoneShareBar),
        new PropertyMetadata(default(double)));

    public static readonly DependencyProperty ElevatedProperty = DependencyProperty.Register(
        nameof(Elevated),
        typeof(double),
        typeof(ZoneShareBar),
        new PropertyMetadata(default(double)));

    public static readonly DependencyProperty HighProperty = DependencyProperty.Register(
        nameof(High),
        typeof(double),
        typeof(ZoneShareBar),
        new PropertyMetadata(default(double)));

    static ZoneShareBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoneShareBar), new FrameworkPropertyMetadata(typeof(ZoneShareBar)));
    }

    public double Good
    {
        get => (double)GetValue(GoodProperty);
        set => SetValue(GoodProperty, value);
    }

    public double Elevated
    {
        get => (double)GetValue(ElevatedProperty);
        set => SetValue(ElevatedProperty, value);
    }

    public double High
    {
        get => (double)GetValue(HighProperty);
        set => SetValue(HighProperty, value);
    }
}
