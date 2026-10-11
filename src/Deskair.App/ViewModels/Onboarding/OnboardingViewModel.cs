using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Devices;
using Deskair.Services.Sensors;

namespace Deskair.App.ViewModels.Onboarding;

public sealed partial class OnboardingViewModel : ObservableObject
{
    private readonly IEnumerable<ISensorProvider> providers;
    private readonly DeviceRegistry devices;

    [ObservableProperty]
    private int stepIndex;

    [ObservableProperty]
    private IReadOnlyList<DiscoveredSensor> nearby = [];

    [ObservableProperty]
    private string? roomName;

    public OnboardingViewModel(IEnumerable<ISensorProvider> providers, DeviceRegistry devices)
    {
        this.providers = providers;
        this.devices = devices;
    }

    // TODO(FIRST-01a) [L, sonnet]
    // Spec: docs/design/screens-and-states.md "Live conditions" (First run); docs/engineering/sync-and-import.md
    // Input : A CancellationToken.
    // Output: Nearby sensors from every provider, collected as they are found.
    // Rules : Scanning stops on cancellation or when the step changes.
    // Edge  : Bluetooth off ends with a state the view shows as the Bluetooth hint.
    // Done  : Add Tests/ViewModels/OnboardingViewModelTests with the simulated provider.
    public Task ScanAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(FIRST-01a)");
}
