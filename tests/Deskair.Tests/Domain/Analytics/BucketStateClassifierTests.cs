using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.Tests.Domain.Analytics;

public class BucketStateClassifierTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(0, 0)]
    [InlineData(5, 0)]
    public void Classify_returns_NoData_when_nothing_present_or_nothing_expected(int present, int expected)
    {
        Assert.Equal(BucketState.NoData, BucketStateClassifier.Classify(present, expected));
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(7, 10)]
    [InlineData(79, 100)]
    public void Classify_returns_Partial_under_80_percent(int present, int expected)
    {
        Assert.Equal(BucketState.Partial, BucketStateClassifier.Classify(present, expected));
    }

    [Theory]
    [InlineData(8, 10)]
    [InlineData(80, 100)]
    [InlineData(10, 10)]
    [InlineData(12, 10)]
    public void Classify_returns_Complete_at_80_percent_or_more(int present, int expected)
    {
        Assert.Equal(BucketState.Complete, BucketStateClassifier.Classify(present, expected));
    }
}
