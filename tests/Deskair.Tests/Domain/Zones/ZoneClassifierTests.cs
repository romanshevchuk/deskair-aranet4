using Deskair.Domain.Zones;

namespace Deskair.Tests.Domain.Zones;

public class ZoneClassifierTests
{
    [Theory]
    [InlineData(0, Zone.Good)]
    [InlineData(450, Zone.Good)]
    [InlineData(999.9, Zone.Good)]
    [InlineData(1000, Zone.Elevated)]
    [InlineData(1400, Zone.Elevated)]
    [InlineData(1400.1, Zone.High)]
    [InlineData(5000, Zone.High)]
    public void Classify_uses_the_fixed_limits(double ppm, Zone expected)
    {
        Assert.Equal(expected, ZoneClassifier.Classify(ppm));
    }

    [Fact]
    public void Classify_uses_custom_thresholds_when_given()
    {
        var thresholds = new ZoneThresholds(800, 1200);

        Assert.Equal(Zone.Elevated, ZoneClassifier.Classify(900, thresholds));
        Assert.Equal(Zone.High, ZoneClassifier.Classify(1201, thresholds));
    }

    [Fact]
    public void Classify_rejects_NaN()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ZoneClassifier.Classify(double.NaN));
    }

    [Fact]
    public void Classify_rejects_missing_thresholds()
    {
        Assert.Throws<ArgumentNullException>(() => ZoneClassifier.Classify(500, null!));
    }
}
