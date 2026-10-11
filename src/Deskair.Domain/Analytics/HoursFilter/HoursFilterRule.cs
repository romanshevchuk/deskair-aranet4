using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.HoursFilter;

public static class HoursFilterRule
{
    // TODO(HIST-02a) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 7.
    // Input : a UTC instant, filter, LocalCalendar.
    // Output: true when the instant belongs to the filter.
    // Rules : AllDay is always true; Night is local 22:00 up to 07:00 (end exclusive).
    // Edge  : exactly 22:00 is in, exactly 07:00 is out.
    // Done  : unskip tests Domain/Analytics/HoursFilterRuleTests.Includes_* and make them pass.
    public static bool Includes(DateTimeOffset instant, HoursFilterMode filter, LocalCalendar calendar)
        => throw new NotImplementedException("TODO(HIST-02a)");

    // TODO(HIST-02b) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 7 (a night belongs to the evening it starts on).
    // Input : a UTC instant that is inside a night, LocalCalendar.
    // Output: the local date of the evening that night started on.
    // Rules : before 07:00 local belongs to the previous date; from 22:00 it belongs to the same date.
    // Edge  : 00:00 to 07:00 on 1 January belongs to 31 December.
    // Done  : unskip tests Domain/Analytics/HoursFilterRuleTests.NightOwner_* and make them pass.
    public static DateOnly NightOwner(DateTimeOffset instant, LocalCalendar calendar)
        => throw new NotImplementedException("TODO(HIST-02b)");

    // TODO(HIST-02c) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 7.
    // Input : readings, filter, LocalCalendar.
    // Output: only the readings the filter includes, order preserved.
    // Rules : uses Includes; does not mutate the input.
    // Edge  : empty input returns an empty list.
    // Done  : unskip tests Domain/Analytics/HoursFilterRuleTests.Apply_* and make them pass.
    public static IReadOnlyList<Reading> Apply(
        IReadOnlyList<Reading> readings,
        HoursFilterMode filter,
        LocalCalendar calendar)
        => throw new NotImplementedException("TODO(HIST-02c)");
}
