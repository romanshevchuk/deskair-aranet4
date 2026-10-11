namespace Deskair.Domain.Analytics.Coverage;

public static class CoverageCalculator
{
    // TODO(HIST-04b) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 5; docs/engineering/data-model.md.
    // Input : readings present, expected slots.
    // Output: coverage as a fraction from 0 to 1.
    // Rules : present divided by expected, capped at 1.
    // Edge  : expected = 0 returns 0.
    // Done  : unskip tests Domain/Analytics/CoverageCalculatorTests.Fraction_* and make them pass.
    public static double Fraction(int present, int expected)
        => throw new NotImplementedException("TODO(HIST-04b)");

    // TODO(HIST-04c) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 5.
    // Input : coverage fraction, number of buckets with no data.
    // Output: true when the coverage chip should show.
    // Rules : hidden at 98 % or more with no empty buckets; otherwise shown.
    // Edge  : fraction 0.98 exactly with zero empty buckets is hidden.
    // Done  : unskip tests Domain/Analytics/CoverageCalculatorTests.ShouldShowChip_* and make them pass.
    public static bool ShouldShowChip(double fraction, int emptyBuckets)
        => throw new NotImplementedException("TODO(HIST-04c)");
}
