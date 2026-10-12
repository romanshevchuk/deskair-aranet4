using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Ribbon;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
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

        Unit = formatter.Unit(metric);
        Window = length;
        EndTime = timeProvider.GetUtcNow();
        IdealMin = descriptor.IdealMin is { } min ? formatter.ToDisplay(metric, min) : null;
        IdealMax = descriptor.IdealMax is { } max ? formatter.ToDisplay(metric, max) : null;
        IdealLabel = IdealMin is { } low && IdealMax is { } high
            ? localizer.Format("Chart_Ideal", ("min", Math.Round(low)), ("max", Math.Round(high)))
            : null;

        if (devices.Selected is not { } device)
        {
            Points = [];
            SummaryText = null;
            RibbonSegments = [];
            return;
        }

        var day = await query.GetRecentAsync(device, TimeSpan.FromHours(24), cancellationToken);
        var recent = length == TimeSpan.FromHours(24) ? day : await query.GetRecentAsync(device, length, cancellationToken);

        Points = ToPoints(recent, metric, device);
        SummaryText = Summary(recent, metric, range);
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
    {
        if (recent.Count == 0)
        {
            return null;
        }

        var values = recent.Select(reading => reading.ValueOf(metric)).ToList();

        return localizer.Format(
            $"Chart_Summary_{range}",
            ("low", formatter.Value(metric, values.Min())),
            ("avg", formatter.Value(metric, values.Average())),
            ("high", formatter.Value(metric, values.Max())),
            ("unit", formatter.Unit(metric)));
    }
}
