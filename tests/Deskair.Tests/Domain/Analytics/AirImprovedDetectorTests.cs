using Deskair.Domain.Analytics.AirImproved;
using Deskair.Domain.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class AirImprovedDetectorTests
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 14, 14);

    /// <summary>Readings every <paramref name="minutes"/> minutes from 14:00 with these values.</summary>
    private static List<Reading> At(int minutes, params double[] ppm)
        => [.. ppm.Select((value, index) => Samples.Co2(start.AddMinutes(index * minutes), value))];

    [Fact]
    public void A_drop_of_150_ppm_within_10_minutes_from_1000_or_more_starts_an_event()
    {
        var events = AirImprovedDetector.Detect(At(5, 1400, 1380, 1250, 1100, 1099));

        var found = Assert.Single(events);
        Assert.Equal(start, found.Start);
        Assert.Equal(1400, found.StartPpm);
    }

    [Fact]
    public void The_event_ends_when_it_flattens_and_reports_the_drop_and_how_long_it_took()
    {
        // 1400 -> 1100 in 15 minutes, then 1095 (5 ppm per 5 minutes, flat): the event ends at 1100.
        var found = Assert.Single(AirImprovedDetector.Detect(At(5, 1400, 1250, 1180, 1100, 1095)));

        Assert.Equal(300, found.DropPpm);
        Assert.Equal(TimeSpan.FromMinutes(15), found.Duration);
    }

    [Fact]
    public void The_event_ends_when_the_air_is_below_1000()
    {
        var found = Assert.Single(AirImprovedDetector.Detect(At(5, 1200, 1000, 900, 700, 500)));

        Assert.Equal(300, found.DropPpm);
        Assert.Equal(TimeSpan.FromMinutes(10), found.Duration);
    }

    [Fact]
    public void The_event_ends_45_minutes_after_it_started()
    {
        var values = Enumerable.Range(0, 12).Select(index => 2400.0 - (index * 80)).ToArray();

        var found = Assert.Single(AirImprovedDetector.Detect(At(5, values)));

        Assert.Equal(TimeSpan.FromMinutes(45), found.Duration);
        Assert.Equal(9 * 80, found.DropPpm);
    }

    [Fact]
    public void A_fall_that_is_too_slow_is_not_an_event()
    {
        Assert.Empty(AirImprovedDetector.Detect(At(5, 1400, 1350, 1300, 1250, 1200)));
    }

    [Fact]
    public void A_drop_that_takes_longer_than_10_minutes_is_not_an_event()
    {
        Assert.Empty(AirImprovedDetector.Detect(At(6, 1400, 1330, 1260)));
    }

    [Fact]
    public void Air_below_1000_never_starts_an_event()
    {
        Assert.Empty(AirImprovedDetector.Detect(At(5, 990, 700, 500, 400)));
    }

    [Fact]
    public void A_drop_that_starts_at_exactly_1000_counts()
    {
        Assert.Single(AirImprovedDetector.Detect(At(5, 1000, 900, 800)));
    }

    [Fact]
    public void A_gap_in_the_readings_does_not_join_two_sides_into_a_drop()
    {
        var readings = new List<Reading>
        {
            Samples.Co2(start, 1500),
            Samples.Co2(start.AddMinutes(30), 1200)
        };

        Assert.Empty(AirImprovedDetector.Detect(readings));
    }

    [Fact]
    public void Two_readings_are_enough_and_none_or_one_gives_nothing()
    {
        Assert.Single(AirImprovedDetector.Detect(At(5, 1500, 1300)));
        Assert.Empty(AirImprovedDetector.Detect(At(5, 1500)));
        Assert.Empty(AirImprovedDetector.Detect([]));
    }

    [Fact]
    public void A_later_event_starts_after_the_first_one_has_ended()
    {
        // First event 14:00 to 14:10; the air stays flat; second drop at 15:30.
        var readings = At(5, 1500, 1350, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1300, 1600, 1450, 1300);

        var events = AirImprovedDetector.Detect(readings);

        Assert.Equal(2, events.Count);
        Assert.Equal(start, events[0].Start);
        Assert.Equal(start.AddMinutes(90), events[1].Start);
    }

    [Fact]
    public void Markers_are_at_least_60_minutes_apart()
    {
        var events = new[]
        {
            new AirImprovedEvent(start, 1500, 300, TimeSpan.FromMinutes(10)),
            new AirImprovedEvent(start.AddMinutes(40), 1500, 300, TimeSpan.FromMinutes(10)),
            new AirImprovedEvent(start.AddMinutes(60), 1500, 300, TimeSpan.FromMinutes(10)),
            new AirImprovedEvent(start.AddMinutes(100), 1500, 300, TimeSpan.FromMinutes(10)),
            new AirImprovedEvent(start.AddMinutes(130), 1500, 300, TimeSpan.FromMinutes(10))
        };

        var markers = AirImprovedDetector.Markers(events);

        Assert.Equal([start, start.AddMinutes(60), start.AddMinutes(130)], markers.Select(marker => marker.Start));
    }
}
