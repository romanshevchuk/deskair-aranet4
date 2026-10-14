using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Connection;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Connection;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Queries;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class OfflineViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);
    private static readonly DeviceId device = new("test", "A1");
    private static readonly DeviceId other = new("test", "B2");

    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeBluetoothRadio radio = new();
    private readonly FakeReadingStore store = new();
    private readonly FakeSystemSettings systemSettings = new();
    private readonly DeviceRegistry registry;
    private readonly FakeDeviceStore deviceStore = new();
    private readonly FakeClock clock = new(now);

    public OfflineViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        deviceStore.Devices.Add(new DeviceInfo(device, "Office", null, [], null));
    }

    private async Task<OfflineViewModel> CreateAsync()
    {
        await registry.LoadAsync(TestContext.Current.CancellationToken);
        var localizer = new FakeLocalizer();
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);

        return new OfflineViewModel(
            registry,
            new ReadingQueryService(store, registry, clock, calendar),
            radio,
            messenger,
            new FakeUiDispatcher(),
            TestFormatter.Create(localizer),
            localizer,
            new FriendlyTime(localizer, calendar, clock),
            systemSettings);
    }

    [Fact]
    public async Task With_Bluetooth_on_the_message_blames_range_or_sleep()
    {
        var viewModel = await CreateAsync();

        Assert.False(viewModel.BluetoothOff);
        Assert.Equal("Offline_Text Offline_HistoryHint", viewModel.Message);
    }

    [Fact]
    public async Task With_Bluetooth_off_the_message_says_so()
    {
        radio.IsOn = false;
        var viewModel = await CreateAsync();

        Assert.True(viewModel.BluetoothOff);
        Assert.StartsWith("Offline_Text_BluetoothOff", viewModel.Message);
    }

    [Fact]
    public async Task Turning_Bluetooth_on_or_off_updates_the_view()
    {
        var viewModel = await CreateAsync();

        radio.IsOn = false;
        Assert.True(viewModel.BluetoothOff);

        radio.IsOn = true;
        Assert.False(viewModel.BluetoothOff);
    }

    [Fact]
    public async Task A_reading_is_remembered_with_its_time()
    {
        var viewModel = await CreateAsync();

        messenger.Send(new ReadingReceived(device, Samples.Co2(now.AddMinutes(-20), 1068)));

        Assert.Equal("Offline_LastReading(value=1068 ppm, time=11:40)", viewModel.LastReadingText);
        Assert.Equal("Offline_Text Offline_LastReading(value=1068 ppm, time=11:40) Offline_HistoryHint", viewModel.Message);
    }

    [Fact]
    public async Task A_reading_from_an_earlier_day_names_the_day()
    {
        var viewModel = await CreateAsync();

        messenger.Send(new ReadingReceived(device, Samples.Co2(now.AddDays(-2), 900)));

        Assert.Equal("Offline_LastReading(value=900 ppm, time=10 Oct, 12:00)", viewModel.LastReadingText);
    }

    [Fact]
    public async Task A_reading_of_another_room_is_ignored()
    {
        var viewModel = await CreateAsync();

        messenger.Send(new ReadingReceived(other, Samples.Co2(now, 1068)));

        Assert.Null(viewModel.LastReadingText);
    }

    [Fact]
    public async Task When_the_sensor_cannot_be_reached_from_the_start_the_last_stored_reading_is_shown()
    {
        store.Readings.AddRange([Samples.Co2(now.AddHours(-5), 700), Samples.Co2(now.AddHours(-3), 950)]);
        var viewModel = await CreateAsync();

        messenger.Send(new ConnectionStateChanged(ConnectionState.NotConnected));

        await Wait.Until(() => viewModel.LastReadingText is not null);
        Assert.Equal("Offline_LastReading(value=950 ppm, time=09:00)", viewModel.LastReadingText);
    }

    [Fact]
    public async Task Another_room_forgets_the_last_reading_of_the_first()
    {
        var viewModel = await CreateAsync();
        messenger.Send(new ReadingReceived(device, Samples.Co2(now, 1068)));

        messenger.Send(new DeviceSelected(other));

        Assert.Null(viewModel.LastReadingText);
    }

    [Fact]
    public async Task The_steps_mention_the_battery_once_it_is_known()
    {
        var viewModel = await CreateAsync();
        Assert.Equal(["Offline_Step_Range", "Offline_Step_Wake"], viewModel.Steps);

        messenger.Send(new SensorStatusUpdated(device, new SensorStatus(TimeSpan.FromMinutes(2), 65, null)));

        Assert.Equal(["Offline_Step_Range", "Offline_Step_Wake", "Offline_Step_Battery(percent=65%)"], viewModel.Steps);
    }

    [Fact]
    public async Task A_new_language_rebuilds_the_text()
    {
        var viewModel = await CreateAsync();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        messenger.Send(new LanguageChanged("uk"));

        Assert.Contains(nameof(OfflineViewModel.Message), changed);
        Assert.Contains(nameof(OfflineViewModel.Steps), changed);
    }

    [Fact]
    public async Task Turn_on_Bluetooth_opens_the_system_page()
    {
        var viewModel = await CreateAsync();

        viewModel.TurnOnBluetoothCommand.Execute(null);

        Assert.Equal(1, systemSettings.BluetoothOpened);
    }

    [Fact]
    public async Task Try_again_asks_the_app_to_retry()
    {
        var viewModel = await CreateAsync();
        var requests = 0;
        messenger.Register<RetryRequested>(this, (_, _) => requests++);

        viewModel.RetryCommand.Execute(null);

        Assert.Equal(1, requests);
    }
}
