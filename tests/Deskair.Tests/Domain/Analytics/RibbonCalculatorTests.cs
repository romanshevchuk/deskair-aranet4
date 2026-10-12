using Deskair.Domain.Analytics.Ribbon;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class RibbonCalculatorTests
{
    private static readonly DateTimeOffset end = Samples.Utc(2026, 10, 12, 12);

    private static Reading At(int minutesAgo, double ppm) => Samples.Co2(end.AddMinutes(-minutesAgo), ppm);

    [Fact]
    public void A_day_is_144_slots_of_ten_minutes_ending_at_the_end_time()
    {
        var readings = Enumerable.Range(0, 24 * 30).Select(index => At(index * 2, 700)).ToList();

        var spans = RibbonCalculator.Build(readings, end);

        Assert.Equal(144, spans.Count);
        Assert.Equal(end.AddHours(-24), spans[0].Start);
        Assert.Equal(end, spans[^1].End);
        Assert.All(spans, span => Assert.Equal(TimeSpan.FromMinutes(10), span.End - span.Start));
    }

    [Fact]
    public void A_slot_takes_the_zone_of_its_average()
    {
        // The last slot (minutes 0 to 10 before the end) holds 900 and 1,300 ppm: the average, 1,100, is elevated.
        var readings = new[] { At(9, 900), At(3, 1300) };

        var last = RibbonCalculator.Build(readings, end)[^1];

        Assert.Equal(Zone.Elevated, last.Zone);
    }

    [Theory]
    [InlineData(600, Zone.Good)]
    [InlineData(1000, Zone.Elevated)]
    [InlineData(1400, Zone.Elevated)]
    [InlineData(1401, Zone.High)]
    public void Zone_limits_follow_the_classifier(double ppm, Zone expected)
    {
        var last = RibbonCalculator.Build([At(5, ppm)], end)[^1];

        Assert.Equal(expected, last.Zone);
    }

    [Fact]
    public void A_slot_without_readings_is_left_out_so_it_shows_as_a_gap()
    {
        var spans = RibbonCalculator.Build([At(5, 700), At(125, 700)], end);

        Assert.Equal(2, spans.Count);
        Assert.Equal(end.AddMinutes(-10), spans[1].Start);
    }

    [Fact]
    public void Readings_older_than_a_day_or_in_the_future_are_ignored()
    {
        var spans = RibbonCalculator.Build([At(24 * 60 + 5, 700), Samples.Co2(end.AddMinutes(5), 700)], end);

        Assert.Empty(spans);
    }

    [Fact]
    public void No_readings_give_no_spans()
    {
        Assert.Empty(RibbonCalculator.Build([], end));
    }

    [Fact]
    public void A_reading_exactly_on_the_end_belongs_to_the_last_slot()
    {
        var spans = RibbonCalculator.Build([At(0, 1500)], end);

        Assert.Equal(Zone.High, Assert.Single(spans).Zone);
    }
}
