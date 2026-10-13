using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class ZoneShareCalculatorTests
{
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    private static IEnumerable<Deskair.Domain.Readings.Reading> At(params double[] ppm)
        => ppm.Select((value, index) => Samples.Co2(noon.AddMinutes(index * 2), value));

    [Fact]
    public void Readings_are_shared_out_by_zone()
    {
        var share = ZoneShareCalculator.Calculate(At(500, 600, 700, 800, 1100, 1200, 1500, 1600, 1700, 900))!;

        Assert.Equal(0.5, share.Good);
        Assert.Equal(0.2, share.Elevated);
        Assert.Equal(0.3, share.High);
    }

    [Fact]
    public void The_zone_limits_follow_the_thresholds()
    {
        Assert.Equal(1000, ZoneThresholds.Default.ElevatedFromPpm);
        Assert.Equal(1400, ZoneThresholds.Default.HighAbovePpm);

        var share = ZoneShareCalculator.Calculate(At(999, 1000, 1400, 1401))!;

        Assert.Equal((0.25, 0.5, 0.25), (share.Good, share.Elevated, share.High));
    }

    [Fact]
    public void Readings_without_a_value_are_left_out()
    {
        var share = ZoneShareCalculator.Calculate(At(500, double.NaN))!;

        Assert.Equal(1, share.Good);
    }

    [Fact]
    public void No_readings_give_no_share()
    {
        Assert.Null(ZoneShareCalculator.Calculate([]));
        Assert.Null(ZoneShareCalculator.Calculate(At(double.NaN)));
    }

    [Fact]
    public void Own_thresholds_are_used_when_given()
    {
        var share = ZoneShareCalculator.Calculate(At(500, 700), new ZoneThresholds(600, 900))!;

        Assert.Equal((0.5, 0.5, 0.0), (share.Good, share.Elevated, share.High));
    }
}
