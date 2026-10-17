using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public sealed class DevicesSettingsViewModelTests : IDisposable
{
    private static readonly DeviceId office = new("test", "A1");
    private static readonly DeviceId bedroom = new("test", "B2");

    private readonly FakeDeviceStore store = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly DeviceRegistry registry;
    private readonly DevicesSettingsViewModel viewModel;
    private readonly CancellationTokenSource stop = new();

    public DevicesSettingsViewModelTests()
    {
        registry = new DeviceRegistry(store);
        var enrollment = new DeviceEnrollment(store, registry, new SettingsService(new FakeSettingsStore(), messenger), messenger);
        viewModel = new DevicesSettingsViewModel(registry, enrollment, messenger, NullLogger<DevicesSettingsViewModel>.Instance);
    }

    public void Dispose()
    {
        stop.Cancel();
        stop.Dispose();
    }

    private void AddTwo()
    {
        store.Devices.Add(new DeviceInfo(office, "Office", null, [], null));
        store.Devices.Add(new DeviceInfo(bedroom, "Bedroom", "bedroom", [], null));
    }

    [Fact]
    public async Task Load_lists_the_known_devices()
    {
        AddTwo();

        await viewModel.LoadAsync(stop.Token);

        Assert.Equal(["Office", "Bedroom"], viewModel.Items.Select(item => item.RoomName));
        Assert.Equal(["Office", "Bedroom"], viewModel.Rows.Select(row => row.Name));
        Assert.Equal(["A1", "B2"], viewModel.Rows.Select(row => row.Address));
        Assert.True(viewModel.HasDevices);
    }

    [Fact]
    public async Task No_devices_gives_an_empty_list()
    {
        await viewModel.LoadAsync(stop.Token);

        Assert.Empty(viewModel.Items);
        Assert.Empty(viewModel.Rows);
        Assert.False(viewModel.HasDevices);
    }

    [Fact]
    public async Task Typing_a_new_name_saves_it_and_announces_it()
    {
        AddTwo();
        await viewModel.LoadAsync(stop.Token);
        var renamed = new List<DeviceRenamed>();
        messenger.Register<DeviceRenamed>(this, (_, message) => renamed.Add(message));

        viewModel.Rows[0].Name = "Study";

        await Wait.Until(() => store.Snapshot()[0].RoomName == "Study");
        Assert.Equal(office, Assert.Single(renamed).Device);
        Assert.Equal("Study", viewModel.Rows[0].Name);
    }

    [Fact]
    public async Task A_blank_name_is_put_back()
    {
        AddTwo();
        await viewModel.LoadAsync(stop.Token);

        viewModel.Rows[0].Name = "   ";

        await Wait.Until(() => viewModel.Rows[0].Name == "Office");
        Assert.Equal("Office", store.Snapshot()[0].RoomName);
    }

    [Fact]
    public void Adding_asks_for_the_sensor_search()
    {
        var requests = new List<AddSensorRequested>();
        messenger.Register<AddSensorRequested>(this, (_, message) => requests.Add(message));

        viewModel.AddSensorCommand.Execute(null);

        Assert.Single(requests);
    }

    [Fact]
    public async Task A_newly_paired_sensor_shows_up_in_the_list()
    {
        await viewModel.LoadAsync(stop.Token);
        Assert.Empty(viewModel.Rows);

        store.Devices.Add(new DeviceInfo(office, "My room", null, [], null));
        messenger.Send(new DeviceSelected(office));

        await Wait.Until(() => viewModel.Rows.Count == 1);
        Assert.Equal("My room", viewModel.Rows[0].Name);
    }

    [Fact]
    public async Task Coming_back_to_Settings_follows_new_rooms_again()
    {
        using var firstVisit = new CancellationTokenSource();
        await viewModel.LoadAsync(firstVisit.Token);
        await firstVisit.CancelAsync();
        await viewModel.LoadAsync(stop.Token);

        store.Devices.Add(new DeviceInfo(office, "My room", null, [], null));
        messenger.Send(new DeviceSelected(office));

        await Wait.Until(() => viewModel.Rows.Count == 1);
    }
}
