using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

/// <summary>The parts of the stats card that need more than the current period: change, best and worst period, zone share and the insight.</summary>
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
    public async Task The_best_and_worst_day_are_the_lowest_and_highest_average()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800), Day(1, 500), Day(2, 1200)]));

        Assert.True(viewModel.HasPeriods);
        Assert.Equal("Stats_BestPeriod(period=Stats_Period_Day)", viewModel.BestLabel);
        Assert.Equal("500 ppm", viewModel.BestText);
        Assert.Equal("2 Oct", viewModel.BestWhen);
        Assert.Equal("Stats_WorstPeriod(period=Stats_Period_Day)", viewModel.WorstLabel);
        Assert.Equal("1,200 ppm", viewModel.WorstText);
        Assert.Equal("3 Oct", viewModel.WorstWhen);
    }

    [Fact]
    public async Task Other_metrics_say_lowest_and_highest_instead_of_best_and_worst()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 20), Day(1, 24)], metric: Metric.Temperature));

        Assert.Equal("Stats_LowestPeriod(period=Stats_Period_Day)", viewModel.BestLabel);
        Assert.Equal("Stats_HighestPeriod(period=Stats_Period_Day)", viewModel.WorstLabel);
    }

    [Fact]
    public async Task A_week_is_named_by_its_first_and_last_day()
    {
        var viewModel = await CreateAsync();
        var input = new StatsInput([
            new Bucket(start, start.AddDays(7), 600, 600, 600, 10, 10, BucketState.Complete),
            new Bucket(start.AddDays(7), start.AddDays(14), 900, 900, 900, 10, 10, BucketState.Complete)
        ])
        {
            Range = Period.Month,
            Grouping = Period.Week
        };

        viewModel.Update(input);

        Assert.Equal("1 Oct – 7 Oct", viewModel.BestWhen);
        Assert.Equal("Stats_BestPeriod(period=Stats_Period_Week)", viewModel.BestLabel);
    }

    [Fact]
    public async Task A_week_of_one_day_is_named_by_that_day()
    {
        var viewModel = await CreateAsync();
        var input = new StatsInput([
            new Bucket(start, start.AddDays(1), 600, 600, 600, 10, 10, BucketState.Complete),
            new Bucket(start.AddDays(1), start.AddDays(8), 900, 900, 900, 10, 10, BucketState.Complete)
        ])
        {
            Range = Period.Month,
            Grouping = Period.Week
        };

        viewModel.Update(input);

        Assert.Equal("1 Oct", viewModel.BestWhen);
        Assert.Equal("2 Oct – 8 Oct", viewModel.WorstWhen);
    }

    [Fact]
    public async Task One_complete_period_or_a_single_day_has_no_best_and_worst()
    {
        var viewModel = await CreateAsync();

        viewModel.Update(Week([Day(0, 800), Day(1, 900, BucketState.Partial, 2)]));
        Assert.False(viewModel.HasPeriods);

        viewModel.Update(new StatsInput([Day(0, 800), Day(1, 900)]) { Range = Period.Day });
        Assert.False(viewModel.HasPeriods);
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

        Assert.Equal("Insight_WorstHours(from=14:00, to=17:00, value=1,350 ppm)", viewModel.InsightText);
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
    public async Task There_is_no_insight_without_a_typical_day_or_for_other_metrics()
    {
        var viewModel = await CreateAsync();
        List<HourOfDayAverage> hours = [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, 800, 1))];

        viewModel.Update(Week([Day(0, 800)]));
        Assert.Null(viewModel.InsightText);

        viewModel.Update(Week([Day(0, 20)], metric: Metric.Temperature) with { TypicalDay = hours });
        Assert.Null(viewModel.InsightText);
    }
}
