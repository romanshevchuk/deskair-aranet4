using System.Windows;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

public sealed class LiveChart : ChartBase
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MetricProperty = DependencyProperty.Register(
        nameof(Metric),
        typeof(Metric),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(Metric.Co2, FrameworkPropertyMetadataOptions.AffectsRender));


    public IReadOnlyList<ChartPoint>? Points
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public Metric Metric
    {
        get => (Metric)GetValue(MetricProperty);
        set => SetValue(MetricProperty, value);
    }

    // TODO(CHART-20) [L, sonnet]
    // Spec: docs/design/components.md "Live chart"; docs/design/screens-and-states.md "Screens" (Live)
    // Input : Points and the metric.
    // Output: Layers in order: BandLayer, GridLayer, DashedZoneLinesLayer, ZoneLineLayer (CO2) or a plain metric line, CrosshairLayer, AxisLabelLayer.
    // Rules : Non-CO2 metrics fill the plot with Metric.*.Fill and the ideal range with Metric.*.Ideal (design-tokens.md "Metric colours").
    // Edge  : No points shows the Chart_NoData text, not an empty frame.
    // Done  : The gallery shows the chart with sample data from the simulated sensor, hover working.
    protected override IReadOnlyList<IChartLayer> CreateLayers()
        => throw new NotImplementedException("TODO(CHART-20)");
}
