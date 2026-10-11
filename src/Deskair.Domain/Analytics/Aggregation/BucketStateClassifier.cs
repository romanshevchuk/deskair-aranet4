namespace Deskair.Domain.Analytics.Aggregation;

public static class BucketStateClassifier
{
    // TODO(HIST-04a) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 4.
    // Input : readings present in one bucket, expected slot count.
    // Output: BucketState (Complete, Partial, NoData).
    // Rules : none present = NoData; under 80 % of expected = Partial; otherwise Complete.
    // Edge  : expected = 0 returns NoData.
    // Done  : unskip tests in Domain/Analytics/BucketStateClassifierTests.cs and make them pass.
    public static BucketState Classify(int present, int expected)
        => throw new NotImplementedException("TODO(HIST-04a)");
}
