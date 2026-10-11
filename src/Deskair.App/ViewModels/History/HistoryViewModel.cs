using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.ViewModels.History;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryToolbarViewModel toolbar;
    private readonly StatsViewModel stats;
    private readonly TypicalDayViewModel typicalDay;

    [ObservableProperty]
    private IReadOnlyList<ChartBar>? bars;

    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    [ObservableProperty]
    private IReadOnlyList<ChartGap>? gaps;

    public HistoryViewModel(HistoryToolbarViewModel toolbar, StatsViewModel stats, TypicalDayViewModel typicalDay)
    {
        this.toolbar = toolbar;
        this.stats = stats;
        this.typicalDay = typicalDay;
    }

    // TODO(HIST-00a) [M, sonnet]
    // Spec: docs/engineering/architecture.md section 7; docs/design/screens-and-states.md "History controls"
    // Input : A CancellationToken; toolbar selections.
    // Output: Queries ReadingQueryService for the selection, then fills bars, points, gaps and the child view models.
    // Rules : Long calculations run off the UI thread (architecture.md section 11).
    // Edge  : A newer refresh cancels the previous one.
    // Done  : Add Tests/ViewModels/HistoryViewModelTests with fake services.
    public Task RefreshAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(HIST-00a)");
}
