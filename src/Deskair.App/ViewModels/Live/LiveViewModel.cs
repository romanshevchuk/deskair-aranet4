using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Metrics;

namespace Deskair.App.ViewModels.Live;

public sealed partial class LiveViewModel : ObservableObject
{
    private readonly HeroViewModel hero;
    private readonly GaugeViewModel gauge;
    private readonly MetricTabsViewModel tabs;
    private readonly ChartCardViewModel chartCard;
    private readonly MiniCardsViewModel miniCards;

    public LiveViewModel(HeroViewModel hero, GaugeViewModel gauge, MetricTabsViewModel tabs, ChartCardViewModel chartCard, MiniCardsViewModel miniCards)
    {
        this.hero = hero;
        this.gauge = gauge;
        this.tabs = tabs;
        this.chartCard = chartCard;
        this.miniCards = miniCards;
    }

    // TODO(LIVE-00a) [M, sonnet]
    // Spec: docs/engineering/architecture.md sections 6 and 7
    // Input : A CancellationToken.
    // Output: Child view models are loaded and subscribe to ReadingReceived and ZoneChanged through IMessenger; UI updates go through IUiDispatcher.
    // Rules : About 150 lines at most; no System.Windows.
    // Edge  : Cancellation unsubscribes cleanly.
    // Done  : Add Tests/ViewModels/LiveViewModelTests with fake services.
    public Task StartAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(LIVE-00a)");
}
