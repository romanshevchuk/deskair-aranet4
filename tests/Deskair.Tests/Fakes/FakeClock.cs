namespace Deskair.Tests.Fakes;

/// <summary>A clock tests can move by hand.</summary>
public sealed class FakeClock : TimeProvider
{
    private DateTimeOffset now;

    public FakeClock(DateTimeOffset start)
    {
        now = start;
    }

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan amount)
    {
        now += amount;
    }
}
