using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Ribbon;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Devices;
using Deskair.Services.Queries;

namespace Deskair.App.ViewModels.Live;

public sealed partial class ChartCardViewModel : ObservableObject
{
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly MetricFormatter formatter;
    private readonly ILocalizer localizer;
    private readonly TimeProvider timeProvider;

    /// <summary>The points in the unit shown (temperature converted to the selected unit). A null value is a gap.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    [ObservableProperty]
    private string? unit;

    [ObservableProperty]
    private double? idealMin;

    [ObservableProperty]
    private double? idealMax;

    [ObservableProperty]
    private string? idealLabel;

    /// <summary>The chart title: the name of the metric it shows ("CO₂", "Temperature"), now that there are no metric tabs (POL-06).</summary>
    [ObservableProperty]
    private string? title;

    /// <summary>The span the chart shows, beside the title ("last hour"): the summary line no longer repeats it.</summary>
    [ObservableProperty]
    private string? subtitle;

    /// <summary>What a screen reader says for the chart: the metric, the span and the low, average and high.</summary>
    [ObservableProperty]
    private string? accessibleName;

    /// <summary>The labels on the CO₂ threshold lines ("Elevated from 1000", "High from 1400"); null for the other metrics.</summary>
    [ObservableProperty]
    private string? elevatedLineLabel;

    [ObservableProperty]
    private string? highLineLabel;

    /// <summary>"Last hour · Low … · Avg … · High … ppm". Null without readings.</summary>
    [ObservableProperty]
    private string? summaryText;

    /// <summary>The last 24 hours in zone colours, 144 slots of ten minutes. A missing slot is a gap.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartZoneSegment> ribbonSegments = [];

    [ObservableProperty]
    private TimeSpan window = TimeSpan.FromHours(1);

    [ObservableProperty]
    private DateTimeOffset? endTime;

    public ChartCardViewModel(
        ReadingQueryService query,
        DeviceRegistry devices,
        MetricFormatter formatter,
        ILocalizer localizer,
        TimeProvider timeProvider)
    {
        this.query = query;
        this.devices = devices;
        this.formatter = formatter;
        this.localizer = localizer;
        this.timeProvider = timeProvider;
    }

    public async Task RefreshAsync(Metric metric, LiveRange range, CancellationToken cancellationToken)
    {
        var length = WindowOf(range);
        var descriptor = MetricDescriptor.For(metric);

        Title = localizer.Get(descriptor.NameKey);
        Subtitle = localizer.Get($"Chart_Subtitle_{range}");
        Unit = formatter.Unit(metric);
        Window = length;
        EndTime = timeProvider.GetUtcNow();
        IdealMin = descriptor.IdealMin is { } min ? formatter.ToDisplay(metric, min) : null;
        IdealMax = descriptor.IdealMax is { } max ? formatter.ToDisplay(metric, max) : null;
        IdealLabel = IdealMin is { } low && IdealMax is { } high
            ? localizer.Format("Chart_Ideal", ("min", Math.Round(low)), ("max", Math.Round(high)))
            : null;
        ElevatedLineLabel = metric == Metric.Co2
            ? localizer.Format("Chart_Threshold_Elevated", ("ppm", formatter.Value(metric, ZoneThresholds.Default.ElevatedFromPpm)))
            : null;
        HighLineLabel = metric == Metric.Co2
            ? localizer.Format("Chart_Threshold_High", ("ppm", formatter.Value(metric, ZoneThresholds.Default.HighAbovePpm)))
            : null;

        if (devices.Selected is not { } device)
        {
            Points = [];
            SummaryText = null;
            AccessibleName = null;
            RibbonSegments = [];
            return;
        }

        var day = await query.GetRecentAsync(device, TimeSpan.FromHours(24), cancellationToken);
        var recent = length == TimeSpan.FromHours(24) ? day : await query.GetRecentAsync(device, length, cancellationToken);

        Points = ToPoints(recent, metric, device);
        SummaryText = Summary(recent, metric, range);
        AccessibleName = AccessibleNameOf(recent, metric);
        RibbonSegments = RibbonCalculator.Build(day, EndTime ?? timeProvider.GetUtcNow())
            .Select(span => new ChartZoneSegment(span.Start, span.End, span.Zone))
            .ToList();
    }

    private static TimeSpan WindowOf(LiveRange range)
    {
        return range switch
        {
            LiveRange.OneHour => TimeSpan.FromHours(1),
            LiveRange.SixHours => TimeSpan.FromHours(6),
            _ => TimeSpan.FromHours(24)
        };
    }

    private List<ChartPoint> ToPoints(IReadOnlyList<Reading> recent, Metric metric, DeviceId device)
    {
        var result = new List<ChartPoint>(recent.Count);

        for (var index = 0; index < recent.Count; index++)
        {
            var reading = recent[index];

            if (index > 0)
            {
                var previous = recent[index - 1].Timestamp;
                var interval = devices.IntervalFor(device, reading.Timestamp);

                if (reading.Timestamp - previous > interval * 1.5)
                {
                    result.Add(new ChartPoint(previous + interval, null));
                }
            }

            result.Add(new ChartPoint(reading.Timestamp, formatter.ToDisplay(metric, reading.ValueOf(metric))));
        }

        return result;
    }

    private string? Summary(IReadOnlyList<Reading> recent, Metric metric, LiveRange range)
        => Describe($"Chart_Summary_{range}", recent, metric);

    /// <summary>The same figures as the summary line, in a sentence for a screen reader ("CO₂, last hour: low 1076, average 1105, high 1171 ppm").</summary>
    private string SpokenTitle(Metric metric) => metric == Metric.Co2 ? localizer.Get("Metric_Co2_Spoken") : Title ?? string.Empty;

    private string AccessibleNameOf(IReadOnlyList<Reading> recent, Metric metric)
        => Describe("Chart_Live_AccessibleName", recent, metric, ("metric", SpokenTitle(metric)), ("span", Subtitle ?? string.Empty))
           ?? localizer.Format("Chart_Live_AccessibleName_Empty", ("metric", SpokenTitle(metric)), ("span", Subtitle ?? string.Empty));

    private string? Describe(string key, IReadOnlyList<Reading> recent, Metric metric, params (string Name, object Value)[] extra)
    {
        if (recent.Count == 0)
        {
            return null;
        }

        var values = recent.Select(reading => reading.ValueOf(metric)).ToList();

        return localizer.Format(
            key,
            [
                ("low", formatter.Value(metric, values.Min())),
                ("avg", formatter.Value(metric, values.Average())),
                ("high", formatter.Value(metric, values.Max())),
                ("unit", formatter.Unit(metric)),
                .. extra
            ]);
    }
}
