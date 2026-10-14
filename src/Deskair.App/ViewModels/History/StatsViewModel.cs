using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.Coverage;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// The numbers of the stats card for the period shown: average, highest and lowest (weighted by reading count, from buckets with data), how much of the period the readings cover,
/// the change against the period before, the best and worst period, the time in each zone and an insight about the worst hours.
/// With a formatter, a localizer and a calendar the values are also given as text in the unit shown (°C or °F for temperature); without them only the numbers are set.
/// </summary>
public sealed partial class StatsViewModel : ObservableObject
{
    private const string NoChangeText = "–";

    private readonly MetricFormatter? formatter;
    private readonly ILocalizer? localizer;
    private readonly LocalCalendar? calendar;

    [ObservableProperty]
    private double? average;

    [ObservableProperty]
    private double? highest;

    [ObservableProperty]
    private double? lowest;

    /// <summary>Readings present divided by expected, from 0 to 1.</summary>
    [ObservableProperty]
    private double coverage;

    /// <summary>True when the coverage chip should show: under 98 % of the expected readings, or a bucket with no data (behaviour-rules.md "Aggregation" rule 5).</summary>
    [ObservableProperty]
    private bool showCoverage;

    [ObservableProperty]
    private bool hasData;

    [ObservableProperty]
    private string? averageText;

    /// <summary>The average without its unit ("1,013"), for the large number at the top of the card.</summary>
    [ObservableProperty]
    private string? averageNumber;

    /// <summary>"Average · 2 Oct – 8 Oct 2026", or "Night average · ..." with the night filter. Null without data buckets.</summary>
    [ObservableProperty]
    private string? headerText;

    /// <summary>The zone of the average while there is an insight, so its box takes that zone's advice colours. Null otherwise.</summary>
    [ObservableProperty]
    private Zone? insightZone;

    [ObservableProperty]
    private string? highestText;

    [ObservableProperty]
    private string? lowestText;

    /// <summary>The unit of the metric shown ("ppm", "°C"), for the chart readout.</summary>
    [ObservableProperty]
    private string? unitText;

    /// <summary>True when the change line shows: there is an average. Without an earlier period it says there is nothing to compare.</summary>
    [ObservableProperty]
    private bool hasChange;

    /// <summary>"▼ 120 ppm", "▲ 2.0 °C", "±0 ppm" or "–" when there is no earlier period.</summary>
    [ObservableProperty]
    private string? changeText;

    /// <summary>"vs previous week", or "no earlier data to compare".</summary>
    [ObservableProperty]
    private string? changeCaption;

    /// <summary>Good when CO₂ fell, High when it rose; null for no change and for the other metrics, which have no better or worse.</summary>
    [ObservableProperty]
    private Zone? changeZone;

    /// <summary>True when the best and worst period show: at least two complete periods with different averages, and a range longer than a day.</summary>
    [ObservableProperty]
    private bool hasPeriods;

    /// <summary>"Best day" for CO₂, "Lowest day" for the other metrics.</summary>
    [ObservableProperty]
    private string? bestLabel;

    [ObservableProperty]
    private string? bestText;

    /// <summary>When the best period was ("2 Oct", "1 Oct – 7 Oct", "October 2026").</summary>
    [ObservableProperty]
    private string? bestWhen;

    [ObservableProperty]
    private string? worstLabel;

    [ObservableProperty]
    private string? worstText;

    [ObservableProperty]
    private string? worstWhen;

    /// <summary>True for CO₂ when there are readings: the shares below make a bar.</summary>
    [ObservableProperty]
    private bool hasZoneShare;

    [ObservableProperty]
    private double zoneGood;

    [ObservableProperty]
    private double zoneElevated;

    [ObservableProperty]
    private double zoneHigh;

    [ObservableProperty]
    private string? zoneGoodText;

    [ObservableProperty]
    private string? zoneElevatedText;

    [ObservableProperty]
    private string? zoneHighText;

    /// <summary>One sentence about the worst three hours of the typical day (CO₂ only), or null.</summary>
    [ObservableProperty]
    private string? insightText;

    public StatsViewModel()
    {
    }

    public StatsViewModel(MetricFormatter formatter, ILocalizer localizer, LocalCalendar calendar)
    {
        this.formatter = formatter;
        this.localizer = localizer;
        this.calendar = calendar;
    }

    public void Update(IReadOnlyList<Bucket> buckets, Metric metric = Metric.Co2)
    {
        Update(new StatsInput(buckets) { Metric = metric });
    }

    public void Update(StatsInput input)
    {
        UpdateNumbers(input.Buckets, input.Metric);
        UpdateHeader(input);
        UpdateChange(input);
        UpdatePeriods(input);
        UpdateZoneShare(input);
        UpdateInsight(input);
    }

    private void UpdateNumbers(IReadOnlyList<Bucket> buckets, Metric metric)
    {
        var withData = buckets.Where(BucketStatistics.HasData).ToList();
        var readings = withData.Sum(bucket => bucket.ReadingCount);

        Average = BucketStatistics.WeightedAverage(buckets);
        Highest = withData.Count == 0 ? null : withData.Max(bucket => bucket.Maximum);
        Lowest = withData.Count == 0 ? null : withData.Min(bucket => bucket.Minimum);
        Coverage = CoverageCalculator.Fraction(readings, buckets.Sum(bucket => bucket.ExpectedCount));
        HasData = withData.Count > 0;
        ShowCoverage = buckets.Count > 0 && CoverageCalculator.ShouldShowChip(Coverage, buckets.Count(bucket => bucket.State == BucketState.NoData));
        AverageText = Text(Average, metric);
        AverageNumber = formatter is not null && Average is { } stored ? formatter.Value(metric, stored) : null;
        HighestText = Text(Highest, metric);
        LowestText = Text(Lowest, metric);
        UnitText = formatter?.Unit(metric);
    }

