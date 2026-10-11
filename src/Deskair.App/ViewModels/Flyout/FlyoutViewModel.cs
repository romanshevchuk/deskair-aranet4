using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Flyout;

public sealed partial class FlyoutViewModel : ObservableObject
{
    private readonly ILocalizer localizer;

    [ObservableProperty]
    private Zone? zone;

    [ObservableProperty]
    private double? co2Ppm;

    public FlyoutViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    // TODO(TRAY-02c) [M, haiku]
    // Spec: docs/design/screens-and-states.md "Screens" (Flyout)
    // Input : The latest reading or null.
    // Output: Zone and value for the flyout, plus localized room and advice text.
    // Rules : Share logic with HeroViewModel rather than copying it.
    // Edge  : Null reading shows the not-connected state.
    // Done  : Add Tests/ViewModels/FlyoutViewModelTests.
    public void Update(Reading? reading)
        => throw new NotImplementedException("TODO(TRAY-02c)");
}
