using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Devices;

namespace Deskair.App.ViewModels.Settings;

public sealed partial class DevicesSettingsViewModel : ObservableObject
{
    private readonly DeviceRegistry devices;

    [ObservableProperty]
    private IReadOnlyList<DeviceInfo> items = [];

    public DevicesSettingsViewModel(DeviceRegistry devices)
    {
        this.devices = devices;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        Items = await devices.LoadAsync(cancellationToken);
    }
}
