using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>A sensor connected and reported its interval, battery and signal.</summary>
public sealed record SensorStatusUpdated(DeviceId Device, SensorStatus Status);
