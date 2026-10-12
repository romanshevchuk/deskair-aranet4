using Deskair.App.ViewModels.Settings;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class DevicesSettingsViewModelTests
{
    private readonly FakeDeviceStore store = new();

    [Fact]
    public async Task Load_lists_the_known_devices()
    {
        store.Devices.Add(new DeviceInfo(new DeviceId("test", "A1"), "Office", null, [], null));
        store.Devices.Add(new DeviceInfo(new DeviceId("test", "B2"), "Bedroom", "bedroom", [], null));
        var viewModel = new DevicesSettingsViewModel(new DeviceRegistry(store));

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Office", "Bedroom"], viewModel.Items.Select(item => item.RoomName));
    }

    [Fact]
    public async Task No_devices_gives_an_empty_list()
    {
        var viewModel = new DevicesSettingsViewModel(new DeviceRegistry(store));

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(viewModel.Items);
    }
}
