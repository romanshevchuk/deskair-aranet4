using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

public sealed partial class HistoryToolbarViewModel : ObservableObject
{
    [ObservableProperty]
    private Metric metric;

    [ObservableProperty]
    private Period range;

    [ObservableProperty]
    private HoursFilterMode hours;

    [ObservableProperty]
    private Period grouping;

    // TODO(HIST-01b) [S, haiku]
    // Spec: docs/design/screens-and-states.md "History controls" (Group by)
    // Input : The current Range.
    // Output: The groupings allowed: Month gives day and week; Year gives week and month; Day and Week give none.
    // Rules : Pure function of Range.
    // Edge  : If the current Grouping is not allowed after a range change, the first allowed one is chosen.
    // Done  : Add Tests/ViewModels/HistoryToolbarViewModelTests.
    public IReadOnlyList<Period> GroupingOptions()
        => throw new NotImplementedException("TODO(HIST-01b)");
}
