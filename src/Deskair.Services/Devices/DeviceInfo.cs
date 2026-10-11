using Deskair.Domain.Readings;

namespace Deskair.Services.Devices;

/// <summary>What is kept in device.json. Interval history lets coverage use the interval that applied at the time.</summary>
public sealed record DeviceInfo(
    DeviceId Id,
    string RoomName,
    string? RoomType,
    IReadOnlyList<IntervalChange> IntervalHistory,
    DateTimeOffset? LastSyncUtc);
