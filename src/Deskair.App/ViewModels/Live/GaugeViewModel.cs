using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskair.App.ViewModels.Live;

public sealed partial class GaugeViewModel : ObservableObject
{
    private const double ScaleMinimum = 400;
    private const double ScaleMaximum = 2000;

    [ObservableProperty]
    private double? value;

    public void Update(double? co2Ppm)
    {
        Value = co2Ppm is { } ppm ? Math.Clamp(ppm, ScaleMinimum, ScaleMaximum) : null;
    }
}
