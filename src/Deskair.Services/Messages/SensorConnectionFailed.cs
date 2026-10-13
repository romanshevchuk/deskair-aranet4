using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>An attempt to connect to a sensor (or to keep its connection) failed; the session tries again later. Lets the screen say "Still can't reach it" after a Try again.</summary>
public sealed record SensorConnectionFailed(DeviceId Device);
