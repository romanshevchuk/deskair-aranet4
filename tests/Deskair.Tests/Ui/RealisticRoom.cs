using Deskair.Domain.Readings;

namespace Deskair.Tests.Ui;

/// <summary>
/// A home office over many days, for store screenshots. Unlike the simulated sensor, no two days are alike: weekdays and weekends differ,
/// each day has its own peak, lunch break and window-opening dips, and the air decays slowly overnight. Deterministic, so a render can be repeated.
/// </summary>
public sealed class RealisticRoom
{
    private const double Baseline = 440;

    private readonly Dictionary<DateOnly, DayPlan> plans = [];

    public Reading ReadingAt(DateTimeOffset time)
    {
        var plan = PlanOf(DateOnly.FromDateTime(time.UtcDateTime));
        var previous = PlanOf(DateOnly.FromDateTime(time.UtcDateTime.AddDays(-1)));
        var hour = time.UtcDateTime.TimeOfDay.TotalHours;

        var occupancy = Math.Min(1, Envelope(plan, hour) + Evening(previous, hour + 24));
        var windows = 1 - (0.6 * plan.WindowDips.Sum(dip => Dip(dip, hour)));
        var co2 = Baseline + ((plan.Peak - Baseline) * occupancy * Math.Max(0.2, windows)) + Noise(time, 14);

        var sun = plan.Sunny * Math.Max(0, Math.Sin((hour - 9) / 9 * Math.PI));
        var temperature = plan.Temperature + (2.6 * occupancy) + sun + (0.12 * Noise(time, 1));
        var humidity = plan.Humidity - (1.4 * (temperature - plan.Temperature)) + (0.5 * Noise(time, 1));
        var pressure = 1009 + (6 * Math.Sin(time.ToUnixTimeSeconds() / 270000.0)) + (2.5 * Math.Sin(time.ToUnixTimeSeconds() / 110000.0)) + (0.08 * Noise(time, 1));

        return new Reading(time, Math.Round(co2), Math.Round(temperature, 1), Math.Round(humidity), Math.Round(pressure, 1));
    }

    /// <summary>The air filling up while someone works, then easing off when they stop.</summary>
    private static double Envelope(DayPlan plan, double hour)
    {
        if (hour < plan.Start)
        {
            return 0;
        }

        var working = plan.LunchBreak is { } lunch && hour >= lunch && hour < lunch + 1 ? 0.55 : 1;

        if (hour <= plan.End)
        {
            return (1 - Math.Exp(-(hour - plan.Start) / 1.4)) * working;
        }

        return (1 - Math.Exp(-(plan.End - plan.Start) / 1.4)) * Math.Exp(-(hour - plan.End) / 2.6);
    }

    /// <summary>What is left of the previous evening's air.</summary>
    private static double Evening(DayPlan previous, double hourAfterPreviousMidnight)
    {
        return hourAfterPreviousMidnight <= previous.End
            ? 0
            : (1 - Math.Exp(-(previous.End - previous.Start) / 1.4)) * Math.Exp(-(hourAfterPreviousMidnight - previous.End) / 2.6);
    }

    /// <summary>A window opened for about half an hour: 0 to 1, with soft edges.</summary>
    private static double Dip((double At, double Length) dip, double hour)
    {
        var inside = hour - dip.At;

        if (inside < 0 || inside > dip.Length + 0.5)
        {
            return 0;
        }

        // Air leaves a room over a quarter of an hour, and comes back slower once the window is shut.
        return Math.Min(1, Math.Min(inside / 0.3, (dip.Length + 0.5 - inside) / 0.5));
    }

    private static double Noise(DateTimeOffset time, double size)
    {
        var seconds = time.ToUnixTimeSeconds();

        return size * (0.55 * Math.Sin(seconds / 1100.0) + 0.3 * Math.Sin(seconds / 370.0 + 1.3) + 0.15 * Math.Sin(seconds / 97.0 + 2.1));
    }

    private DayPlan PlanOf(DateOnly day)
    {
        if (plans.TryGetValue(day, out var cached))
        {
            return cached;
        }

        var random = new Random((day.DayNumber * 7919) + 17);
        var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var start = weekend ? 10.5 + (random.NextDouble() * 1.5) : 8 + (random.NextDouble() * 1.5);
        var end = weekend ? 19 + (random.NextDouble() * 2) : 17 + (random.NextDouble() * 2);
        var peak = weekend ? 750 + (random.NextDouble() * 450) : 950 + (random.NextDouble() * 750);
        var windows = new List<(double At, double Length)>();
        var count = random.Next(weekend ? 0 : 1, 4);

        for (var index = 0; index < count; index++)
        {
            windows.Add((start + 2 + (random.NextDouble() * (end - start - 3)), 0.3 + (random.NextDouble() * 0.3)));
        }

        var plan = new DayPlan(
            start,
            end,
            peak,
            weekend ? null : 12 + (random.NextDouble() * 1.2),
            windows,
            18.4 + (random.NextDouble() * 3),
            random.NextDouble() < 0.3 ? 2 + (random.NextDouble() * 2) : 0.4,
            41 + (random.NextDouble() * 10));

        plans[day] = plan;

        return plan;
    }

    private sealed record DayPlan(
        double Start,
        double End,
        double Peak,
        double? LunchBreak,
        IReadOnlyList<(double At, double Length)> WindowDips,
        double Temperature,
        double Sunny,
        double Humidity);
}
