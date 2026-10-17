using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The value range a sparkline draws (design-tokens.md "Sparklines"): at least a minimum span per metric, centred on the data, so a steady reading looks steady
/// instead of drawing sensor noise as spikes. Values are the stored ones, so the temperature span is in °C whatever unit the card shows.
/// </summary>
public static class SparklineRange
{
    /// <summary>The smallest span the line may be stretched over: 1 °C, 6 % RH, 2 hPa. CO₂ has none.</summary>
    public static double MinimumSpan(Metric metric) => metric switch
    {
        Metric.Temperature => 1,
        Metric.Humidity => 6,
        Metric.Pressure => 2,
        _ => 0
    };

    /// <summary>The range to draw for the lowest and highest value: the data's own range, or the minimum span centred on it when the data is narrower.</summary>
    public static (double Low, double High) Fit(Metric metric, double low, double high)
    {
        var span = MinimumSpan(metric);

        if (high - low >= span)
        {
            return (low, high);
        }

        var middle = (low + high) / 2;

        return (middle - (span / 2), middle + (span / 2));
    }
}
