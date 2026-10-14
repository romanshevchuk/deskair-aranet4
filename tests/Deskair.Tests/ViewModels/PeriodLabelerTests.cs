using Deskair.App.ViewModels.History;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class PeriodLabelerTests
{
    private static readonly LocalCalendar calendar = new(TimeZoneInfo.Utc);
    private static readonly PeriodLabeler labeler = new(new FakeLocalizer(), calendar);

    private static string Label(Period range, int offset, DateTimeOffset now) => labeler.Label(HistoryPeriod.For(range, offset, now, calendar));

    [Fact]
    public void A_span_inside_one_month_names_the_month_once()
    {
        Assert.Equal("History_Period_Span(from=8, to=14 Oct 2026)", Label(Period.Week, 0, Samples.Utc(2026, 10, 14, 12)));
    }

    [Fact]
    public void A_span_across_a_year_end_names_both_years()
    {
        Assert.Equal("History_Period_Span(from=29 Dec 2026, to=4 Jan 2027)", Label(Period.Week, 0, Samples.Utc(2027, 1, 4, 12)));
    }

    [Fact]
    public void A_year_is_named_by_its_first_and_last_month()
    {
        Assert.Equal("History_Period_Span(from=Nov 2025, to=Oct 2026)", Label(Period.Year, 0, Samples.Utc(2026, 10, 14, 12)));
    }
}
