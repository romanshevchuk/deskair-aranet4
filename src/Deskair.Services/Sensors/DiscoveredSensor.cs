using Deskair.Domain.Readings;

namespace Deskair.Services.Sensors;

public sealed record DiscoveredSensor(DeviceId Id, string? Name, int? SignalDbm);
