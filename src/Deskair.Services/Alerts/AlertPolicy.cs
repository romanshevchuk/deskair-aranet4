namespace Deskair.Services.Alerts;

/// <summary>Timing rules for alerts (docs/product/behaviour-rules.md "Alerts").</summary>
/// <param name="Dwell">How long the air must stay outside Good before a toast.</param>
/// <param name="RepeatWhileHigh">How long after a High toast another one may follow while the air is still High.</param>
/// <param name="RecoveryDwell">How long the air must stay Good before an episode ends, so a value hovering at 1000 ppm is one episode and not many.</param>
public sealed record AlertPolicy(TimeSpan Dwell, TimeSpan RepeatWhileHigh, TimeSpan RecoveryDwell)
{
    public static AlertPolicy Default { get; } = new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(10));
}
