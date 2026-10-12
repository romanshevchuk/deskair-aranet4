using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class RoomChipViewModelTests : IDisposable
{
    private static readonly DeviceId office = new("aranet4", "AA:01");
    private static readonly DeviceId bedroom = new("aranet4", "AA:02");
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);

    private readonly FakeDeviceStore deviceStore = new();
    private readonly FakeSettingsStore settingsStore = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly DeviceRegistry registry;
    private readonly SettingsService settings;
    private readonly RoomChipViewModel viewModel;

    public RoomChipViewModelTests()
    {
        registry = new DeviceRegistry(deviceStore);
        settings = new SettingsService(settingsStore, messenger);
        viewModel = new RoomChipViewModel(registry, settings, messenger, new FakeUiDispatcher(), TestFormatter.Create(new FakeLocalizer()));
        deviceStore.Devices.AddRange([new DeviceInfo(office, "Office", null, [], null), new DeviceInfo(bedroom, "Bedroom", null, [], null)]);
    }

    public void Dispose()
    {
        messenger.UnregisterAll(viewModel);
    }

    [Fact]
    public async Task Load_lists_the_rooms_and_selects_the_current_one()
    {
        await registry.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Load();

        Assert.Equal(["Office", "Bedroom"], viewModel.Rooms.Select(room => room.Name));
        Assert.Equal("Office", viewModel.SelectedRoom?.Name);
    }

    [Fact]
    public void Without_devices_there_are_no_rooms_and_nothing_selected()
    {
        deviceStore.Devices.Clear();

        viewModel.Load();

        Assert.Empty(viewModel.Rooms);
        Assert.Null(viewModel.SelectedRoom);
    }

    [Fact]
    public async Task Rooms_with_a_reading_show_value_and_zone()
    {
        await registry.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        viewModel.Start(CancellationToken.None);

        messenger.Send(new ReadingReceived(office, Samples.Co2(now, 1068)));

        Assert.Equal("1,068 ppm", viewModel.Rooms[0].ValueText);
        Assert.Equal(Zone.Elevated, viewModel.Rooms[0].Zone);
        Assert.Null(viewModel.Rooms[1].ValueText);
        Assert.Equal(Zone.Elevated, viewModel.SelectedRoom?.Zone);
    }

    [Fact]
    public async Task Choosing_a_room_selects_it_remembers_it_and_tells_the_app()
    {
        await registry.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        DeviceSelected? received = null;
        messenger.Register<DeviceSelected>(this, (_, message) => received = message);
        viewModel.IsOpen = true;

        await viewModel.ChooseRoomCommand.ExecuteAsync(viewModel.Rooms[1]);

        Assert.Equal(bedroom, registry.Selected);
        Assert.Equal(bedroom, settings.Current.SelectedDevice);
        Assert.Equal(bedroom, settingsStore.Saved.Last().SelectedDevice);
        Assert.Equal(bedroom, received?.Device);
        Assert.Equal("Bedroom", viewModel.SelectedRoom?.Name);
        Assert.False(viewModel.IsOpen);
    }

    [Fact]
    public async Task Choosing_the_room_that_is_already_selected_changes_nothing()
    {
        await registry.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        var announced = 0;
        messenger.Register<DeviceSelected>(this, (_, _) => announced++);

        await viewModel.ChooseRoomCommand.ExecuteAsync(viewModel.Rooms[0]);

        Assert.Equal(0, announced);
        Assert.Empty(settingsStore.Saved);
    }
}
