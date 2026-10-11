using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.TypicalDay;

namespace Deskair.App.ViewModels.History;

public sealed partial class TypicalDayViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<HourBar> bars = [];

    // TODO(HIST-05c) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 6
    // Input : The 24 hourly averages.
    // Output: HourBar items with zone per hour and the worst three-hour window flagged.
    // Rules : Use ZoneClassifier and TypicalDayCalculator.WorstWindowStartHour.
    // Edge  : Hours without data have a null average and zone.
    // Done  : Add Tests/ViewModels/TypicalDayViewModelTests.
    public void Update(IReadOnlyList<HourOfDayAverage> hours)
        => throw new NotImplementedException("TODO(HIST-05c)");
}
