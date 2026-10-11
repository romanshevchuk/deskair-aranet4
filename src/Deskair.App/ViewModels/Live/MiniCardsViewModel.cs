using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Readings;

namespace Deskair.App.ViewModels.Live;

public sealed partial class MiniCardsViewModel : ObservableObject
{
    private readonly ILocalizer localizer;

    [ObservableProperty]
    private IReadOnlyList<MiniCardItem> items = [];

    public MiniCardsViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    // TODO(LIVE-02b) [S, haiku]
    // Spec: docs/design/components.md "Metric mini card"
    // Input : Recent readings.
    // Output: Items with name key, value text, subtitle and sparkline points for the three non-selected metrics.
    // Rules : Numbers formatted with the localizer culture.
    // Edge  : No readings gives items with empty values.
    // Done  : Add Tests/ViewModels/MiniCardsViewModelTests.
    public void Update(IReadOnlyList<Reading> recent)
        => throw new NotImplementedException("TODO(LIVE-02b)");
}
