using Deskair.Domain.Readings;
using Deskair.Services.Sync;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class SlotMergerTests
{
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 59, 0)]
    [InlineData(1, 0, 2)]
    [InlineData(1, 30, 2)]
    [InlineData(2, 29, 2)]
    public void Snap_rounds_to_the_nearest_slot_and_half_an_interval_rounds_up(int minute, int second, int expectedMinute)
    {
        var timestamp = Samples.Utc(2026, 10, 9, 12, minute, second);

        Assert.Equal(noon.AddMinutes(expectedMinute), SlotMerger.Snap(timestamp, interval));
    }

    [Fact]
    public void Snap_rejects_an_interval_of_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SlotMerger.Snap(noon, TimeSpan.Zero));
    }

    [Fact]
    public void Merge_adds_only_readings_for_empty_slots_sorted_by_time()
    {
        var existing = new List<Reading> { Samples.Co2(noon, 800) };
        var incoming = new List<Reading>
        {
            Samples.Co2(noon.AddMinutes(4), 900),
            Samples.Co2(noon.AddSeconds(20), 700),
            Samples.Co2(noon.AddMinutes(2), 850)
        };

        var added = SlotMerger.Merge(existing, incoming, interval);

        Assert.Equal(new[] { 850.0, 900.0 }, added.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public void Merge_keeps_the_first_of_two_incoming_readings_in_the_same_slot()
    {
        var incoming = new List<Reading>
        {
            Samples.Co2(noon.AddSeconds(10), 700),
            Samples.Co2(noon.AddSeconds(50), 710)
        };

        var added = SlotMerger.Merge([], incoming, interval);

        Assert.Single(added);
        Assert.Equal(700, added[0].Co2Ppm);
    }

    [Fact]
    public void Merge_snaps_timestamps_to_the_slot_start()
    {
        var incoming = new List<Reading> { Samples.Co2(noon.AddSeconds(50), 700) };

        var added = SlotMerger.Merge([], incoming, interval);

        Assert.Equal(noon, added[0].Timestamp);
    }

    [Fact]
    public void Merge_of_nothing_adds_nothing()
    {
        Assert.Empty(SlotMerger.Merge([Samples.Co2(noon, 800)], [], interval));
    }

    [Fact]
    public void Merge_does_not_let_a_reading_with_a_missing_value_fill_a_slot()
    {
        var incoming = new List<Reading> { Samples.Co2(noon, double.NaN), Samples.Co2(noon.AddSeconds(30), 700) };

        var added = SlotMerger.Merge([], incoming, interval);

        Assert.Equal(700, Assert.Single(added).Co2Ppm);
    }
}
