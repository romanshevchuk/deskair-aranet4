using Deskair.App.ViewModels.Live;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Queries;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class ChartCardViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    private readonly FakeReadingStore store = new();
    private readonly FakeDeviceStore deviceStore = new();
    private readonly DeviceRegistry registry;
    private readonly FakeLocalizer localizer = new();
    private readonly ChartCardViewModel viewModel;

    public ChartCardViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        viewModel = Create(Deskair.Domain.Metrics.TemperatureUnit.Celsius);
    }

    private ChartCardViewModel Create(TemperatureUnit unit)
    {
        var clock = new FakeClock(now);
        var query = new ReadingQueryService(store, registry, clock, new LocalCalendar(TimeZoneInfo.Utc));

        return new ChartCardViewModel(query, registry, TestFormatter.Create(localizer, unit), localizer, clock);
    }

    private async Task SelectDeviceAsync()
    {
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(now.AddYears(-1), interval)], null));
        await registry.LoadAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task No_selected_device_gives_an_empty_list()
    {
        store.Readings.Add(Samples.Co2(now.AddMinutes(-2), 800));

        await viewModel.RefreshAsync(Metric.Co2, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel.Points);
        Assert.Empty(viewModel.Points);
        Assert.Null(viewModel.SummaryText);
    }

    [Fact]
    public async Task Points_carry_the_selected_metric()
    {
        await SelectDeviceAsync();
        store.Readings.Add(Samples.Climate(now.AddMinutes(-4), 21.5, 40, 1000));
        store.Readings.Add(Samples.Climate(now.AddMinutes(-2), 22.5, 41, 1001));

        await viewModel.RefreshAsync(Metric.Temperature, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Equal([21.5, 22.5], viewModel.Points!.Select(point => point.Value));
        Assert.Equal([now.AddMinutes(-4), now.AddMinutes(-2)], viewModel.Points!.Select(point => point.Time));
    }

    [Theory]
    [InlineData(LiveRange.OneHour, 1)]
    [InlineData(LiveRange.SixHours, 6)]
    [InlineData(LiveRange.TwentyFourHours, 24)]
    public async Task Range_sets_the_window(LiveRange range, int hours)
    {
        await SelectDeviceAsync();

        await viewModel.RefreshAsync(Metric.Co2, range, TestContext.Current.CancellationToken);

        Assert.Equal(now.AddHours(-hours), store.Requested!.Value.Start);
        Assert.Equal(now, store.Requested!.Value.End);
        Assert.Equal(TimeSpan.FromHours(hours), viewModel.Window);
        Assert.Equal(now, viewModel.EndTime);
    }

    [Fact]
    public async Task A_missing_stretch_becomes_a_null_point_not_a_line()
    {
        await SelectDeviceAsync();
        store.Readings.Add(Samples.Co2(now.AddMinutes(-30), 700));
        store.Readings.Add(Samples.Co2(now.AddMinutes(-28), 710));
        store.Readings.Add(Samples.Co2(now.AddMinutes(-10), 900));

        await viewModel.RefreshAsync(Metric.Co2, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Equal([700, 710, null, 900], viewModel.Points!.Select(point => point.Value));
        Assert.Equal(now.AddMinutes(-26), viewModel.Points![2].Time);
    }

    [Fact]
    public async Task Normal_spacing_adds_no_gap_points()
    {
        await SelectDeviceAsync();
        store.Readings.AddRange(Enumerable.Range(1, 5).Select(i => Samples.Co2(now.AddMinutes(-2 * i), 600 + i)));

        await viewModel.RefreshAsync(Metric.Co2, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Equal(5, viewModel.Points!.Count);
        Assert.All(viewModel.Points, point => Assert.NotNull(point.Value));
    }

    [Fact]
    public async Task Temperature_points_and_the_ideal_range_are_in_the_selected_unit()
    {
        await SelectDeviceAsync();
        store.Readings.Add(Samples.Climate(now.AddMinutes(-2), 22.0, 40, 1000));
        var fahrenheit = Create(Deskair.Domain.Metrics.TemperatureUnit.Fahrenheit);

        await fahrenheit.RefreshAsync(Metric.Temperature, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Equal(71.6, fahrenheit.Points![0].Value!.Value, 6);
        Assert.Equal("°F", fahrenheit.Unit);
        Assert.Equal(68, fahrenheit.IdealMin);
        Assert.Equal(75.2, fahrenheit.IdealMax!.Value, 6);
    }

    [Theory]
    [InlineData(Metric.Co2, null, null)]
    [InlineData(Metric.Pressure, null, null)]
    [InlineData(Metric.Humidity, 30.0, 50.0)]
    public async Task The_ideal_range_comes_from_the_metric(Metric metric, double? min, double? max)
    {
        await SelectDeviceAsync();

        await viewModel.RefreshAsync(metric, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Equal(min, viewModel.IdealMin);
        Assert.Equal(max, viewModel.IdealMax);
        Assert.Equal(min is null ? null : "Chart_Ideal(min=30, max=50)", viewModel.IdealLabel);
    }

    [Fact]
    public async Task The_summary_has_low_average_and_high_with_the_unit()
    {
        await SelectDeviceAsync();
        store.Readings.AddRange([Samples.Co2(now.AddMinutes(-6), 1000), Samples.Co2(now.AddMinutes(-4), 1500), Samples.Co2(now.AddMinutes(-2), 1100)]);

        await viewModel.RefreshAsync(Metric.Co2, LiveRange.SixHours, TestContext.Current.CancellationToken);

        Assert.Equal("Chart_Summary_SixHours(low=1000, avg=1200, high=1500, unit=ppm)", viewModel.SummaryText);
    }

    [Fact]
    public async Task The_ribbon_shows_the_last_day_whatever_range_is_selected()
    {
        await SelectDeviceAsync();
        store.Readings.AddRange([Samples.Co2(now.AddHours(-20), 700), Samples.Co2(now.AddMinutes(-5), 1500)]);

        await viewModel.RefreshAsync(Metric.Co2, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Equal(2, viewModel.RibbonSegments.Count);
        Assert.Equal(Deskair.Domain.Zones.Zone.Good, viewModel.RibbonSegments[0].Zone);
        Assert.Equal(Deskair.Domain.Zones.Zone.High, viewModel.RibbonSegments[1].Zone);
        Assert.Equal(1, viewModel.Points!.Count(point => point.Value is not null));
    }

    [Fact]
    public async Task Without_a_device_the_ribbon_is_empty()
    {
        await viewModel.RefreshAsync(Metric.Co2, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Empty(viewModel.RibbonSegments);
    }

    [Fact]
    public async Task No_readings_give_no_summary()
    {
        await SelectDeviceAsync();

        await viewModel.RefreshAsync(Metric.Co2, LiveRange.OneHour, TestContext.Current.CancellationToken);

        Assert.Empty(viewModel.Points!);
        Assert.Null(viewModel.SummaryText);
    }
}
