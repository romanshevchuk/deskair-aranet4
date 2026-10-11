using Deskair.Domain.Zones;

namespace Deskair.Tests.Domain.Zones;

public class ZoneThresholdsTests
{
    [Fact]
    public void Default_is_1000_and_1400_ppm()
    {
        Assert.Equal(1000, ZoneThresholds.Default.ElevatedFromPpm);
        Assert.Equal(1400, ZoneThresholds.Default.HighAbovePpm);
    }

    [Theory]
    [InlineData(1400, 1000)]
    [InlineData(1000, 1000)]
    public void Constructor_rejects_limits_that_are_not_increasing(double elevated, double high)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneThresholds(elevated, high));
    }

    [Fact]
    public void Constructor_rejects_NaN()
    {
        Assert.Throws<ArgumentException>(() => new ZoneThresholds(double.NaN, 1400));
    }
}
