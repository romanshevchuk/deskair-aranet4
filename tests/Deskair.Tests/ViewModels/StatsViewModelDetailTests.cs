using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.Pressure;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

/// <summary>The parts of the summary card that need more than the current period: change, extreme times, coverage, zone share and the tip.</summary>
public class StatsViewModelDetailTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static Bucket Day(int day, double average, BucketState state = BucketState.Complete, int count = 10)
        => new(start.AddDays(day), start.AddDays(day + 1), average, average, average, count, 10, state);

    private static async Task<StatsViewModel> CreateAsync(TemperatureUnit unit = TemperatureUnit.Celsius)
    {
        var settings = new SettingsService(new FakeSettingsStore { Stored = AppSettings.Default with { TemperatureUnit = unit } }, new StrongReferenceMessenger());
        await settings.LoadAsync(CancellationToken.None);
        var localizer = new FakeLocalizer();

        return new StatsViewModel(new MetricFormatter(localizer, settings), localizer, new LocalCalendar(TimeZoneInfo.Utc));
    }

    private static StatsInput Week(IReadOnlyList<Bucket> buckets, IReadOnlyList<Bucket>? previous = null, Metric metric = Metric.Co2)
        => new(buckets) { Metric = metric, Range = Period.Week, Grouping = Period.Day, Previous = previous };

    [Fact]
    public async Task A_lower_average_than_the_period_before_is_shown_as_an_improvement_for_CO2()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1000)], [Day(-7, 1120)]));

        Assert.Equal("▼ 120 ppm", viewModel.ChangeText);
        Assert.Equal(Zone.Good, viewModel.ChangeZone);
        Assert.Equal("Stats_Change_Week", viewModel.ChangeCaption);
    }

    [Fact]
    public async Task A_higher_average_is_shown_as_worse_for_CO2()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1200)], [Day(-7, 1000)]));

        Assert.Equal("▲ 200 ppm", viewModel.ChangeText);
        Assert.Equal(Zone.High, viewModel.ChangeZone);
    }

    [Fact]
    public async Task A_change_that_rounds_to_nothing_has_no_arrow_and_no_colour()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1000.2)], [Day(-7, 1000)]));

        Assert.Equal("±0 ppm", viewModel.ChangeText);
        Assert.Null(viewModel.ChangeZone);
    }

    [Fact]
    public async Task Without_an_earlier_period_there_is_nothing_to_compare()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1000)]));

        Assert.True(viewModel.HasChange);
        Assert.Equal("–", viewModel.ChangeText);
        Assert.Equal("Stats_Change_None", viewModel.ChangeCaption);
        Assert.Null(viewModel.ChangeZone);
    }

    [Fact]
    public async Task Without_data_there_is_no_change_line()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([], [Day(-7, 1000)]));

        Assert.False(viewModel.HasChange);
    }

    [Fact]
    public async Task A_temperature_change_is_not_coloured_and_follows_the_unit_without_the_offset()
    {
        var viewModel = await CreateAsync(TemperatureUnit.Fahrenheit);

        viewModel.Update(Week([Day(0, 22)], [Day(-7, 20)], Metric.Temperature));

        Assert.Equal("▲ 3.6 °F", viewModel.ChangeText);
        Assert.Null(viewModel.ChangeZone);
    }

    [Fact]
    public async Task The_zone_share_is_given_as_percent_for_CO2_only()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800)]) with { ZoneShare = new ZoneShare(0.62, 0.28, 0.10) });

        Assert.True(viewModel.HasZoneShare);
        Assert.Equal((0.62, 0.28, 0.10), (viewModel.ZoneGood, viewModel.ZoneElevated, viewModel.ZoneHigh));
        Assert.Equal(("62%", "28%", "10%"), (viewModel.ZoneGoodText, viewModel.ZoneElevatedText, viewModel.ZoneHighText));

        viewModel.Update(Week([Day(0, 20)], metric: Metric.Temperature) with { ZoneShare = new ZoneShare(1, 0, 0) });
        Assert.False(viewModel.HasZoneShare);
    }

    [Fact]
    public async Task The_insight_names_the_worst_three_hours()
    {
        var viewModel = await CreateAsync();
        List<HourOfDayAverage> hours = [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is 14 or 15 or 16 ? 1350 : 700, 1))];

        viewModel.Update(Week([Day(0, 800)]) with { TypicalDay = hours });

        Assert.Equal("Insight_WorstHours(from=14:00, to=17:00, value=1350 ppm)", viewModel.InsightText);
    }

    [Theory]
    [InlineData(22, true)]
    [InlineData(23, true)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    [InlineData(20, false)]
    [InlineData(21, true)]
    [InlineData(14, false)]
    public void A_worst_window_with_two_night_hours_or_more_is_while_you_sleep(int startHour, bool expected)
        => Assert.Equal(expected, StatsViewModel.WhileSleeping(NightWindow.Default, startHour));

    [Fact]
    public void Without_night_hours_nothing_is_while_you_sleep()
    {
        Assert.False(StatsViewModel.WhileSleeping(null, 23));
        Assert.False(StatsViewModel.WhileSleeping(new NightWindow(new TimeOnly(7, 0), new TimeOnly(7, 0)), 23));
    }

    [Fact]
    public async Task A_worst_window_at_night_gets_the_while_you_sleep_wording()
    {
        var viewModel = await CreateAsync();
        List<HourOfDayAverage> hours = [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is 23 or 0 or 1 ? 1350 : 700, 1))];

        viewModel.Update(Week([Day(0, 800)]) with { TypicalDay = hours, Night = NightWindow.Default });

        Assert.Equal("Insight_WorstHours_Night(from=23:00, to=02:00, value=1350 ppm)", viewModel.InsightText);
    }

    [Fact]
    public async Task A_worst_window_by_day_keeps_the_open_a_window_wording_even_with_night_hours_known()
    {
        var viewModel = await CreateAsync();
        List<HourOfDayAverage> hours = [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is 14 or 15 or 16 ? 1350 : 700, 1))];

        viewModel.Update(Week([Day(0, 800)]) with { TypicalDay = hours, Night = NightWindow.Default });

        Assert.StartsWith("Insight_WorstHours(", viewModel.InsightText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_highest_and_lowest_reading_have_their_day_and_time()
    {
        var viewModel = await CreateAsync();
        var bucket = Day(2, 900) with { Minimum = 450, Maximum = 1530, MinimumAt = start.AddDays(2).AddHours(4), MaximumAt = start.AddDays(2).AddHours(14).AddMinutes(16) };

        viewModel.Update(Week([Day(0, 800), bucket]));

        Assert.Equal("1530 ppm", viewModel.HighestText);
        Assert.Equal("Sat 3 Oct, 14:16", viewModel.HighestWhen);
        Assert.Equal("Sat 3 Oct, 04:00", viewModel.LowestWhen);
    }

    [Fact]
    public async Task Without_data_there_is_no_time_for_the_extremes()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([]));

        Assert.Null(viewModel.HighestWhen);
        Assert.Null(viewModel.LowestWhen);
    }

    [Fact]
    public async Task Coverage_is_a_line_of_text_only_when_it_is_worth_a_note()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800, count: 8), Day(1, 800, count: 8)]));
        Assert.Equal("Coverage_Chip(percent=80%)", viewModel.CoverageText);

        viewModel.Update(Week([Day(0, 800), Day(1, 800)]));
        Assert.Null(viewModel.CoverageText);
    }

    [Fact]
    public async Task When_even_the_worst_hours_are_good_the_insight_says_so()
    {
        var viewModel = await CreateAsync();
        List<HourOfDayAverage> hours = [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is 8 or 9 or 10 ? 900 : 500, 1))];

        viewModel.Update(Week([Day(0, 600)]) with { TypicalDay = hours });

        Assert.Equal("Insight_AllGood(from=08:00, to=11:00, value=900 ppm)", viewModel.InsightText);
    }

    [Fact]
    public async Task There_is_no_co2_insight_without_a_typical_day()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800)]));
        Assert.Null(viewModel.InsightText);

        viewModel.Update(Week([Day(0, 800)], metric: Metric.Temperature) with { TypicalDay = [] });
        Assert.Null(viewModel.InsightText);
    }

    [Fact]
    public async Task Pressure_shows_its_biggest_change_with_the_start_and_one_line_about_the_weather()
    {
        var viewModel = await CreateAsync();
        var change = new PressureChange(start.AddHours(6), start.AddHours(30), 1012, 1008);

        viewModel.Update(Week([Day(0, 1010)], metric: Metric.Pressure) with { PressureChange = change });

        Assert.True(viewModel.HasBiggestChange);
        Assert.StartsWith("▼ ", viewModel.BiggestChangeText);
        Assert.Contains("Thu 1 Oct, 06:00", viewModel.BiggestChangeWhen);
        Assert.NotNull(viewModel.PressureRangeText);
        Assert.Equal("Insight_Pressure", viewModel.InsightText);
    }

    [Fact]
    public async Task The_other_metrics_have_no_pressure_rows()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800)]));

        Assert.False(viewModel.HasBiggestChange);
        Assert.Null(viewModel.PressureRangeText);
    }

    [Fact]
    public async Task The_header_names_the_period_by_its_first_and_last_day()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1000), Day(1, 1000), Day(2, 1000)]));

        Assert.Equal("Stats_Header_Average(range=1 Oct – 3 Oct 2026)", viewModel.HeaderText);
    }

    [Fact]
    public async Task The_header_names_both_years_when_the_period_crosses_a_new_year()
    {
        var viewModel = await CreateAsync();
        var december = new Bucket(new DateTimeOffset(2025, 12, 30, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero), 1000, 1000, 1000, 10, 10, BucketState.Complete);
        var january = new Bucket(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero), 1000, 1000, 1000, 10, 10, BucketState.Complete);

        viewModel.Update(Week([december, january]));

        Assert.Equal("Stats_Header_Average(range=30 Dec 2025 – 2 Jan 2026)", viewModel.HeaderText);
    }

    [Fact]
    public async Task The_header_of_a_single_day_is_that_day()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(4, 1000)]) with { Range = Period.Day });

        Assert.Equal("Stats_Header_Average(range=5 Oct 2026)", viewModel.HeaderText);
    }

    [Fact]
    public async Task Without_buckets_there_is_no_header()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([]));

        Assert.Null(viewModel.HeaderText);
    }

    [Fact]
    public async Task The_big_number_is_the_average_without_its_unit()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1013)]));

        Assert.Equal("1013", viewModel.AverageNumber);
        Assert.Equal("1013 ppm", viewModel.AverageText);
        Assert.Equal("ppm", viewModel.UnitText);
    }

    [Theory]
    [InlineData(800, Zone.Good)]
    [InlineData(1200, Zone.Elevated)]
    [InlineData(1600, Zone.High)]
    public async Task The_insight_box_takes_the_zone_of_the_average(double average, Zone expected)
    {
        var viewModel = await CreateAsync();
        List<HourOfDayAverage> hours = [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, 700, 1))];

        viewModel.Update(Week([Day(0, average)]) with { TypicalDay = hours });

        Assert.Equal(expected, viewModel.InsightZone);
    }

    [Fact]
    public async Task Without_an_insight_there_is_no_insight_zone()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 1200)]));

        Assert.Null(viewModel.InsightZone);
    }

    private static List<HourOfDayAverage> TypicalDay(Func<int, double> value)
        => [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, value(hour), 1))];

    [Fact]
    public async Task A_cold_night_gives_the_coolest_hours_tip_while_you_sleep()
    {
        var viewModel = await CreateAsync();
        var hours = TypicalDay(hour => hour is >= 1 and <= 4 ? 17 : 22);

        viewModel.Update(Week([Day(0, 20)], metric: Metric.Temperature) with { TypicalDay = hours, Night = NightWindow.Default });

        Assert.Equal(Zone.Elevated, viewModel.InsightZone);
        Assert.StartsWith("Insight_Coolest_Night(from=01:00, to=04:00", viewModel.InsightText);
    }

    [Fact]
    public async Task A_warm_afternoon_gives_the_warmest_hours_tip_and_a_humid_evening_the_most_humid()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 24)], metric: Metric.Temperature) with { TypicalDay = TypicalDay(hour => hour is >= 13 and <= 16 ? 27 : 22), Night = NightWindow.Default });
        Assert.StartsWith("Insight_Warmest(from=13:00, to=16:00", viewModel.InsightText);

        viewModel.Update(Week([Day(0, 45)], metric: Metric.Humidity) with { TypicalDay = TypicalDay(hour => hour is >= 18 and <= 20 ? 65 : 40), Night = NightWindow.Default });
        Assert.StartsWith("Insight_MostHumid(from=18:00, to=21:00", viewModel.InsightText);
    }

    [Fact]
    public async Task A_comfortable_day_says_so_in_the_good_colours()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 22)], metric: Metric.Temperature) with { TypicalDay = TypicalDay(_ => 22) });

        Assert.Equal(Zone.Good, viewModel.InsightZone);
        Assert.StartsWith("Insight_Comfortable(range=", viewModel.InsightText);
    }

    [Theory]
    [InlineData(Metric.Temperature, 22, "Comfort_Status_Comfortable", Zone.Good)]
    [InlineData(Metric.Temperature, 19, "Comfort_Status_ABitCool", Zone.Elevated)]
    [InlineData(Metric.Temperature, 16, "Comfort_Status_TooCool", Zone.Elevated)]
    [InlineData(Metric.Temperature, 25, "Comfort_Status_ABitWarm", Zone.Elevated)]
    [InlineData(Metric.Temperature, 27, "Comfort_Status_TooWarm", Zone.Elevated)]
    [InlineData(Metric.Humidity, 40, "Comfort_Status_Comfortable", Zone.Good)]
    [InlineData(Metric.Humidity, 27, "Comfort_Status_ABitDry", Zone.Elevated)]
    [InlineData(Metric.Humidity, 20, "Comfort_Status_TooDry", Zone.Elevated)]
    [InlineData(Metric.Humidity, 55, "Comfort_Status_ABitHumid", Zone.Elevated)]
    [InlineData(Metric.Humidity, 70, "Comfort_Status_TooHumid", Zone.Elevated)]
    public async Task The_average_gets_a_comfort_pill_for_temperature_and_humidity(Metric metric, double average, string text, Zone zone)
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, average)], metric: metric));

        Assert.True(viewModel.HasComfort);
        Assert.Equal(text, viewModel.ComfortText);
        Assert.Equal(zone, viewModel.ComfortZone);
    }

    [Fact]
    public async Task Co2_and_pressure_have_no_comfort_pill()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800)]));
        Assert.False(viewModel.HasComfort);

        viewModel.Update(Week([Day(0, 1010)], metric: Metric.Pressure));
        Assert.False(viewModel.HasComfort);
    }

    [Fact]
    public async Task The_comfort_share_is_the_bar_with_its_range_and_palette()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 21)], metric: Metric.Temperature) with { Comfort = new ComfortShare(0.2, 0.7, 0.1) });

        Assert.True(viewModel.HasZoneShare);
        Assert.Equal(HourGridPalette.Temperature, viewModel.SharePalette);
        Assert.Equal((0.2, 0.7, 0.1), (viewModel.ZoneGood, viewModel.ZoneElevated, viewModel.ZoneHigh));
        Assert.Equal("Stats_ComfortShare", viewModel.ShareTitle);
        Assert.Equal(("Comfort_Cool", "Comfort_Comfortable", "Comfort_Warm"), (viewModel.ShareFirstLabel, viewModel.ShareSecondLabel, viewModel.ShareThirdLabel));
        Assert.NotNull(viewModel.ShareRange);
    }
}
