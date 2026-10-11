using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-03) [S, haiku]
// Spec: docs/design/components.md "Hero card" (three stats)
// Input : Items (three objects shown as KeyValueCell) and an optional Zone.
// Output: The items side by side, separated by 1 px lines in the Zone.*.Divider colour.
// Rules : Equal columns; no fixed widths (Ukrainian text is longer).
// Edge  : Fewer than three items still lay out evenly.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class StatTriple : ItemsControl
{
    public static readonly DependencyProperty ZoneProperty = DependencyProperty.Register(
        nameof(Zone),
        typeof(Zone?),
        typeof(StatTriple),
        new PropertyMetadata(null));

    static StatTriple()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatTriple), new FrameworkPropertyMetadata(typeof(StatTriple)));
    }

    public Zone? Zone
    {
        get => (Zone?)GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }
}
