using Deskair.Domain.Metrics;

namespace Deskair.Tests.Domain;

public class TemperatureUnitTests
{
    [Theory]
    [InlineData(0, 32)]
    [InlineData(22.3, 72.14)]
    [InlineData(-40, -40)]
    [InlineData(100, 212)]
    public void Fahrenheit_converts_from_celsius(double celsius, double expected)
    {
        Assert.Equal(expected, TemperatureUnit.Fahrenheit.FromCelsius(celsius), 2);
    }

    [Fact]
    public void Celsius_leaves_the_value_alone()
    {
        Assert.Equal(22.3, TemperatureUnit.Celsius.FromCelsius(22.3));
        Assert.Equal(22.3, TemperatureUnit.Celsius.ToCelsius(22.3));
    }

    [Theory]
    [InlineData(32, 0)]
    [InlineData(75.2, 24)]
    [InlineData(-40, -40)]
    public void Fahrenheit_converts_to_celsius(double fahrenheit, double expected)
    {
        Assert.Equal(expected, TemperatureUnit.Fahrenheit.ToCelsius(fahrenheit), 6);
    }

    [Theory]
    [InlineData(TemperatureUnit.Celsius, "°C")]
    [InlineData(TemperatureUnit.Fahrenheit, "°F")]
    public void Symbol_is_the_unit_text(TemperatureUnit unit, string expected)
    {
        Assert.Equal(expected, unit.Symbol());
    }
}
