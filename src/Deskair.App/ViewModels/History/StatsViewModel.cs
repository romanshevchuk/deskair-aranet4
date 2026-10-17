using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.Comfort;
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
    private readonly StatsInsight? insight;
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

    /// <summary>When the highest reading was taken ("Tue 13 Oct, 14:16"). Null without a time.</summary>
    [ObservableProperty]
    private string? highestWhen;

    [ObservableProperty]
    private string? lowestWhen;

    /// <summary>"Based on 82% of expected readings" as one line of text; null when the coverage is not worth a note (98% or more, behaviour-rules.md "Aggregation" rule 5).</summary>
    [ObservableProperty]
    private string? coverageText;

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

    /// <summary>For Temperature and Humidity: "Comfortable", "A bit cool", "Too warm" and so on for the average, shown as a pill beside it. Null for the other metrics.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasComfort))]
    private string? comfortText;

    /// <summary>Good while the average is comfortable, Elevated outside the range. Null without a comfort text.</summary>
    [ObservableProperty]
    private Zone? comfortZone;

    /// <summary>"Time in each zone" for CO₂, "Time in comfort range" for Temperature and Humidity.</summary>
    [ObservableProperty]
    private string? shareTitle;

    /// <summary>The comfortable range ("20–24 °C") next to the title. Null for CO₂.</summary>
    [ObservableProperty]
    private string? shareRange;

    /// <summary>The names under the bar, left to right: Good, Elevated, High; or Cool, Comfortable, Warm; or Dry, Comfortable, Humid.</summary>
    [ObservableProperty]
    private string? shareFirstLabel;

    [ObservableProperty]
    private string? shareSecondLabel;

    [ObservableProperty]
    private string? shareThirdLabel;

    /// <summary>The colours of the share bar: the zones, or a comfort scale.</summary>
    [ObservableProperty]
    private HourGridPalette sharePalette;

    /// <summary>One sentence about the worst three hours of the typical day (CO₂ only), or null.</summary>
    [ObservableProperty]
    private string? insightText;

    /// <summary>For Pressure: the biggest change over 24 hours as "▼ 3.8 hPa". Null without one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBiggestChange))]
    private string? biggestChangeText;

    /// <summary>"from Tue 6 Oct, 18:00": where the biggest change started.</summary>
    [ObservableProperty]
    private string? biggestChangeWhen;

    /// <summary>For Pressure: highest minus lowest of the period ("8.0 hPa").</summary>
    [ObservableProperty]
    private string? pressureRangeText;

    public bool HasComfort => ComfortText is not null;

    public bool HasBiggestChange => BiggestChangeText is not null;

    public StatsViewModel()
    {
    }

    public StatsViewModel(MetricFormatter formatter, ILocalizer localizer, LocalCalendar calendar)
    {
        this.formatter = formatter;
        this.localizer = localizer;
        insight = new StatsInsight(formatter, localizer);
        this.calendar = calendar;
    }

    public void Update(IReadOnlyList<Bucket> buckets, Metric metric = Metric.Co2)
    {
        Update(new StatsInput(buckets) { Metric = metric });
    }

    public void Update(StatsInput input)
    {
        UpdateNumbers(input.Buckets, input.Metric);
        UpdateExtremeTimes(input.Buckets);
        UpdateHeader(input);
        UpdateChange(input);
        UpdateComfort(input);
        UpdateZoneShare(input);
        UpdateInsight(input);
        UpdatePressure(input);
    }

    /// <summary>Pressure follows the weather: the biggest 24 hour change, the range of the period and one line saying so (behaviour-rules.md rule 12).</summary>
    private void UpdatePressure(StatsInput input)
    {
        BiggestChangeText = BiggestChangeWhen = PressureRangeText = null;

        if (formatter is null || localizer is null || input.Metric != Metric.Pressure || !HasData)
        {
            return;
        }

        if (Highest is { } high && Lowest is { } low)
        {
            PressureRangeText = $"{formatter.Difference(Metric.Pressure, high - low)} {formatter.Unit(Metric.Pressure)}";
        }

        InsightText = localizer.Get("Insight_Pressure");

        if (input.PressureChange is { } change)
        {
            BiggestChangeText = $"{(change.Delta < 0 ? '▼' : '▲')} {formatter.Difference(Metric.Pressure, Math.Abs(change.Delta))} {formatter.Unit(Metric.Pressure)}";
            BiggestChangeWhen = ReadingTime(change.Start) is { } when ? localizer.Format("Stats_BiggestChange_From", ("when", when)) : null;
        }
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
        CoverageText = ShowCoverage && localizer is not null ? localizer.Format("Coverage_Chip", ("percent", NumberFormat.Percent(Coverage, localizer.Culture))) : null;
        AverageText = Text(Average, metric);
        AverageNumber = formatter is not null && Average is { } stored ? formatter.Value(metric, stored) : null;
        HighestText = Text(Highest, metric);
        LowestText = Text(Lowest, metric);
        UnitText = formatter?.Unit(metric);
    }

    /// <summary>When the highest and the lowest reading were taken, from the buckets that hold them, in the day-and-time form ("Tue 13 Oct, 14:16").</summary>
    private void UpdateExtremeTimes(IReadOnlyList<Bucket> buckets)
    {
        var withData = buckets.Where(BucketStatistics.HasData).ToList();

        HighestWhen = withData.Count == 0 ? null : ReadingTime(withData.MaxBy(bucket => bucket.Maximum)?.MaximumAt);
        LowestWhen = withData.Count == 0 ? null : ReadingTime(withData.MinBy(bucket => bucket.Minimum)?.MinimumAt);
    }

    private string? ReadingTime(DateTimeOffset? time)
        => time is { } at && localizer is not null && calendar is not null
            ? TimeZoneInfo.ConvertTime(at, calendar.Zone).ToString("ddd d MMM, HH:mm", localizer.Culture)
            : null;

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
            : $"{first.ToString(first.Year == last.Year ? "d MMM" : "d MMM yyyy", culture)} – {last.ToString("d MMM yyyy", culture)}";

        HeaderText = localizer.Format("Stats_Header_Average", ("range", range));
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

    /// <summary>
    /// The three shares behind the bar: for CO₂ the zones (Good, Elevated, High); for Temperature and Humidity below, inside and above the comfortable range, left to right
    /// (the properties keep their zone names).
    /// </summary>
    private void UpdateZoneShare(StatsInput input)
    {
        HasZoneShare = false;
        ZoneGood = ZoneElevated = ZoneHigh = 0;
        ZoneGoodText = ZoneElevatedText = ZoneHighText = null;
        ShareTitle = ShareRange = ShareFirstLabel = ShareSecondLabel = ShareThirdLabel = null;
        SharePalette = HourGridPalette.Zones;

        if (localizer is null)
        {
            return;
        }

        if (input.Metric == Metric.Co2 && input.ZoneShare is { } zones)
        {
            ShowShare(zones.Good, zones.Elevated, zones.High);
            ShareTitle = localizer.Get("Stats_ZoneShare");
            ShareFirstLabel = localizer.Get("Zone_Good");
            ShareSecondLabel = localizer.Get("Zone_Elevated");
            ShareThirdLabel = localizer.Get("Zone_High");
        }
        else if (ComfortScale.For(input.Metric) is { } scale && input.Comfort is { } comfort)
        {
            var temperature = input.Metric == Metric.Temperature;

            ShowShare(comfort.Low, comfort.Inside, comfort.High);
            ShareTitle = localizer.Get("Stats_ComfortShare");
            ShareRange = insight?.RangeText(scale);
            ShareFirstLabel = localizer.Get(temperature ? "Comfort_Cool" : "Comfort_Dry");
            ShareSecondLabel = localizer.Get("Comfort_Comfortable");
            ShareThirdLabel = localizer.Get(temperature ? "Comfort_Warm" : "Comfort_Humid");
            SharePalette = temperature ? HourGridPalette.Temperature : HourGridPalette.Humidity;
        }
    }

    private void ShowShare(double first, double second, double third)
    {
        ZoneGood = first;
        ZoneElevated = second;
        ZoneHigh = third;
        ZoneGoodText = NumberFormat.Percent(first, localizer!.Culture);
        ZoneElevatedText = NumberFormat.Percent(second, localizer.Culture);
        ZoneHighText = NumberFormat.Percent(third, localizer.Culture);
        HasZoneShare = true;
    }

    /// <summary>The pill beside the average for Temperature and Humidity: comfortable, or a bit or too far on one side.</summary>
    private void UpdateComfort(StatsInput input)
    {
        ComfortText = null;
        ComfortZone = null;

        if (localizer is null || ComfortScale.For(input.Metric) is not { } scale || Average is not { } average)
        {
            return;
        }

        var side = scale.Side(average);

        if (side == ComfortSide.Inside)
        {
            ComfortText = localizer.Get("Comfort_Status_Comfortable");
            ComfortZone = Zone.Good;
            return;
        }

        var far = scale.IsFarOutside(average) ? "Too" : "ABit";
        var word = (input.Metric, side) switch
        {
            (Metric.Temperature, ComfortSide.Low) => "Cool",
            (Metric.Temperature, _) => "Warm",
            (_, ComfortSide.Low) => "Dry",
            _ => "Humid"
        };

        ComfortText = localizer.Get($"Comfort_Status_{far}{word}");
        ComfortZone = Zone.Elevated;
    }

    private void UpdateInsight(StatsInput input)
    {
        var insight = this.insight?.Build(input, Average);
        InsightText = insight?.Text;
        InsightZone = insight?.Zone;
    }

    private string? Text(double? value, Metric metric) => insight?.ValueText(value, metric);
}
