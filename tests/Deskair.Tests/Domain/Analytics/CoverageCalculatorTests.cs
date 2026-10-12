using Deskair.Domain.Analytics.Coverage;

namespace Deskair.Tests.Domain.Analytics;

public class CoverageCalculatorTests
{
    [Theory]
    [InlineData(50, 100, 0.5)]
    [InlineData(100, 100, 1.0)]
    [InlineData(120, 100, 1.0)]
    [InlineData(0, 100, 0.0)]
    [InlineData(5, 0, 0.0)]
    public void Fraction_is_present_over_expected_capped_at_one(int present, int expected, double fraction)
    {
        Assert.Equal(fraction, CoverageCalculator.Fraction(present, expected), 6);
    }

    [Theory]
    [InlineData(0.98, 0, false)]
    [InlineData(1.0, 0, false)]
    [InlineData(0.979, 0, true)]
    [InlineData(1.0, 1, true)]
    [InlineData(0.5, 3, true)]
    public void ShouldShowChip_hides_at_98_percent_with_no_empty_buckets(double fraction, int emptyBuckets, bool expected)
    {
        Assert.Equal(expected, CoverageCalculator.ShouldShowChip(fraction, emptyBuckets));
    }
}
