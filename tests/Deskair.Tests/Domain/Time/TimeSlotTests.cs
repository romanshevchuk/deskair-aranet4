using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Time;

public class TimeSlotTests
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 9, 12);

    [Fact]
    public void End_is_start_plus_interval()
    {
        var slot = new TimeSlot(start, TimeSpan.FromMinutes(2));

        Assert.Equal(start.AddMinutes(2), slot.End);
    }

    [Fact]
    public void Contains_is_start_inclusive_and_end_exclusive()
    {
        var slot = new TimeSlot(start, TimeSpan.FromMinutes(2));

        Assert.True(slot.Contains(start));
        Assert.True(slot.Contains(start.AddSeconds(119)));
        Assert.False(slot.Contains(slot.End));
        Assert.False(slot.Contains(start.AddSeconds(-1)));
    }
}
