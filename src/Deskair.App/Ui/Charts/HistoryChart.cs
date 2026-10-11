using System.Windows;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

public sealed class HistoryChart : ChartBase
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarsProperty = DependencyProperty.Register(
        nameof(Bars),
        typeof(IReadOnlyList<ChartBar>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GapsProperty = DependencyProperty.Register(
        nameof(Gaps),
        typeof(IReadOnlyList<ChartGap>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WeekendSpansProperty = DependencyProperty.Register(
        nameof(WeekendSpans),
        typeof(IReadOnlyList<ChartGap>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MetricProperty = DependencyProperty.Register(
        nameof(Metric),
        typeof(Metric),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(Metric.Co2, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowBarsProperty = DependencyProperty.Register(
        nameof(ShowBars),
        typeof(bool),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


    public IReadOnlyList<ChartPoint>? Points
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IReadOnlyList<ChartBar>? Bars
    {
        get => (IReadOnlyList<ChartBar>?)GetValue(BarsProperty);
        set => SetValue(BarsProperty, value);
    }

    public IReadOnlyList<ChartGap>? Gaps
    {
        get => (IReadOnlyList<ChartGap>?)GetValue(GapsProperty);
        set => SetValue(GapsProperty, value);
    }

    public IReadOnlyList<ChartGap>? WeekendSpans
    {
        get => (IReadOnlyList<ChartGap>?)GetValue(WeekendSpansProperty);
        set => SetValue(WeekendSpansProperty, value);
    }

    public Metric Metric
    {
        get => (Metric)GetValue(MetricProperty);
        set => SetValue(MetricProperty, value);
    }

    public bool ShowBars
    {
        get => (bool)GetValue(ShowBarsProperty);
        set => SetValue(ShowBarsProperty, value);
    }

    // TODO(CHART-21) [L, sonnet]
    // Spec: docs/design/components.md "History chart"; docs/design/screens-and-states.md "History controls"
    // Input : Points (hourly line) or bars, gaps, weekend spans, metric, and whether to show bars.
    // Output: Hourly line with band for Day; floating min to max bars with average ticks otherwise; weekend shading; hatched gaps; dashed partial bars.
    // Rules : Shares ChartBase with LiveChart; hover says "only N of M h recorded" for partial buckets.
    // Edge  : No data shows Chart_NoData.
    // Done  : The gallery shows complete, partial and no-data buckets.
    protected override IReadOnlyList<IChartLayer> CreateLayers()
        => throw new NotImplementedException("TODO(CHART-21)");
}
