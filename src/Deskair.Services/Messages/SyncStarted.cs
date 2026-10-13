using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>The history download of a sensor began. It ends with <see cref="SyncCompleted"/>, which is sent even when the download failed or was stopped.</summary>
public sealed record SyncStarted(DeviceId Device);
