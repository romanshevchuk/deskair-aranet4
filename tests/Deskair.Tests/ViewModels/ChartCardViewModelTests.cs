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
    private readonly ChartCardViewModel viewModel;

    public ChartCardViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        var query = new ReadingQueryService(store, registry, new FakeClock(now), new LocalCalendar(TimeZoneInfo.Utc));
        viewModel = new ChartCardViewModel(query, registry);
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
}
