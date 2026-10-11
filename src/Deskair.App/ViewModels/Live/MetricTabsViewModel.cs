using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Metrics;

namespace Deskair.App.ViewModels.Live;

public sealed partial class MetricTabsViewModel : ObservableObject
{
    [ObservableProperty]
    private Metric selectedMetric;

    [ObservableProperty]
    private LiveRange selectedRange;

    // TODO(LIVE-02a) [S, haiku]
    // Spec: docs/design/screens-and-states.md "Live conditions" (4 metrics, 3 ranges)
    // Input : A metric and a range.
    // Output: Updates SelectedMetric and SelectedRange.
    // Rules : The ribbon is shown for CO2 on 1h and 6h only; expose a ShowRibbon property.
    // Edge  : Selecting the same values changes nothing.
    // Done  : Add Tests/ViewModels/MetricTabsViewModelTests.
    public void Select(Metric metric, LiveRange range)
        => throw new NotImplementedException("TODO(LIVE-02a)");
}
