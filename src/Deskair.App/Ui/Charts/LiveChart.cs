using System.Globalization;
using System.Windows;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The Live chart. CO₂ gets zone bands, dashed zone lines and a zone-coloured line; the other metrics get a plot fill, the ideal range and a
/// plain metric line. Values in <see cref="Points"/> are already in the unit shown (the view model converts °C to °F).
/// </summary>
public sealed class LiveChart : ChartBase
{
    private const double Co2Minimum = 400;
    private const double Co2Maximum = 2000;
    private const double ElevatedFrom = 1000;
    private const double HighAbove = 1400;

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

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit),
        typeof(string),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IdealMinProperty = DependencyProperty.Register(
        nameof(IdealMin),
        typeof(double?),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IdealMaxProperty = DependencyProperty.Register(
        nameof(IdealMax),
        typeof(double?),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IdealLabelProperty = DependencyProperty.Register(
        nameof(IdealLabel),
        typeof(string),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NoDataTextProperty = DependencyProperty.Register(
        nameof(NoDataText),
        typeof(string),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NowLabelProperty = DependencyProperty.Register(
        nameof(NowLabel),
        typeof(string),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(
        nameof(Window),
        typeof(TimeSpan?),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register(
        nameof(EndTime),
        typeof(DateTimeOffset?),
        typeof(LiveChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

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

    /// <summary>The unit text in the hover readout ("ppm", "°C").</summary>
    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public double? IdealMin
    {
        get => (double?)GetValue(IdealMinProperty);
        set => SetValue(IdealMinProperty, value);
    }

    public double? IdealMax
    {
        get => (double?)GetValue(IdealMaxProperty);
        set => SetValue(IdealMaxProperty, value);
    }

    public string? IdealLabel
    {
        get => (string?)GetValue(IdealLabelProperty);
        set => SetValue(IdealLabelProperty, value);
    }

    /// <summary>Shown in the middle of the plot with no data, and in the hover readout over a gap. Already localized.</summary>
    public string? NoDataText
    {
        get => (string?)GetValue(NoDataTextProperty);
        set => SetValue(NoDataTextProperty, value);
    }

    public string? NowLabel
    {
        get => (string?)GetValue(NowLabelProperty);
        set => SetValue(NowLabelProperty, value);
    }

    /// <summary>The length of time shown, ending at the latest point. When null the chart shows exactly the time the points cover.</summary>
    public TimeSpan? Window
    {
        get => (TimeSpan?)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    /// <summary>The right end of the time axis, normally "now" from the clock. When null the chart ends at the latest point.</summary>
    public DateTimeOffset? EndTime
    {
        get => (DateTimeOffset?)GetValue(EndTimeProperty);
        set => SetValue(EndTimeProperty, value);
    }

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    private bool HasData => Points?.Any(point => point.Value is not null) == true;

    private string ValueFormat => Metric is Metric.Temperature or Metric.Pressure ? "N1" : "N0";

    protected override ChartRanges GetDataRanges()
    {
        var times = Points?.Select(point => point.Time.UtcTicks).ToList() ?? [];
        var now = EndTime?.UtcTicks ?? (times.Count > 0 ? times.Max() : DateTimeOffset.UnixEpoch.UtcTicks + TimeSpan.TicksPerHour);
        var start = Window is { } window ? now - window.Ticks : times.Count > 0 ? times.Min() : now - TimeSpan.TicksPerHour;
        var (low, high) = ValueRange();

        return new ChartRanges(start, now, low, high);
    }

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        var culture = CultureInfo.CurrentUICulture;
        var layers = new List<IChartLayer>();
        var axis = new AxisLabelLayer { Culture = culture, TimeZone = TimeZone, NowLabel = NowLabel, YFormat = ValueFormat };
        var crosshair = new CrosshairLayer { Hover = Hover };

        if (Metric == Metric.Co2)
        {
            var ticks = new[] { Co2Minimum, ElevatedFrom, HighAbove, ValueRange().High };

            axis.YTicks = ticks;
            layers.Add(new BandLayer());
            layers.Add(new GridLayer { YTicks = ticks });
            layers.Add(new DashedZoneLinesLayer());
            layers.Add(new ZoneLineLayer { Points = Points });
        }
        else
        {
            var (low, high) = ValueRange();
            var ticks = ChartTicks.Nice(low, high, 5);
            var name = Metric.ToString();

            axis.YTicks = ticks;
            layers.Add(new IdealBandLayer
            {
                IdealMin = IdealMin,
                IdealMax = IdealMax,
                Label = IdealLabel,
                FillBrushKey = $"Metric.{name}.Fill.Brush",
                IdealBrushKey = $"Metric.{name}.Ideal.Brush"
            });
            layers.Add(new GridLayer { YTicks = ticks });
            layers.Add(new LineLayer { Points = Points, BrushKey = $"Metric.{name}.Brush", ShowEndMarker = true });
        }

        if (!HasData)
        {
            layers.Add(new NoDataLayer { Text = NoDataText });
        }

        layers.Add(crosshair);
        layers.Add(axis);

        return layers;
    }

    protected override ChartHover? HitTest(Point position, ChartArea area)
    {
        var hover = ChartHitTest.NearestPoint(Points, area, position.X);

        if (hover is null)
        {
            return null;
        }

        var local = TimeZoneInfo.ConvertTime(hover.Time, TimeZone).ToString("HH:mm", CultureInfo.InvariantCulture);
        var text = hover.Value is { } value
            ? $"{value.ToString(ValueFormat, CultureInfo.CurrentUICulture)} {Unit} · {local}".Replace("  ", " ", StringComparison.Ordinal)
            : NoDataText;

        return hover with { Text = text };
    }

    private (double Low, double High) ValueRange()
    {
        var values = Points?.Where(point => point.Value is not null).Select(point => point.Value!.Value).ToList() ?? [];

        if (Metric == Metric.Co2)
        {
            var top = values.Count > 0 ? Math.Ceiling(values.Max() / 100) * 100 : Co2Maximum;

            return (Co2Minimum, Math.Max(Co2Maximum, top));
        }

        var low = values.Count > 0 ? values.Min() : (IdealMin ?? 0);
        var high = values.Count > 0 ? values.Max() : (IdealMax ?? 1);

        low = Math.Min(low, IdealMin ?? low);
        high = Math.Max(high, IdealMax ?? high);

        var padding = Math.Max((high - low) * 0.1, MinimumPadding());

        return (low - padding, high + padding);
    }

    private double MinimumPadding() => Metric switch
    {
        Metric.Temperature => 0.5,
        Metric.Humidity => 2,
        _ => 1
    };
}
