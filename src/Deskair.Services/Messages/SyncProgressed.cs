using Deskair.Domain.Readings;
using Deskair.Services.Sensors;

namespace Deskair.Services.Messages;

/// <summary>A history download is under way: how far it is.</summary>
public sealed record SyncProgressed(DeviceId Device, HistoryProgress Progress);
