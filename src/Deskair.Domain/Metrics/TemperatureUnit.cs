namespace Deskair.Domain.Metrics;

/// <summary>How temperatures are shown. Stored readings are always in °C.</summary>
public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

public static class TemperatureUnitExtensions
{
    public static double FromCelsius(this TemperatureUnit unit, double celsius)
        => unit == TemperatureUnit.Fahrenheit ? celsius * 9 / 5 + 32 : celsius;

    public static double ToCelsius(this TemperatureUnit unit, double value)
        => unit == TemperatureUnit.Fahrenheit ? (value - 32) * 5 / 9 : value;

    public static string Symbol(this TemperatureUnit unit)
        => unit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
}
