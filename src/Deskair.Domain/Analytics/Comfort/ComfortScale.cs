using Deskair.Domain.Metrics;

namespace Deskair.Domain.Analytics.Comfort;

/// <summary>
/// The comfortable range of a metric and the four limits that split the History grid into five steps (behaviour-rules.md "Aggregation" rule 10).
/// Values are in the stored unit (°C, %). A value on a limit belongs to the step above it, so 20.0 °C is comfortable and 24.0 °C is warm.
/// Only Temperature and Humidity have a scale; the limits are fixed in v1.
/// </summary>
public sealed record ComfortScale
{
    public static ComfortScale Temperature { get; } = new(Metric.Temperature, 20, 24, [18, 20, 24, 26], 2);

    public static ComfortScale Humidity { get; } = new(Metric.Humidity, 30, 50, [25, 30, 50, 60], 5);

    private ComfortScale(Metric metric, double low, double high, double[] steps, double farOutside)
    {
        FarOutside = farOutside;
        Metric = metric;
        Low = low;
        High = high;
        Steps = steps;
    }

    public Metric Metric { get; }

    /// <summary>The lowest comfortable value.</summary>
    public double Low { get; }

    /// <summary>The highest comfortable value: the range is <see cref="Low"/> to this, both included.</summary>
    public double High { get; }

    /// <summary>An average this far or further outside the range is "too cool", "too warm", "too dry" or "too humid" rather than "a bit".</summary>
    public double FarOutside { get; }

    /// <summary>The four limits between the five steps, lowest first; the middle two are <see cref="Low"/> and <see cref="High"/>.</summary>
    public IReadOnlyList<double> Steps { get; }

    /// <summary>The scale of the metric, or null when the metric has none.</summary>
    public static ComfortScale? For(Metric metric) => metric switch
    {
        Metric.Temperature => Temperature,
        Metric.Humidity => Humidity,
        _ => null
    };

    /// <summary>The step of a value, from 0 (furthest below) to 4 (furthest above); 2 is comfortable. A value exactly on a limit is in the step nearer the comfortable range.</summary>
    public int Step(double value)
    {
        var step = 0;

        while (step < Steps.Count && (Steps[step] <= Low ? value >= Steps[step] : value > Steps[step]))
        {
            step++;
        }

        return step;
    }

    public ComfortSide Side(double value) => value < Low ? ComfortSide.Low : value > High ? ComfortSide.High : ComfortSide.Inside;

    /// <summary>How far a value is outside the comfortable range; 0 inside it.</summary>
    public double Outside(double value) => value < Low ? Low - value : value > High ? value - High : 0;

    public bool IsFarOutside(double value) => Outside(value) > FarOutside;

    /// <summary>How the values spread over the three sides. Null when there are none.</summary>
    public ComfortShare? Share(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        int low = 0, inside = 0, high = 0;

        foreach (var value in values.Where(value => !double.IsNaN(value)))
        {
            switch (Side(value))
            {
                case ComfortSide.Low:
                    low++;
                    break;
                case ComfortSide.Inside:
                    inside++;
                    break;
                default:
                    high++;
                    break;
            }
        }

        var total = low + inside + high;

        return total == 0 ? null : new ComfortShare((double)low / total, (double)inside / total, (double)high / total);
    }
}
