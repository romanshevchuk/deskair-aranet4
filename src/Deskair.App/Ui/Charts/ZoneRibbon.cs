using System.Windows;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>The 24 h ribbon of zone-coloured segments. It draws at the size it is given (the flyout uses a 22 px tall one), never a scaled-down copy.</summary>
public sealed class ZoneRibbon : ChartBase
{
    private const double LabelRoom = 20;
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

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption),
        typeof(string),
        typeof(ZoneRibbon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Set to label the ribbon the way the Live chart does: this caption ("24 h") at the left and the hours 00, 06, 12 and 18 under it. The ribbon then lines up with the chart above it.</summary>
    public string? Caption
    {
        get => (string?)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    private bool Labelled => !string.IsNullOrEmpty(Caption);

    protected override Thickness PlotMargin
    {
        get
        {
            if (!Labelled)
            {
                return new Thickness(0);
            }

            var chart = base.PlotMargin;

            return new Thickness(chart.Left, 0, chart.Right, LabelRoom);
        }
    }

    protected override ChartRanges GetDataRanges()
    {
        var end = EndTime?.UtcTicks ?? (Segments is { Count: > 0 } segments ? segments.Max(segment => segment.End).UtcTicks : DateTimeOffset.UnixEpoch.UtcTicks + Window.Ticks);

        return new ChartRanges(end - Window.Ticks, end, 0, 1);
    }

    protected override IReadOnlyList<IChartLayer> CreateLayers()
        => Labelled ? [new RibbonLayer { Segments = Segments }, new RibbonLabelLayer { Caption = Caption, TimeZone = TimeZone }] : [new RibbonLayer { Segments = Segments }];
}
