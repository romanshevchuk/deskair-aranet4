namespace Deskair.Domain.Readings;

public readonly record struct Reading(
    DateTimeOffset Timestamp,
    double Co2Ppm,
    double TemperatureC,
    double HumidityPct,
    double PressureHpa);
