namespace Deskair.Domain.Metrics;

/// <summary>
/// Everything a view needs to know about a metric, so views have no per-metric switch.
/// <see cref="NameKey"/> is a resource key; <see cref="ColorToken"/> is the token name prefix
/// (for example "Metric.Temperature").
/// </summary>
public sealed record MetricDescriptor(
    Metric Metric,
    string NameKey,
    string Unit,
    double? IdealMin,
    double? IdealMax,
    string ColorToken)
{
    private static readonly MetricDescriptor[] all =
    [
        new(Metric.Co2, "Metric_Co2", "ppm", null, null, "Metric.Co2"),
        new(Metric.Temperature, "Metric_Temperature", "°C", 20, 24, "Metric.Temperature"),
        new(Metric.Humidity, "Metric_Humidity", "%", 30, 50, "Metric.Humidity"),
        new(Metric.Pressure, "Metric_Pressure", "hPa", null, null, "Metric.Pressure")
    ];

    public static IReadOnlyList<MetricDescriptor> All => all;

    public static MetricDescriptor For(Metric metric)
    {
        return all.First(descriptor => descriptor.Metric == metric);
    }
}
