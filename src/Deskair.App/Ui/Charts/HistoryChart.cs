using System.Globalization;
using System.Windows;
using Deskair.App.Localization;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The History chart. With <see cref="Bars"/> (Week, Month, Year) it draws floating min to max bars with an average tick, weekend shading, dashed partial bars and hatched gaps.
/// Without bars it draws the hourly line of <see cref="Points"/> (Day). CO₂ gets the zone bands and lines like the Live chart. Values are already in the unit shown.
/// </summary>
public sealed class HistoryChart : ChartBase
{
    private const double Co2Minimum = 400;
    private const double Co2Maximum = 2000;
    private const double ElevatedFrom = 1000;
    private const double HighAbove = 1400;
    private const int MaximumDayBars = 14;
    private const double AverageRowHeight = 20;
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

    public static readonly DependencyProperty PartialTextProperty = DependencyProperty.Register(
        nameof(PartialText),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AverageValueProperty = DependencyProperty.Register(
        nameof(AverageValue),
        typeof(double?),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AverageLabelProperty = DependencyProperty.Register(
        nameof(AverageLabel),
        typeof(string),
        typeof(HistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TodayLabelProperty = DependencyProperty.Register(
        nameof(TodayLabel),
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

    /// <summary>The typical day (the 7 days before) as points in the middle of each hour of the window, in the unit shown. Drawn as a quiet line behind the readings (Day, CO₂).</summary>
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

    /// <summary>The average of the whole period, drawn as a dashed line with its label. Null draws none.</summary>
    public double? AverageValue
    {
        get => (double?)GetValue(AverageValueProperty);
        set => SetValue(AverageValueProperty, value);
    }

    /// <summary>The word before the average ("avg") on the line and in the row under the bars. Already localized.</summary>
    public string? AverageLabel
    {
        get => (string?)GetValue(AverageLabelProperty);
        set => SetValue(AverageLabelProperty, value);
    }

    /// <summary>The label of the last day bar ("Today"). Already localized.</summary>
    public string? TodayLabel
    {
        get => (string?)GetValue(TodayLabelProperty);
        set => SetValue(TodayLabelProperty, value);
    }

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

    /// <summary>The hover text of a partial bar, with {recorded} and {expected} (hours): "only {recorded} of {expected} h recorded". Already localized.</summary>
    public string? PartialText
    {
        get => (string?)GetValue(PartialTextProperty);
        set => SetValue(PartialTextProperty, value);
    }

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    private bool BarMode => Bars is not null;

    private bool HasData => BarMode
        ? Bars!.Any(bar => bar.State != BucketState.NoData && bar.Maximum is not null)
        : Points?.Any(point => point.Value is not null) == true;

    private string ValueFormat => NumberFormat.FormatFor(Metric);

    /// <summary>
    /// True when every bar stands for one day and there are few enough to name them (a week): the labels are weekdays with "Today", and a row of averages sits under them.
    /// Longer ranges keep the date labels along the axis.
    /// </summary>
    private bool DayBars => BarMode && Bars!.Count is > 0 and <= MaximumDayBars && Bars.All(IsOneDay);

    private static bool IsOneDay(ChartBar bar) => bar.End - bar.Start <= TimeSpan.FromHours(25);

    protected override Thickness PlotMargin
    {
        get
        {
            var margin = base.PlotMargin;

            // The word "Night" sits on the band above the plot.
            if (!BarMode && NightSpans is { Count: > 0 })
            {
                margin = new Thickness(margin.Left, Math.Max(margin.Top, NightBandRoom), margin.Right, margin.Bottom);
            }

            return DayBars && !string.IsNullOrEmpty(AverageLabel) ? new Thickness(margin.Left, margin.Top, margin.Right, margin.Bottom + AverageRowHeight) : margin;
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
        var ticks = Metric == Metric.Co2 ? Co2Ticks(high) : ChartTicks.Nice(low, high, 5);
        var dayCo2 = !BarMode && Metric == Metric.Co2;
        var axis = new AxisLabelLayer
        {
            Culture = CultureInfo.CurrentUICulture,
            TimeZone = TimeZone,
            YTicks = ticks,
            YFormat = ValueFormat,
            TickBrushKeys = dayCo2 ? limitBrushes : null,
            DateLabels = TimeSpan.FromTicks((long)(end - start)) > clockLabelLimit
        };

        if (BarMode)
        {
            layers.Add(new WeekendShadingLayer { WeekendSpans = WeekendSpans });
        }
        else
        {
            layers.Add(new NightBandLayer { Spans = NightSpans, Label = NightLabel });
            layers.Add(new ComfortBandLayer { Band = ComfortBand, BrushKey = $"Metric.{Metric}.Brush" });
            layers.Add(new HighlightLayer { Highlight = Highlight, BrushKey = $"Metric.{Metric}.Brush" });
        }

        if (Metric == Metric.Co2 && BarMode)
        {
            layers.Add(new BandLayer());
        }

        // The Day line shows the two limits as dashed lines in their zone colours, so the grid leaves those values out.
        layers.Add(new GridLayer { YTicks = dayCo2 ? [.. ticks.Where(tick => tick is not ElevatedFrom and not HighAbove)] : ticks });

        if (dayCo2)
        {
            layers.Add(new ThresholdLayer { ElevatedFrom = ElevatedFrom, HighAbove = HighAbove });
            layers.Add(new LineLayer { Points = TypicalPoints, BrushKey = "Text.Tertiary.Brush", Opacity = TypicalOpacity, Smooth = true });
            layers.Add(new AreaLayer { Points = Points, BrushKey = "Text.Primary.Brush", TopOpacity = AreaOpacity, Smooth = true });
        }

        if (BarMode)
        {
            var zoneColoured = Metric == Metric.Co2;

            layers.Add(new RangeBarLayer { Bars = Bars, BrushKey = $"Metric.{Metric}.Brush", ZoneColoured = zoneColoured });
            layers.Add(new AverageLineLayer { Bars = Bars, BrushKey = $"Metric.{Metric}.Brush", ZoneColoured = zoneColoured });
        }
        else if (Metric == Metric.Co2)
        {
            layers.Add(new ZoneLineLayer { Points = Points, Smooth = true });
        }
        else
        {
            layers.Add(new LineLayer { Points = TypicalPoints, BrushKey = "Text.Tertiary.Brush", Opacity = TypicalOpacity, Smooth = true });
            layers.Add(new LineLayer { Points = Points, BrushKey = $"Metric.{Metric}.Brush" });
        }

        layers.Add(new GapHatchLayer { Gaps = Gaps });

        if (dayCo2)
        {
            layers.Add(new MarkerLayer { Markers = Markers, Label = MarkerLabel });
        }

        if (HasData)
        {
            if (BarMode)
            {
                layers.Add(new OverallAverageLayer { Value = AverageValue, Label = AverageLabel, Format = ValueFormat, Culture = CultureInfo.CurrentUICulture });
            }
        }
        else
        {
            layers.Add(new NoDataLayer { Text = NoDataText });
        }

        layers.Add(new CrosshairLayer { Hover = Hover });
        layers.Add(axis);

        if (DayBars)
        {
            axis.TimeLabels = false;
            layers.Add(new BarLabelLayer
            {
                Bars = Bars,
                Culture = CultureInfo.CurrentUICulture,
                TimeZone = TimeZone,
                TodayLabel = TodayLabel,
                RowLabel = AverageLabel,
                Format = ValueFormat
            });
        }

        return layers;
    }

    protected override ChartHover? HitTest(Point position, ChartArea area)
    {
        return BarMode ? BarHover(position, area) : PointHover(position, area);
    }

    private ChartHover? PointHover(Point position, ChartArea area)
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

    private ChartHover? BarHover(Point position, ChartArea area)
    {
        if (!area.ContainsX(position.X))
        {
            return null;
        }

        var time = area.TimeAt(position.X);
        var bar = Bars!.FirstOrDefault(candidate => candidate.Start <= time && time < candidate.End);

        if (bar is null)
        {
            return null;
        }

        var middle = bar.Start + ((bar.End - bar.Start) / 2);

        if (bar.State == BucketState.NoData || bar.Average is not { } average)
        {
            return new ChartHover(middle, null, area.X(middle), null, NoDataText);
        }

        var culture = CultureInfo.CurrentUICulture;
        var date = TimeZoneInfo.ConvertTime(bar.Start, TimeZone).ToString("d MMM", culture);
        var range = $"{bar.Minimum?.ToString(ValueFormat, culture)}–{bar.Maximum?.ToString(ValueFormat, culture)}";
        var text = $"{date} · {average.ToString(ValueFormat, culture)} {Unit} ({range})";

        if (bar.State == BucketState.Partial && PartialText is { } partial)
        {
            text += $" · {partial.Replace("{recorded}", bar.RecordedHours.ToString("0.#", culture), StringComparison.Ordinal).Replace("{expected}", bar.ExpectedHours.ToString("0.#", culture), StringComparison.Ordinal)}";
        }

        return new ChartHover(middle, average, area.X(middle), area.Y(average), text);
    }

    private (double Start, double End) TimeRange()
    {
        if (BarMode && Bars!.Count > 0)
        {
            return (Bars.Min(bar => bar.Start.UtcTicks), Bars.Max(bar => bar.End.UtcTicks));
        }

        var times = Points?.Select(point => point.Time.UtcTicks).ToList() ?? [];

        return times.Count > 0
            ? (times.Min(), times.Max())
            : (DateTimeOffset.UnixEpoch.UtcTicks, DateTimeOffset.UnixEpoch.UtcTicks + TimeSpan.TicksPerDay);
    }

    /// <summary>The y labels of the CO₂ axis: the bottom, the two limits and the top (and 2000 when the top is 2600).</summary>
    private IReadOnlyList<double> Co2Ticks(double high)
        => !BarMode && high > Co2Maximum ? [Co2Minimum, ElevatedFrom, HighAbove, Co2Maximum, high] : [Co2Minimum, ElevatedFrom, HighAbove, high];

    /// <summary>The top of the Day CO₂ axis: 1600, 2000 or 2600 by how high the readings and the typical line go.</summary>
    private (double Low, double High) DayCo2Range()
    {
        var highest = (Points ?? []).Concat(TypicalPoints ?? []).Where(point => point.Value is not null).Select(point => point.Value!.Value).DefaultIfEmpty(0).Max();

        return (Co2Minimum, highest > 2600 ? Math.Ceiling(highest / 100) * 100 : highest > 2000 ? 2600 : highest > 1600 ? 2000 : 1600);
    }

    private (double Low, double High) ValueRange()
    {
        var values = BarMode
            ? Bars!.Where(bar => bar.State != BucketState.NoData).SelectMany(bar => new[] { bar.Minimum, bar.Maximum }).Where(value => value is not null).Select(value => value!.Value).ToList()
            : (Points ?? []).Concat(TypicalPoints ?? []).Where(point => point.Value is not null).Select(point => point.Value!.Value).ToList();

        if (Metric == Metric.Co2)
        {
            if (!BarMode)
            {
                return DayCo2Range();
            }

            var top = values.Count > 0 ? Math.Ceiling(values.Max() / 100) * 100 : Co2Maximum;

            return (Co2Minimum, Math.Max(Co2Maximum, top));
        }

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
