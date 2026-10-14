using Deskair.Domain.Metrics;
using Deskair.Services.Settings;

namespace Deskair.App.Localization;

/// <summary>Formats metric values and units for the UI: the selected language for numbers, the selected unit for temperature. Stored values are always in °C.</summary>
public sealed class MetricFormatter
{
    private readonly ILocalizer localizer;
    private readonly SettingsService settings;

    public MetricFormatter(ILocalizer localizer, SettingsService settings)
    {
        this.localizer = localizer;
        this.settings = settings;
    }

    /// <summary>The number to show for a stored value (temperature converted to the selected unit).</summary>
    public double ToDisplay(Metric metric, double stored)
        => metric == Metric.Temperature ? settings.Current.TemperatureUnit.FromCelsius(stored) : stored;

    public string Value(Metric metric, double stored)
        => NumberFormat.Value(metric, ToDisplay(metric, stored), localizer.Culture);

    /// <summary>The size of a difference between two stored values, without a sign, in the selected unit. A temperature difference scales but has no offset (2 °C is 3.6 °F).</summary>
    public string Difference(Metric metric, double storedDifference)
        => NumberFormat.Value(metric, Math.Abs(ToDisplay(metric, storedDifference) - ToDisplay(metric, 0)), localizer.Culture);

    /// <summary>A share from 0 to 1 as a whole percent in the selected language ("59%", "59 %").</summary>
    public string Percent(double fraction)
        => NumberFormat.Percent(fraction, localizer.Culture);

    public string Unit(Metric metric)
        => metric == Metric.Temperature ? settings.Current.TemperatureUnit.Symbol() : MetricDescriptor.For(metric).Unit;
}
