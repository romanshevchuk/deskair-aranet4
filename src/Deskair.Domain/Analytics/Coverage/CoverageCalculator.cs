namespace Deskair.Domain.Analytics.Coverage;

public static class CoverageCalculator
{
    public static double Fraction(int present, int expected)
        => expected <= 0 ? 0 : Math.Min(1.0, (double)present / expected);

    public static bool ShouldShowChip(double fraction, int emptyBuckets)
        => fraction < 0.98 || emptyBuckets > 0;
}
