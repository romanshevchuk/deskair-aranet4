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

    // TODO(SET-01f) [M, haiku]
    // Spec: docs/design/screens-and-states.md "Settings" (Devices)
    // Input : A CancellationToken.
    // Output: The list of known devices from DeviceRegistry.
    // Rules : Rename and add commands are separate cards once DeviceRegistry supports them.
    // Edge  : No devices gives an empty list.
    // Done  : Add Tests/ViewModels/DevicesSettingsViewModelTests.
    public Task LoadAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SET-01f)");
}
