using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Localization;

public class MetricFormatterTests
{
    private static async Task<MetricFormatter> CreateAsync(string culture, TemperatureUnit unit)
    {
        var localizer = new Localizer();
        localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
        var settings = new SettingsService(new FakeSettingsStore(), new StrongReferenceMessenger());
        await settings.UpdateAsync(current => current with { TemperatureUnit = unit }, TestContext.Current.CancellationToken);

        return new MetricFormatter(localizer, settings);
    }

    [Theory]
    [InlineData(Metric.Co2, 1068.4, "1,068")]
    [InlineData(Metric.Temperature, 22.34, "22.3")]
    [InlineData(Metric.Humidity, 44.4, "44")]
    [InlineData(Metric.Pressure, 975.26, "975.3")]
    public async Task Values_use_the_metric_precision_in_english(Metric metric, double value, string expected)
    {
        var formatter = await CreateAsync("en-US", TemperatureUnit.Celsius);

        Assert.Equal(expected, formatter.Value(metric, value));
    }

    [Fact]
    public async Task Ukrainian_uses_the_ukrainian_separators()
    {
        var formatter = await CreateAsync("uk-UA", TemperatureUnit.Celsius);

        Assert.Equal("22,3", formatter.Value(Metric.Temperature, 22.34));
        Assert.Equal("1 068", formatter.Value(Metric.Co2, 1068.4));
    }

    [Fact]
    public async Task Temperature_is_converted_when_the_unit_is_fahrenheit()
    {
        var formatter = await CreateAsync("en-US", TemperatureUnit.Fahrenheit);

        Assert.Equal("72.1", formatter.Value(Metric.Temperature, 22.3));
        Assert.Equal(72.14, formatter.ToDisplay(Metric.Temperature, 22.3), 2);
    }

    [Fact]
    public async Task Other_metrics_are_not_converted_in_fahrenheit()
    {
        var formatter = await CreateAsync("en-US", TemperatureUnit.Fahrenheit);

        Assert.Equal("44", formatter.Value(Metric.Humidity, 44));
        Assert.Equal(44, formatter.ToDisplay(Metric.Humidity, 44));
    }

    [Theory]
    [InlineData(TemperatureUnit.Celsius, "°C")]
    [InlineData(TemperatureUnit.Fahrenheit, "°F")]
    public async Task Temperature_unit_follows_the_setting(TemperatureUnit unit, string expected)
    {
        var formatter = await CreateAsync("en-US", unit);

        Assert.Equal(expected, formatter.Unit(Metric.Temperature));
        Assert.Equal("ppm", formatter.Unit(Metric.Co2));
        Assert.Equal("%", formatter.Unit(Metric.Humidity));
        Assert.Equal("hPa", formatter.Unit(Metric.Pressure));
    }
}
