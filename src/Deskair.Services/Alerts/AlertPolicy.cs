namespace Deskair.Services.Alerts;

/// <summary>Timing rules for alerts (docs/product/behaviour-rules.md "Alerts").</summary>
public sealed record AlertPolicy(TimeSpan Dwell, TimeSpan RepeatWhileHigh)
{
    public static AlertPolicy Default { get; } = new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(60));
}
