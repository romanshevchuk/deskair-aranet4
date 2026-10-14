using Deskair.App.Localization;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class SleepViewModelTests
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 12, 22);
    private static readonly DateTimeOffset end = Samples.Utc(2026, 10, 13, 7);

    private static SleepSummary Night(
        Metric metric = Metric.Co2,
        SleepVerdict? verdict = SleepVerdict.Good,
        TimeSpan? elevated = null,
        TimeSpan? recorded = null,
        double? deep = 700,
        double average = 800)
        => new(
            metric,
            start,
            end,
            18,
            recorded ?? TimeSpan.FromHours(9),
            TimeSpan.FromHours(9),
            average,
            new TimedValue(400, Samples.Utc(2026, 10, 13, 4)),
            new TimedValue(1500, Samples.Utc(2026, 10, 13, 1, 30)),
            deep,
            metric == Metric.Co2 ? elevated ?? TimeSpan.Zero : null,
            metric == Metric.Co2 ? verdict : null);

    private static SleepViewModel Create(TemperatureUnit unit = TemperatureUnit.Celsius, TimeZoneInfo? zone = null)
    {
        var localizer = new FakeLocalizer();

        return new SleepViewModel(localizer, TestFormatter.Create(localizer, unit), new LocalCalendar(zone ?? TimeZoneInfo.Utc));
    }

    [Fact]
    public void A_good_night_gives_the_title_the_verdict_and_the_three_values()
    {
        var viewModel = Create();

        viewModel.Update(Night(elevated: TimeSpan.Zero));

        Assert.True(viewModel.HasData);
        Assert.Equal("Sleep_Title(from=22:00, to=07:00)", viewModel.Title);
        Assert.Equal("Sleep_Verdict_Good", viewModel.VerdictText);
        Assert.Equal(Zone.Good, viewModel.VerdictZone);
        Assert.Equal("Sleep_TimeAbove(limit=1000, unit=ppm)", viewModel.FirstLabel);
        Assert.Equal("Sleep_HoursOf(hours=0, total=9)", viewModel.FirstValue);
        Assert.Equal("Sleep_Peak(time=01:30)", viewModel.PeakLabel);
        Assert.Equal("1500 ppm", viewModel.PeakValue);
        Assert.Equal("Sleep_Deep", viewModel.DeepLabel);
        Assert.Equal("700 ppm", viewModel.DeepValue);
        Assert.Equal("Sleep_Tip_Fresh", viewModel.Tip);
        Assert.False(viewModel.ShowPartialNote);
    }

    [Theory]
    [InlineData(SleepVerdict.Good, Zone.Good, "Sleep_Verdict_Good")]
    [InlineData(SleepVerdict.Fair, Zone.Elevated, "Sleep_Verdict_Fair")]
    [InlineData(SleepVerdict.Poor, Zone.High, "Sleep_Verdict_Poor")]
    public void The_verdict_has_its_text_and_zone_colour(SleepVerdict verdict, Zone zone, string text)
    {
        var viewModel = Create();

        viewModel.Update(Night(verdict: verdict));

        Assert.True(viewModel.HasVerdict);
        Assert.Equal(zone, viewModel.VerdictZone);
        Assert.Equal(text, viewModel.VerdictText);
    }

    [Fact]
    public void More_than_an_hour_above_the_limit_suggests_a_door_or_window()
    {
        var viewModel = Create();

        viewModel.Update(Night(verdict: SleepVerdict.Fair, elevated: TimeSpan.FromHours(2.5)));

        Assert.Equal("Sleep_HoursOf(hours=2.5, total=9)", viewModel.FirstValue);
        Assert.Equal("Sleep_Tip_Ajar", viewModel.Tip);
    }

    [Fact]
    public void Exactly_an_hour_above_the_limit_is_still_fresh_air()
    {
        var viewModel = Create();

        viewModel.Update(Night(elevated: TimeSpan.FromHours(1)));

        Assert.Equal("Sleep_Tip_Fresh", viewModel.Tip);
    }

    [Fact]
    public void A_night_with_few_readings_says_how_much_was_recorded()
    {
        var viewModel = Create();

        viewModel.Update(Night(recorded: TimeSpan.FromHours(4.5)));

        Assert.True(viewModel.ShowPartialNote);
        Assert.Equal("Sleep_Partial(recorded=4.5, total=9)", viewModel.PartialNote);
        Assert.Equal("Sleep_HoursOf(hours=0, total=4.5)", viewModel.FirstValue);
    }

    [Fact]
    public void A_night_without_a_deep_sleep_average_shows_a_dash()
    {
        var viewModel = Create();

        viewModel.Update(Night(deep: null));

        Assert.Equal("–", viewModel.DeepValue);
    }

    [Fact]
    public void A_night_without_readings_says_so_and_has_no_numbers_or_verdict()
    {
        var viewModel = Create();
        var empty = new SleepSummary(Metric.Co2, start, end, 0, TimeSpan.Zero, TimeSpan.FromHours(9), null, null, null, null, null, null);

        viewModel.Update(empty);

        Assert.False(viewModel.HasData);
        Assert.False(viewModel.HasVerdict);
        Assert.Equal("Sleep_Title(from=22:00, to=07:00)", viewModel.Title);
        Assert.Equal("Sleep_Empty", viewModel.Tip);
    }

    [Fact]
    public void Without_a_summary_there_is_nothing_to_show()
    {
        var viewModel = Create();
        viewModel.Update(Night());

        viewModel.Update(null);

        Assert.False(viewModel.HasData);
        Assert.Equal(string.Empty, viewModel.Title);
    }

    [Fact]
    public void Other_metrics_show_the_lowest_value_and_no_verdict()
    {
        var viewModel = Create(TemperatureUnit.Fahrenheit);

        viewModel.Update(Night(Metric.Temperature, deep: 20));

        Assert.False(viewModel.HasVerdict);
        Assert.Equal("Stats_Lowest", viewModel.FirstLabel);
        Assert.Equal("752.0 °F", viewModel.FirstValue);
        Assert.Equal("Sleep_Tip_Other", viewModel.Tip);
        Assert.Equal("68.0 °F", viewModel.DeepValue);
    }

    [Fact]
    public void The_title_and_the_peak_time_are_in_local_time()
    {
        // 13 October is summer time in Kyiv (UTC+3).
        var viewModel = Create(zone: Samples.Kyiv);

        viewModel.Update(Night());

        Assert.Equal("Sleep_Title(from=01:00, to=10:00)", viewModel.Title);
        Assert.Equal("Sleep_Peak(time=04:30)", viewModel.PeakLabel);
    }
}