    private void UpdateHeader(StatsInput input)
    {
        HeaderText = null;

        if (localizer is null || calendar is null || input.Buckets.Count == 0)
        {
            return;
        }

        var culture = localizer.Culture;
        var first = calendar.LocalDateOf(input.Buckets[0].Start);
        var last = calendar.LocalDateOf(input.Buckets[^1].End.AddTicks(-1));
        var range = first == last
            ? first.ToString("d MMM yyyy", culture)
            : $"{first.ToString("d MMM", culture)} – {last.ToString("d MMM yyyy", culture)}";

        HeaderText = localizer.Format(input.Hours == HoursFilterMode.Night ? "Stats_Header_NightAverage" : "Stats_Header_Average", ("range", range));
    }

    private void UpdateChange(StatsInput input)
    {
        HasChange = false;
        ChangeText = null;
        ChangeCaption = null;
        ChangeZone = null;

        if (formatter is null || localizer is null || Average is not { } current)
        {
            return;
        }

        HasChange = true;

        if (input.Previous is not { Count: > 0 } || BucketStatistics.WeightedAverage(input.Previous) is not { } before)
        {
            ChangeText = NoChangeText;
            ChangeCaption = localizer.Get("Stats_Change_None");
            return;
        }

        var difference = current - before;
        var size = formatter.Difference(input.Metric, difference);
        var unit = formatter.Unit(input.Metric);
        var unchanged = size == formatter.Difference(input.Metric, 0);

        ChangeText = unchanged ? $"±{size} {unit}" : $"{(difference < 0 ? '▼' : '▲')} {size} {unit}";
        ChangeCaption = localizer.Get($"Stats_Change_{input.Range}");
        ChangeZone = unchanged || input.Metric != Metric.Co2 ? null : difference < 0 ? Zone.Good : Zone.High;
    }

    private void UpdatePeriods(StatsInput input)
    {
        HasPeriods = false;
        BestLabel = BestText = BestWhen = null;
        WorstLabel = WorstText = WorstWhen = null;

        if (formatter is null || localizer is null || calendar is null || input.Range == Period.Day)
        {
            return;
        }

        if (BucketStatistics.Lowest(input.Buckets) is not { } best
            || BucketStatistics.Highest(input.Buckets) is not { } worst
            || best.Start == worst.Start)
        {
            return;
        }

        var co2 = input.Metric == Metric.Co2;
        var period = localizer.Get($"Stats_Period_{input.Grouping}");

        BestLabel = localizer.Format(co2 ? "Stats_BestPeriod" : "Stats_LowestPeriod", ("period", period));
        BestText = Text(best.Average, input.Metric);
        BestWhen = When(best, input.Grouping);
        WorstLabel = localizer.Format(co2 ? "Stats_WorstPeriod" : "Stats_HighestPeriod", ("period", period));
        WorstText = Text(worst.Average, input.Metric);
        WorstWhen = When(worst, input.Grouping);
        HasPeriods = true;
    }

    private void UpdateZoneShare(StatsInput input)
    {
        if (localizer is null || input.Metric != Metric.Co2 || input.ZoneShare is not { } share)
        {
            HasZoneShare = false;
            ZoneGood = ZoneElevated = ZoneHigh = 0;
            ZoneGoodText = ZoneElevatedText = ZoneHighText = null;
            return;
        }

        ZoneGood = share.Good;
        ZoneElevated = share.Elevated;
        ZoneHigh = share.High;
        ZoneGoodText = NumberFormat.Percent(share.Good, localizer.Culture);
        ZoneElevatedText = NumberFormat.Percent(share.Elevated, localizer.Culture);
        ZoneHighText = NumberFormat.Percent(share.High, localizer.Culture);
        HasZoneShare = true;
    }

    private void UpdateInsight(StatsInput input)
    {
        InsightText = null;
        InsightZone = null;

        if (localizer is null || input.Metric != Metric.Co2 || input.TypicalDay is null || TypicalDayCalculator.WorstWindow(input.TypicalDay) is not { } window)
        {
            return;
        }

        var key = window.Average >= ZoneThresholds.Default.ElevatedFromPpm ? "Insight_WorstHours" : "Insight_AllGood";

        InsightText = localizer.Format(
            key,
            ("from", Hour(window.StartHour)),
            ("to", Hour(window.StartHour + 3)),
            ("value", Text(window.Average, Metric.Co2)));
        InsightZone = Average is { } average ? ZoneClassifier.Classify(average) : null;
    }

    private string? Text(double? value, Metric metric)
        => formatter is not null && value is { } stored ? $"{formatter.Value(metric, stored)} {formatter.Unit(metric)}" : null;

    private string When(Bucket bucket, Period grouping)
    {
        var culture = localizer!.Culture;
        var first = calendar!.LocalDateOf(bucket.Start);
        var last = calendar.LocalDateOf(bucket.End.AddTicks(-1));

        return grouping switch
        {
            Period.Week when last != first => $"{first.ToString("d MMM", culture)} – {last.ToString("d MMM", culture)}",
            Period.Month => first.ToString("MMMM yyyy", culture),
            _ => first.ToString("d MMM", culture)
        };
    }

    private static string Hour(int hour) => $"{hour % 24:00}:00";
}
