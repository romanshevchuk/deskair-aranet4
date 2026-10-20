using Deskair.Domain.Readings;

namespace Deskair.Services.Sensors;

/// <summary>A new measurement a sensor announced without a connection, with what it said about itself.</summary>
public sealed record SensorBroadcast(Reading Reading, TimeSpan Interval, int? BatteryPercent);
