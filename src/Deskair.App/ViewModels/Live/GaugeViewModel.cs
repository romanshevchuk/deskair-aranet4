using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskair.App.ViewModels.Live;

public sealed partial class GaugeViewModel : ObservableObject
{
    [ObservableProperty]
    private double? value;

    // TODO(LIVE-01d) [S, haiku]
    // Spec: docs/design/components.md "Gauge"
    // Input : The CO2 value or null.
    // Output: Value for the Gauge control, clamped to its scale.
    // Rules : Pass-through plus clamping only.
    // Edge  : Null clears the value.
    // Done  : Add Tests/ViewModels/GaugeViewModelTests.
    public void Update(double? co2Ppm)
        => throw new NotImplementedException("TODO(LIVE-01d)");
}
