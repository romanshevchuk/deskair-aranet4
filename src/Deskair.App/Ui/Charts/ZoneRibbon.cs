using System.Windows;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>The 24 h ribbon of zone-coloured segments. It draws at the size it is given (the flyout uses a 22 px tall one), never a scaled-down copy.</summary>
public sealed class ZoneRibbon : ChartBase
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments),
        typeof(IReadOnlyList<ChartZoneSegment>),
        typeof(ZoneRibbon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(
        nameof(Window),
        typeof(TimeSpan),
        typeof(ZoneRibbon),
        new FrameworkPropertyMetadata(TimeSpan.FromHours(24), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register(
        nameof(EndTime),
        typeof(DateTimeOffset?),
        typeof(ZoneRibbon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<ChartZoneSegment>? Segments
    {
        get => (IReadOnlyList<ChartZoneSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    /// <summary>The time the ribbon covers, ending at the latest segment.</summary>
    public TimeSpan Window
    {
        get => (TimeSpan)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    /// <summary>The right end of the ribbon, normally "now" from the clock. When null it ends at the latest segment.</summary>
    public DateTimeOffset? EndTime
    {
        get => (DateTimeOffset?)GetValue(EndTimeProperty);
        set => SetValue(EndTimeProperty, value);
    }

    protected override Thickness PlotMargin => new(0);

    protected override ChartRanges GetDataRanges()
    {
        var end = EndTime?.UtcTicks ?? (Segments is { Count: > 0 } segments ? segments.Max(segment => segment.End).UtcTicks : DateTimeOffset.UnixEpoch.UtcTicks + Window.Ticks);

        return new ChartRanges(end - Window.Ticks, end, 0, 1);
    }

    protected override IReadOnlyList<IChartLayer> CreateLayers() => [new RibbonLayer { Segments = Segments }];
}
