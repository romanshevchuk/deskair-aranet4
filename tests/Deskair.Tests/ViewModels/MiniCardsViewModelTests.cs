using Deskair.App.Localization;
using Deskair.App.ViewModels.Live;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class MiniCardsViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);
    private readonly MiniCardsViewModel viewModel = new(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()));

    private static List<Reading> Hour(double temperature, double humidity, double pressure, double temperatureAtStart = double.NaN)
    {
        var start = double.IsNaN(temperatureAtStart) ? temperature : temperatureAtStart;

        return Enumerable.Range(0, 31)
            .Select(step => Samples.Climate(
                now.AddMinutes(-60 + step * 2),
                step == 0 ? start : temperature,
                humidity,
                pressure))
            .ToList();
    }

    [Theory]
    [InlineData(Metric.Co2)]
    [InlineData(Metric.Temperature)]
    [InlineData(Metric.Pressure)]
    public void Items_are_always_temperature_humidity_and_pressure_in_that_order(Metric selected)
    {
        // POL-06: CO₂ is the hero; the cards never move when the chart's metric changes.
        viewModel.Update(Hour(22, 40, 1000), selected, now);

        Assert.Equal([Metric.Temperature, Metric.Humidity, Metric.Pressure], viewModel.Items.Select(item => item.Metric));
    }

    [Theory]
    [InlineData(Metric.Co2, new bool[] { false, false, false })]
    [InlineData(Metric.Humidity, new bool[] { false, true, false })]
    [InlineData(Metric.Pressure, new bool[] { false, false, true })]
    public void The_card_of_the_chart_metric_is_selected(Metric selected, bool[] expected)
    {
        viewModel.Update(Hour(22, 40, 1000), selected, now);

        Assert.Equal(expected, viewModel.Items.Select(item => item.IsSelected));
    }

    [Fact]
    public void Each_card_has_a_name_for_narrator_with_metric_value_unit_and_status()
    {
        viewModel.Update(Hour(22, 40, 1000), Metric.Co2, now);

        var temperature = viewModel.Items[0];

        Assert.Contains(temperature.Title, temperature.AccessibleName, StringComparison.Ordinal);
        Assert.Contains(temperature.ValueText!, temperature.AccessibleName, StringComparison.Ordinal);
        Assert.Contains(temperature.SubtitleText!, temperature.AccessibleName, StringComparison.Ordinal);
    }

    [Fact]
    public void Items_carry_title_value_and_sparkline()
    {
        var recent = Hour(22.34, 44.4, 975.26);

        viewModel.Update(recent, Metric.Co2, now);

        var temperature = viewModel.Items[0];
        Assert.Equal("Metric_Temperature", temperature.Title);
        Assert.Equal("22.3", temperature.ValueText);
        Assert.Equal(recent.Count, temperature.Sparkline.Count);
        Assert.Equal("44", viewModel.Items[1].ValueText);
        Assert.Equal("975.3", viewModel.Items[2].ValueText);
    }

    [Fact]
    public void In_range_values_show_the_trend()
    {
        viewModel.Update(Hour(22, 40, 1000), Metric.Co2, now);

        Assert.All(viewModel.Items, item => Assert.Equal("Trend_Steady", item.SubtitleText));
    }

    [Fact]
    public void A_rising_pressure_shows_the_trend()
    {
        var recent = Hour(22, 40, 1003);
        recent[0] = Samples.Climate(recent[0].Timestamp, 22, 40, 1000);

        viewModel.Update(recent, Metric.Co2, now);

        Assert.Equal("Trend_Rising", viewModel.Items[2].SubtitleText);
    }

    [Theory]
    [InlineData(18, "Mini_Temperature_Low(min=20, max=24, unit=°C)")]
    [InlineData(26, "Mini_Temperature_High(min=20, max=24, unit=°C)")]
    [InlineData(20, "Trend_Steady")]
    [InlineData(24, "Trend_Steady")]
    public void Temperature_outside_the_ideal_range_shows_a_label(double celsius, string expected)
    {
        viewModel.Update(Hour(celsius, 40, 1000), Metric.Co2, now);

        Assert.Equal(expected, viewModel.Items[0].SubtitleText);
    }

    [Theory]
    [InlineData(20, "Mini_Humidity_Low(min=30, max=50, unit=%)")]
    [InlineData(24, "Mini_Humidity_Low(min=30, max=50, unit=%)")]
    [InlineData(25, "Mini_Humidity_ABitLow(min=30, max=50, unit=%)")]
    [InlineData(29, "Mini_Humidity_ABitLow(min=30, max=50, unit=%)")]
    [InlineData(30, "Trend_Steady")]
    [InlineData(50, "Trend_Steady")]
    [InlineData(51, "Mini_Humidity_ABitHigh(min=30, max=50, unit=%)")]
    [InlineData(55, "Mini_Humidity_ABitHigh(min=30, max=50, unit=%)")]
    [InlineData(56, "Mini_Humidity_High(min=30, max=50, unit=%)")]
    [InlineData(60, "Mini_Humidity_High(min=30, max=50, unit=%)")]
    public void Humidity_outside_the_ideal_range_shows_a_label(double percent, string expected)
    {
        viewModel.Update(Hour(22, percent, 1000), Metric.Co2, now);

        Assert.Equal(expected, viewModel.Items[1].SubtitleText);
    }

    [Fact]
    public void Pressure_never_shows_a_comfort_label()
    {
        viewModel.Update(Hour(22, 40, 900), Metric.Co2, now);
        Assert.Equal("Trend_Steady", viewModel.Items[2].SubtitleText);

        viewModel.Update(Hour(22, 40, 1100), Metric.Co2, now);
        Assert.Equal("Trend_Steady", viewModel.Items[2].SubtitleText);
    }

    [Fact]
    public void No_readings_gives_items_with_empty_values()
    {
        viewModel.Update([], Metric.Co2, now);

        Assert.Equal(3, viewModel.Items.Count);
        Assert.All(viewModel.Items, item =>
        {
            Assert.Null(item.ValueText);
            Assert.Null(item.SubtitleText);
            Assert.Empty(item.Sparkline);
        });
    }

    [Fact]
    public void Numbers_use_the_localizer_culture()
    {
        var localizer = new Localizer();
        localizer.SetCulture(System.Globalization.CultureInfo.GetCultureInfo("uk-UA"));
        var ukrainian = new MiniCardsViewModel(localizer, TestFormatter.Create(localizer));

        ukrainian.Update(Hour(22.3, 44, 975.3), Metric.Co2, now);

        Assert.Equal("22,3", ukrainian.Items[0].ValueText);
        Assert.DoesNotContain("[", ukrainian.Items[0].SubtitleText);
    }

    [Fact]
    public void Fahrenheit_converts_the_value_and_the_ideal_range()
    {
        var localizer = new FakeLocalizer();
        var fahrenheit = new MiniCardsViewModel(localizer, TestFormatter.Create(localizer, TemperatureUnit.Fahrenheit));

        fahrenheit.Update(Hour(18, 40, 1000), Metric.Co2, now);

        Assert.Equal("64.4", fahrenheit.Items[0].ValueText);
        Assert.Equal("Mini_Temperature_Low(min=68, max=75, unit=°F)", fahrenheit.Items[0].SubtitleText);
    }

    [Fact]
    public void Items_carry_the_unit()
    {
        viewModel.Update(Hour(22, 40, 1000), Metric.Co2, now);

        Assert.Equal(["°C", "%", "hPa"], viewModel.Items.Select(item => item.UnitText));
    }
}
