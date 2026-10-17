namespace Deskair.Domain.Analytics.Pressure;

/// <summary>A change in pressure over about 24 hours, in hPa: the value at <see cref="Start"/>, the value at <see cref="End"/> and their difference.</summary>
public sealed record PressureChange(DateTimeOffset Start, DateTimeOffset End, double From, double To)
{
    public double Delta => To - From;
}
