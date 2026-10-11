using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-10) [S, haiku]
// Spec: docs/product/behaviour-rules.md "Aggregation" rule 5; docs/design/components.md "Stats card"
// Input : Coverage fraction from 0 to 1.
// Output: A Pill reading "Based on N % of expected readings".
// Rules : Add the string key Coverage_Chip (en and uk) with a {percent} placeholder; the view model decides visibility with CoverageCalculator.ShouldShowChip.
// Edge  : Null fraction shows nothing.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class CoverageChip : Control
{
    public static readonly DependencyProperty FractionProperty = DependencyProperty.Register(
        nameof(Fraction),
        typeof(double?),
        typeof(CoverageChip),
        new PropertyMetadata(null));

    static CoverageChip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CoverageChip), new FrameworkPropertyMetadata(typeof(CoverageChip)));
    }

    public double? Fraction
    {
        get => (double?)GetValue(FractionProperty);
        set => SetValue(FractionProperty, value);
    }
}
