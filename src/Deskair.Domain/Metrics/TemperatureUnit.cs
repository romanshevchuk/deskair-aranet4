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

    public static string Symbol(this TemperatureUnit unit)
        => unit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
}
