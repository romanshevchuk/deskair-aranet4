using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Metrics;
using Deskair.Services.Devices;
using Deskair.Services.Queries;

namespace Deskair.App.ViewModels.Live;

public sealed partial class ChartCardViewModel : ObservableObject
{
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;

    [ObservableProperty]
    private IReadOnlyList<ChartPoint>? points;

    public ChartCardViewModel(ReadingQueryService query, DeviceRegistry devices)
    {
        this.query = query;
        this.devices = devices;
    }

    // TODO(LIVE-03a) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Sync and data" rule 4; docs/design/screens-and-states.md "Screens" (Live)
    // Input : A metric, a range (1h, 6h, 24h), a CancellationToken.
    // Output: Points for the LiveChart from ReadingQueryService.GetRecentAsync.
    // Rules : Gaps stay gaps (null values), never interpolated.
    // Edge  : No selected device gives an empty list.
    // Done  : Add Tests/ViewModels/ChartCardViewModelTests with a fake query.
    public Task RefreshAsync(Metric metric, LiveRange range, CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(LIVE-03a)");
}
