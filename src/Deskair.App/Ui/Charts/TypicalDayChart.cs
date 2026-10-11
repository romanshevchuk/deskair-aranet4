using System.Windows;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

public sealed class TypicalDayChart : ChartBase
{
    public static readonly DependencyProperty HoursProperty = DependencyProperty.Register(
        nameof(Hours),
        typeof(IReadOnlyList<HourBar>),
        typeof(TypicalDayChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));


    public IReadOnlyList<HourBar>? Hours
    {
        get => (IReadOnlyList<HourBar>?)GetValue(HoursProperty);
        set => SetValue(HoursProperty, value);
    }

    // TODO(CHART-22) [M, sonnet]
    // Spec: docs/design/components.md "Typical-day chart"; docs/product/behaviour-rules.md "Aggregation" rule 6
    // Input : 24 hour bars.
    // Output: 24 bars coloured by zone with an outline around the worst three-hour window.
    // Rules : Bars use the Zone.*.Segment colours; hover shows the hour and average.
    // Edge  : Hours without data draw no bar.
    // Done  : The gallery shows a sample day with the worst window outlined.
    protected override IReadOnlyList<IChartLayer> CreateLayers()
        => throw new NotImplementedException("TODO(CHART-22)");
}
