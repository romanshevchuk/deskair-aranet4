using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-09) [M, haiku]
// Spec: docs/design/design-tokens.md "Icons" (battery 26 x 12); docs/design/screens-and-states.md "Live conditions"
// Input : Battery percent (null = unknown).
// Output: A 26 x 12 battery drawn with a fill proportional to Percent.
// Rules : Green above 20 %, amber at 20 % or below, red at 10 % or below; use the Zone.*.Dot tokens (questions.md Q15).
// Edge  : Null draws an empty outline with no fill; values clamp to 0 to 100.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class BatteryIcon : Control
{
    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent),
        typeof(int?),
        typeof(BatteryIcon),
        new PropertyMetadata(null));

    static BatteryIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BatteryIcon), new FrameworkPropertyMetadata(typeof(BatteryIcon)));
    }

    public int? Percent
    {
        get => (int?)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }
}
