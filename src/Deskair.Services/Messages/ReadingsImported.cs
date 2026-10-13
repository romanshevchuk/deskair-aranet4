using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>Readings from a file were merged into a device's history. <see cref="ReadingsAdded"/> is how many new slots were filled.</summary>
public sealed record ReadingsImported(DeviceId Device, int ReadingsAdded);
