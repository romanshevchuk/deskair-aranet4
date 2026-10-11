using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.Services.Messages;

public sealed record ZoneChanged(DeviceId Device, Zone Previous, Zone Current, DateTimeOffset At);
