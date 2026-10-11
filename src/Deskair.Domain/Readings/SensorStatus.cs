namespace Deskair.Domain.Readings;

public sealed record SensorStatus(TimeSpan Interval, int? BatteryPercent, int? SignalDbm);
