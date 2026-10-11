using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.Services.Alerts;

/// <summary>Structured alert data. The app turns it into words.</summary>
public sealed record AlertNotification(DeviceId Device, string RoomName, Zone Zone, double Co2Ppm, DateTimeOffset At);
