using Deskair.App.Localization;
using Deskair.Domain.Analytics.Live;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Live;

/// <summary>Turns the last 24 h of readings into the three texts under the hero card. The calculations are in Domain; this only formats them.</summary>
public sealed class HeroStatsBuilder
{
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;
    private readonly LocalCalendar calendar;

    public HeroStatsBuilder(ILocalizer localizer, MetricFormatter formatter, LocalCalendar calendar)
    {
        this.localizer = localizer;
        this.formatter = formatter;
        this.calendar = calendar;
    }

    public IReadOnlyList<StatItem> Build(IReadOnlyList<Reading> readings, DateTimeOffset now)
    {
        if (readings.Count == 0)
        {
            return [];
        }

        return [GoodShare(readings), Peak(readings), InZone(readings, now)];
    }

    private StatItem GoodShare(IReadOnlyList<Reading> readings)
    {
        var share = LiveStatsCalculator.GoodShare(readings) ?? 0;

        return new StatItem(NumberFormat.Percent(share, localizer.Culture), localizer.Get("Stat_GoodShare"));
    }

    private StatItem Peak(IReadOnlyList<Reading> readings)
    {
        var (ppm, at) = LiveStatsCalculator.Peak(readings)!.Value;
        var value = $"{formatter.Value(Metric.Co2, ppm)} {formatter.Unit(Metric.Co2)}";
        var label = at == readings[^1].Timestamp
            ? localizer.Get("Stat_PeakNow")
            : localizer.Format("Stat_Peak", ("time", TimeZoneInfo.ConvertTime(at, calendar.Zone).ToString("HH:mm", localizer.Culture)));

        return new StatItem(value, label);
    }

    private StatItem InZone(IReadOnlyList<Reading> readings, DateTimeOffset now)
    {
        var duration = LiveStatsCalculator.TimeInCurrentZone(readings, now) ?? TimeSpan.Zero;
        var zone = ZoneClassifier.Classify(readings[^1].Co2Ppm);
        var minutes = (int)Math.Floor(duration.TotalMinutes);
        var text = minutes < 60
            ? localizer.Format("Duration_Minutes", ("min", minutes))
            : localizer.Format("Duration_HoursMinutes", ("h", minutes / 60), ("min", (minutes % 60).ToString("00", localizer.Culture)));

        return new StatItem(text, localizer.Get($"Stat_InZone_{zone}"));
    }
}
