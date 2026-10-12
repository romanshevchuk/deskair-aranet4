using System.Windows;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

/// <summary>A small line in the metric colour with no axes, for the metric cards. Fewer than two values draw nothing.</summary>
public sealed class Sparkline : ChartBase
{
    private const double VerticalPadding = 3;

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

    protected override Thickness PlotMargin => new(0, VerticalPadding, 0, VerticalPadding);

    protected override ChartRanges GetDataRanges()
    {
        var points = Points?.Where(point => point.Value is not null).ToList() ?? [];

        if (points.Count == 0)
        {
            return new ChartRanges(0, 1, 0, 1);
        }

        return new ChartRanges(
            points.Min(point => point.Time.UtcTicks),
            points.Max(point => point.Time.UtcTicks),
            points.Min(point => point.Value!.Value),
            points.Max(point => point.Value!.Value));
    }

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        var enough = Points?.Count(point => point.Value is not null) >= 2;

        return enough ? [new LineLayer { Points = Points, BrushKey = $"Metric.{Metric}.Brush" }] : [];
    }
}
