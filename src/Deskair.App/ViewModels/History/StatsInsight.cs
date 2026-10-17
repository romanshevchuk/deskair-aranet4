using Deskair.App.Localization;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

/// <summary>The sentence under the stats card: when the air is worst (CO₂), or when it leaves the comfortable range (Temperature, Humidity), from the typical day. Also the text helpers the card shares.</summary>
public sealed class StatsInsight
{
    private readonly MetricFormatter formatter;
    private readonly ILocalizer localizer;

    public StatsInsight(MetricFormatter formatter, ILocalizer localizer)
    {
        this.formatter = formatter;
        this.localizer = localizer;
    }

    /// <summary>True when at least 2 of the 3 worst hours are night hours (behaviour-rules.md "Aggregation" rule 9).</summary>
    public static bool WhileSleeping(NightWindow? night, int startHour)
        => night is { IsEmpty: false } && Enumerable.Range(startHour, 3).Count(hour => night.Includes(TimeSpan.FromHours(hour % 24))) >= 2;

    /// <summary>The insight for the input and its zone colour, or null when the typical day says nothing. <paramref name="average"/> is the average of the period, which colours the CO₂ insight.</summary>
    public (string Text, Zone? Zone)? Build(StatsInput input, double? average)
    {
        if (input.TypicalDay is null)
        {
            return null;
        }

        if (input.Metric == Metric.Co2)
        {
            return Co2(input, average);
        }

        return ComfortScale.For(input.Metric) is { } scale ? Comfort(input, scale) : null;
    }

    /// <summary>"20–24 °C" or "30–50 %": the comfortable range in the unit shown.</summary>
    public string RangeText(ComfortScale scale)
    {
        var unit = formatter.Unit(scale.Metric);

        return $"{formatter.Value(scale.Metric, scale.Low)}–{formatter.Value(scale.Metric, scale.High)} {unit}";
    }

    public string? ValueText(double? value, Metric metric)
        => value is { } stored ? $"{formatter.Value(metric, stored)} {formatter.Unit(metric)}" : null;

    private static string Hour(int hour) => $"{hour % 24:00}:00";

    private (string Text, Zone? Zone)? Comfort(StatsInput input, ComfortScale scale)
    {
        var typical = input.TypicalDay!;

        if (typical.All(hour => hour.Average is null))
        {
            return null;
        }

        if (TypicalDayCalculator.FurthestOutsideWindow(typical, scale) is not { } window)
        {
            return (localizer.Format("Insight_Comfortable", ("range", RangeText(scale))), Zone.Good);
        }

        var name = (input.Metric, scale.Side(window.Average)) switch
        {
            (Metric.Temperature, ComfortSide.Low) => "Coolest",
            (Metric.Temperature, _) => "Warmest",
            (_, ComfortSide.Low) => "Driest",
            _ => "MostHumid"
        };
        var night = WhileSleeping(input.Night, window.StartHour) ? "_Night" : string.Empty;
        var text = localizer.Format(
            $"Insight_{name}{night}",
            ("from", Hour(window.StartHour)),
            ("to", Hour(window.StartHour + 3)),
            ("value", ValueText(window.Average, input.Metric)));

        return (text, Zone.Elevated);
    }

    private (string Text, Zone? Zone)? Co2(StatsInput input, double? average)
    {
        if (TypicalDayCalculator.WorstWindow(input.TypicalDay!) is not { } window)
        {
            return null;
        }

        var key = window.Average < ZoneThresholds.Default.ElevatedFromPpm ? "Insight_AllGood" : WhileSleeping(input.Night, window.StartHour) ? "Insight_WorstHours_Night" : "Insight_WorstHours";
        var text = localizer.Format(
            key,
            ("from", Hour(window.StartHour)),
            ("to", Hour(window.StartHour + 3)),
            ("value", ValueText(window.Average, Metric.Co2)));

        return (text, average is { } value ? ZoneClassifier.Classify(value) : null);
    }
}
