namespace Deskair.Domain.Analytics.Aggregation;

public static class BucketStateClassifier
{
    public static BucketState Classify(int present, int expected)
    {
        if (present <= 0 || expected <= 0)
        {
            return BucketState.NoData;
        }

        return present * 5 < expected * 4 ? BucketState.Partial : BucketState.Complete;
    }
}
