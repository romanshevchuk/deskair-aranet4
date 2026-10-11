using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.ViewModels.History;

public sealed partial class StatsViewModel : ObservableObject
{
    [ObservableProperty]
    private double? average;

    [ObservableProperty]
    private double? highest;

    [ObservableProperty]
    private double? lowest;

    [ObservableProperty]
    private double coverage;

    // TODO(HIST-06b) [M, haiku]
    // Spec: docs/design/components.md "Stats card"; docs/product/behaviour-rules.md "Aggregation" rules 1, 3, 5
    // Input : Buckets of the selected range (and of the previous period for comparison).
    // Output: Average, highest, lowest and coverage for display.
    // Rules : Use only buckets with data; averages are weighted by reading count.
    // Edge  : All buckets NoData clears every value.
    // Done  : Add Tests/ViewModels/StatsViewModelTests.
    public void Update(IReadOnlyList<Bucket> buckets)
        => throw new NotImplementedException("TODO(HIST-06b)");
}
