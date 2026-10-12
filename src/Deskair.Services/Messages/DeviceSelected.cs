using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>The user chose another room (device). Live, the footer and the sensor session follow.</summary>
public sealed record DeviceSelected(DeviceId Device);
