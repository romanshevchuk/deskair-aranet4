using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.TypicalDay;

public static class TypicalDayCalculator
{
    // TODO(HIST-05a) [M, sonnet]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 6.
    // Input : CO₂ readings in the chosen range, LocalCalendar.
    // Output: 24 HourOfDayAverage entries, hour 0 to 23, in local time.
    // Rules : mean of existing readings per local hour; DST hours merge into the same hour number.
    // Edge  : an hour with no readings has Average null and count 0.
    // Done  : unskip tests Domain/Analytics/TypicalDayCalculatorTests.Hourly_* and make them pass.
    public static IReadOnlyList<HourOfDayAverage> HourlyAverages(
        IReadOnlyList<Reading> readings,
        LocalCalendar calendar)
        => throw new NotImplementedException("TODO(HIST-05a)");

    // TODO(HIST-05b) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 6 (worst three-hour window).
    // Input : the 24 hourly averages.
    // Output: start hour (0 to 23) of the three consecutive hours with the highest mean, or null.
    // Rules : windows wrap past midnight (22, 23, 0); hours without data are skipped; ties pick the earliest start.
    // Edge  : fewer than three hours with data returns null.
    // Done  : unskip tests Domain/Analytics/TypicalDayCalculatorTests.Worst_* and make them pass.
    public static int? WorstWindowStartHour(IReadOnlyList<HourOfDayAverage> hours)
        => throw new NotImplementedException("TODO(HIST-05b)");
}
