using Deskair.Domain.Readings;

namespace Deskair.Services.Messages;

/// <summary>The sensor refuses to share its readings until the user changes a setting on it; <paramref name="StepKey"/> is the resource key of what to change. The session waits for Try again.</summary>
public sealed record SensorNeedsSetup(DeviceId Device, string StepKey);
