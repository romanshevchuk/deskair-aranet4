using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Shell;

/// <summary>One room in the room chip and its menu. The value text is already formatted ("1,068 ppm"); the zone is null when there is no reading.</summary>
public sealed record RoomItem(DeviceId Device, string Name, string? ValueText, Zone? Zone);
