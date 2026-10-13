using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>A room was given another name. The room chip and the footer show the new name.</summary>
public sealed record DeviceRenamed(DeviceId Device);
