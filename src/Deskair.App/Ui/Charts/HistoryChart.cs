using System.Globalization;
using System.Windows;
using Deskair.App.Localization;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The History line chart: the readings of <see cref="Points"/> (Day), or the Pressure line for a longer range. CO₂ gets the limit lines and the zone-coloured line like the
/// Live chart; the other metrics a line in their own colour. Behind the line: the typical day, the night band, the comfort range and the biggest pressure change.
/// Values are already in the unit shown. The days × hours grids are drawn by <see cref="HourGridChart"/>.
/// </summary>
public sealed class HistoryChart : ChartBase
{
    private const double Co2Minimum = 400;
    private const double Co2Maximum = 2000;
    private const double ElevatedFrom = 1000;
    private const double HighAbove = 1400;
    private const double NightBandRoom = 22;
    private const double TypicalOpacity = 0.6;
    private const double AreaOpacity = 0.06;
    private const double MarkerHitRadius = 9;
    private static readonly Dictionary<double, string> limitBrushes = new() { [ElevatedFrom] = "Zone.Elevated.PillText.Brush", [HighAbove] = "Zone.High.PillText.Brush" };
    private static readonly TimeSpan clockLabelLimit = TimeSpan.FromHours(36);

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GapsProperty = DependencyProperty.Register(
        nameof(Gaps),
        typeof(IReadOnlyList<ChartGap>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MetricProperty = DependencyProperty.Register(
        nameof(Metric),
        typeof(Metric),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(Metric.Co2, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NoDataTextProperty = DependencyProperty.Register(
        nameof(NoDataText),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TypicalPointsProperty = DependencyProperty.Register(
        nameof(TypicalPoints),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NightSpansProperty = DependencyProperty.Register(
        nameof(NightSpans),
        typeof(IReadOnlyList<ChartGap>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ComfortBandProperty = DependencyProperty.Register(
        nameof(ComfortBand),
        typeof(ChartBand),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register(
        nameof(Highlight),
        typeof(ChartHighlight),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MarkersProperty = DependencyProperty.Register(
        nameof(Markers),
        typeof(IReadOnlyList<ChartMarker>),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NightLabelProperty = DependencyProperty.Register(
        nameof(NightLabel),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MarkerLabelProperty = DependencyProperty.Register(
        nameof(MarkerLabel),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TypicalHoverTextProperty = DependencyProperty.Register(
        nameof(TypicalHoverText),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The typical day (the 7 days before) as points in the middle of each hour of the window, in the unit shown. Drawn as a quiet line behind the readings (Day).</summary>
    public IReadOnlyList<ChartPoint>? TypicalPoints
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(TypicalPointsProperty);
        set => SetValue(TypicalPointsProperty, value);
    }

    /// <summary>The night hours inside the window, drawn as a band behind the line (Day).</summary>
    public IReadOnlyList<ChartGap>? NightSpans
    {
        get => (IReadOnlyList<ChartGap>?)GetValue(NightSpansProperty);
        set => SetValue(NightSpansProperty, value);
    }

    /// <summary>The comfortable range as a soft band behind the Day line (Temperature, Humidity), in the unit shown. Null for none.</summary>
    public ChartBand? ComfortBand
    {
        get => (ChartBand?)GetValue(ComfortBandProperty);
        set => SetValue(ComfortBandProperty, value);
    }

    /// <summary>A stretch of time shaded behind the line with a label: the biggest 24 hour change of the Pressure line. Null for none.</summary>
    public ChartHighlight? Highlight
    {
        get => (ChartHighlight?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>The "air improved" rings with their hover texts (Day, CO₂).</summary>
    public IReadOnlyList<ChartMarker>? Markers
    {
        get => (IReadOnlyList<ChartMarker>?)GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>The word on the night band ("Night"). Already localized.</summary>
    public string? NightLabel
    {
        get => (string?)GetValue(NightLabelProperty);
        set => SetValue(NightLabelProperty, value);
    }

    /// <summary>The word above each marker ("air improved"). Already localized.</summary>
    public string? MarkerLabel
    {
        get => (string?)GetValue(MarkerLabelProperty);
        set => SetValue(MarkerLabelProperty, value);
    }

    /// <summary>The part added to the hover readout when the typical value is known, with {value}: "typical 1234". Already localized.</summary>
    public string? TypicalHoverText
    {
        get => (string?)GetValue(TypicalHoverTextProperty);
        set => SetValue(TypicalHoverTextProperty, value);
    }

    public IReadOnlyList<ChartPoint>? Points
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IReadOnlyList<ChartGap>? Gaps
    {
        get => (IReadOnlyList<ChartGap>?)GetValue(GapsProperty);
        set => SetValue(GapsProperty, value);
    }

    public Metric Metric
    {
        get => (Metric)GetValue(MetricProperty);
        set => SetValue(MetricProperty, value);
    }

    /// <summary>The unit text in the hover readout ("ppm", "°C").</summary>
    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>Shown in the middle of the plot with no data, and in the hover readout over a gap. Already localized.</summary>
    public string? NoDataText
    {
        get => (string?)GetValue(NoDataTextProperty);
        set => SetValue(NoDataTextProperty, value);
    }

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    private bool HasData => Points?.Any(point => point.Value is not null) == true;

    private string ValueFormat => NumberFormat.FormatFor(Metric);

    protected override Thickness PlotMargin
    {
        get
        {
            var margin = base.PlotMargin;

            // The word "Night" sits on the band above the plot.
            return NightSpans is { Count: > 0 } ? new Thickness(margin.Left, Math.Max(margin.Top, NightBandRoom), margin.Right, margin.Bottom) : margin;
        }
    }

    protected override ChartRanges GetDataRanges()
    {
        var (start, end) = TimeRange();
        var (low, high) = ValueRange();

        return new ChartRanges(start, end, low, high);
    }

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        var layers = new List<IChartLayer>();
        var (low, high) = ValueRange();
        var (start, end) = TimeRange();
        var co2 = Metric == Metric.Co2;
        var ticks = co2 ? Co2Ticks(high) : ChartTicks.Nice(low, high, 5);

        layers.Add(new NightBandLayer { Spans = NightSpans, Label = NightLabel });
        layers.Add(new ComfortBandLayer { Band = ComfortBand, BrushKey = $"Metric.{Metric}.Brush" });
        layers.Add(new HighlightLayer { Highlight = Highlight, BrushKey = $"Metric.{Metric}.Brush" });

        // The CO₂ line shows the two limits as dashed lines in their zone colours, so the grid leaves those values out.
        layers.Add(new GridLayer { YTicks = co2 ? [.. ticks.Where(tick => tick is not ElevatedFrom and not HighAbove)] : ticks });

        if (co2)
        {
            layers.Add(new ThresholdLayer { ElevatedFrom = ElevatedFrom, HighAbove = HighAbove });
        }

        layers.Add(new LineLayer { Points = TypicalPoints, BrushKey = "Text.Tertiary.Brush", Opacity = TypicalOpacity, Smooth = true });

        if (co2)
        {
            layers.Add(new AreaLayer { Points = Points, BrushKey = "Text.Primary.Brush", TopOpacity = AreaOpacity, Smooth = true });
            layers.Add(new ZoneLineLayer { Points = Points, Smooth = true });
        }
        else
        {
            layers.Add(new LineLayer { Points = Points, BrushKey = $"Metric.{Metric}.Brush", Smooth = true });
        }

        layers.Add(new GapHatchLayer { Gaps = Gaps });

        if (co2)
        {
            layers.Add(new MarkerLayer { Markers = Markers, Label = MarkerLabel });
        }

        if (!HasData)
        {
            layers.Add(new NoDataLayer { Text = NoDataText });
        }

        layers.Add(new CrosshairLayer { Hover = Hover });
        layers.Add(new AxisLabelLayer
        {
            Culture = CultureInfo.CurrentUICulture,
            TimeZone = TimeZone,
            YTicks = ticks,
            YFormat = ValueFormat,
            TickBrushKeys = co2 ? limitBrushes : null,
            DateLabels = TimeSpan.FromTicks((long)(end - start)) > clockLabelLimit
        });

        return layers;
    }

    protected override ChartHover? HitTest(Point position, ChartArea area)
    {
        if (MarkerHover(position, area) is { } marker)
        {
            return marker;
        }

        var hover = ChartHitTest.NearestPoint(Points, area, position.X);

        if (hover is null)
        {
            return null;
        }

        var local = TimeZoneInfo.ConvertTime(hover.Time, TimeZone).ToString("HH:mm", CultureInfo.InvariantCulture);
        var text = hover.Value is { } value
            ? $"{value.ToString(ValueFormat, CultureInfo.CurrentUICulture)} {Unit} · {local}".Replace("  ", " ", StringComparison.Ordinal)
            : NoDataText;

        if (hover.Value is not null && TypicalAt(hover.Time) is { } typical && !string.IsNullOrEmpty(TypicalHoverText))
        {
            text += " · " + TypicalHoverText.Replace("{value}", typical.ToString(ValueFormat, CultureInfo.CurrentUICulture), StringComparison.Ordinal);
        }

        return hover with { Text = text };
    }

    /// <summary>The marker under the pointer, with its own text; null when the pointer is not near a ring.</summary>
    private ChartHover? MarkerHover(Point position, ChartArea area)
    {
        foreach (var marker in Markers ?? [])
        {
            var at = new Point(area.X(marker.Time), area.Y(marker.Value));

            if (Math.Abs(at.X - position.X) <= MarkerHitRadius && Math.Abs(at.Y - position.Y) <= MarkerHitRadius)
            {
                return new ChartHover(marker.Time, marker.Value, at.X, at.Y, marker.Text);
            }
        }

        return null;
    }

    /// <summary>The typical value for the hour that holds a time: the nearest typical point, if it is within the hour.</summary>
    private double? TypicalAt(DateTimeOffset time)
    {
        var nearest = (TypicalPoints ?? []).Where(point => point.Value is not null).OrderBy(point => Math.Abs((point.Time - time).Ticks)).FirstOrDefault();

        return nearest is not null && Math.Abs((nearest.Time - time).TotalMinutes) <= 30 ? nearest.Value : null;
    }

    private (double Start, double End) TimeRange()
    {
        var times = Points?.Select(point => point.Time.UtcTicks).ToList() ?? [];

        return times.Count > 0
            ? (times.Min(), times.Max())
            : (DateTimeOffset.UnixEpoch.UtcTicks, DateTimeOffset.UnixEpoch.UtcTicks + TimeSpan.TicksPerDay);
    }

    /// <summary>The y labels of the CO₂ axis: the bottom, the two limits and the top (and 2000 when the top is 2600).</summary>
    private static double[] Co2Ticks(double high)
        => high > Co2Maximum ? [Co2Minimum, ElevatedFrom, HighAbove, Co2Maximum, high] : [Co2Minimum, ElevatedFrom, HighAbove, high];

    /// <summary>The top of the CO₂ axis: 1600, 2000 or 2600 by how high the readings and the typical line go.</summary>
    private (double Low, double High) Co2Range()
    {
        var highest = (Points ?? []).Concat(TypicalPoints ?? []).Where(point => point.Value is not null).Select(point => point.Value!.Value).DefaultIfEmpty(0).Max();

        return (Co2Minimum, highest > 2600 ? Math.Ceiling(highest / 100) * 100 : highest > 2000 ? 2600 : highest > 1600 ? 2000 : 1600);
    }

    private (double Low, double High) ValueRange()
    {
        if (Metric == Metric.Co2)
        {
            return Co2Range();
        }

        var values = (Points ?? []).Concat(TypicalPoints ?? []).Where(point => point.Value is not null).Select(point => point.Value!.Value).ToList();

        if (values.Count == 0)
        {
            return (0, 1);
        }

        var low = values.Min();
        var high = values.Max();
        var padding = Math.Max((high - low) * 0.1, Metric switch
        {
            Metric.Temperature => 0.5,
            Metric.Humidity => 2,
            _ => 1
        });

        return (low - padding, high + padding);
    }
}
