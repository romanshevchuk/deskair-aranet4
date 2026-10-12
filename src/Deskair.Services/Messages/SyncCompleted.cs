using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>The sensor's history was downloaded and merged. <see cref="ReadingsAdded"/> is how many new slots were filled.</summary>
public sealed record SyncCompleted(DeviceId Device, int ReadingsAdded);
