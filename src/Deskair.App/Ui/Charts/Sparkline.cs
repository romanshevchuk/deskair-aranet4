using System.Windows;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

public sealed class Sparkline : ChartBase
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MetricProperty = DependencyProperty.Register(
        nameof(Metric),
        typeof(Metric),
        typeof(Sparkline),
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

    // TODO(CHART-24) [S, haiku]
    // Spec: docs/design/components.md "Metric mini card"
    // Input : Points and the metric.
    // Output: A small line in the metric colour, no axes.
    // Rules : Reuses ChartBase with a single line layer.
    // Edge  : Fewer than two points draws nothing.
    // Done  : The gallery shows it inside MetricCard.
    protected override IReadOnlyList<IChartLayer> CreateLayers()
        => throw new NotImplementedException("TODO(CHART-24)");
}
