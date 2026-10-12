using Deskair.Domain.Metrics;

namespace Deskair.Domain.Readings;

public static class ReadingExtensions
{
    public static double ValueOf(this Reading reading, Metric metric) => metric switch
    {
        Metric.Co2 => reading.Co2Ppm,
        Metric.Temperature => reading.TemperatureC,
        Metric.Humidity => reading.HumidityPct,
        Metric.Pressure => reading.PressureHpa,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };
}
