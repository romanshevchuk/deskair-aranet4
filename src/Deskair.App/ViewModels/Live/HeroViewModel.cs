using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Live;

public sealed partial class HeroViewModel : ObservableObject
{
    private readonly ILocalizer localizer;

    [ObservableProperty]
    private Zone? zone;

    [ObservableProperty]
    private double? co2Ppm;

    [ObservableProperty]
    private TrendDirection trend;

    [ObservableProperty]
    private string? trendText;

    [ObservableProperty]
    private string? adviceTitle;

    [ObservableProperty]
    private string? adviceText;

    public HeroViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    // TODO(LIVE-01c) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Live values"; docs/design/localization.md (Advice_*, Trend_* keys)
    // Input : The latest reading, recent readings, the current instant.
    // Output: Zone from ZoneClassifier, trend from TrendCalculator, and localized trend and advice text.
    // Rules : No calculation here beyond calling Domain; text only through ILocalizer.
    // Edge  : No reading clears all fields.
    // Done  : Add Tests/ViewModels/HeroViewModelTests with a fake localizer.
    public void Update(Reading reading, IReadOnlyList<Reading> recent, DateTimeOffset now)
        => throw new NotImplementedException("TODO(LIVE-01c)");
}
