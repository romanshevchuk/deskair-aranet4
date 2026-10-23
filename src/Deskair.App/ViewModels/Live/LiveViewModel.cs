using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Deskair.Services.Queries;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Live;

public sealed partial class LiveViewModel : ObservableObject
{
    private static readonly TimeSpan window = TimeSpan.FromHours(24);
    private static readonly TimeSpan miniCardWindow = TimeSpan.FromHours(1);

    private readonly HeroViewModel hero;
    private readonly GaugeViewModel gauge;
    private readonly MetricTabsViewModel tabs;
    private readonly ChartCardViewModel chartCard;
    private readonly MiniCardsViewModel miniCards;
    private readonly ReadingQueryService query;
    private readonly DeviceRegistry devices;
    private readonly IMessenger messenger;
    private readonly IUiDispatcher dispatcher;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<LiveViewModel> logger;
    private List<Reading> recent = [];
    private CancellationToken lifetime;
    private CancellationTokenSource? chartRefresh;

    public LiveViewModel(
        HeroViewModel hero,
        GaugeViewModel gauge,
        MetricTabsViewModel tabs,
        ChartCardViewModel chartCard,
        MiniCardsViewModel miniCards,
        ReadingQueryService query,
        DeviceRegistry devices,
        IMessenger messenger,
        IUiDispatcher dispatcher,
        TimeProvider timeProvider,
        ILogger<LiveViewModel> logger)
    {
        this.hero = hero;
        this.gauge = gauge;
        this.tabs = tabs;
        this.chartCard = chartCard;
        this.miniCards = miniCards;
        this.query = query;
        this.devices = devices;
        this.messenger = messenger;
        this.dispatcher = dispatcher;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public HeroViewModel Hero => hero;

    public GaugeViewModel Gauge => gauge;

    public MetricTabsViewModel Tabs => tabs;

    public ChartCardViewModel ChartCard => chartCard;

    public MiniCardsViewModel MiniCards => miniCards;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime = cancellationToken;
        messenger.Register<LiveViewModel, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        messenger.Register<LiveViewModel, DeviceSelected>(this, static (self, _) => self.OnReload());
        messenger.Register<LiveViewModel, SettingsChanged>(this, static (self, _) => self.OnReload());
        messenger.Register<LiveViewModel, SyncCompleted>(this, static (self, message) => self.OnSyncCompleted(message));
        tabs.PropertyChanged += OnTabsChanged;
        cancellationToken.Register(Stop);

        await LoadAsync(cancellationToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        recent = devices.Selected is { } device
            ? [.. await query.GetRecentAsync(device, window, cancellationToken)]
            : [];

        ShowRecent();
        await chartCard.RefreshAsync(tabs.SelectedMetric, tabs.SelectedRange, cancellationToken);
    }

    private void Stop()
    {
        messenger.UnregisterAll(this);
        tabs.PropertyChanged -= OnTabsChanged;
        chartRefresh?.Cancel();
    }

    private void OnReading(ReadingReceived message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        dispatcher.Post(() =>
        {
            if (lifetime.IsCancellationRequested)
            {
                return;
            }

            var cutoff = message.Reading.Timestamp - window;
            recent = [.. recent.Where(reading => reading.Timestamp >= cutoff && reading.Timestamp < message.Reading.Timestamp), message.Reading];
            ShowRecent();
            _ = RefreshChartAsync();
        });
    }

    private void OnReload()
    {
        dispatcher.Post(() => _ = ReloadAsync());
    }

    private void OnSyncCompleted(SyncCompleted message)
    {
        if (message.ReadingsAdded > 0 && message.Device == devices.Selected)
        {
            OnReload();
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            await LoadAsync(lifetime);
        }
        catch (OperationCanceledException)
        {
            // Shutdown replaced this reload.
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Reloading the Live screen failed.");
            }
        }
    }

    private void OnTabsChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(MetricTabsViewModel.SelectedMetric) or nameof(MetricTabsViewModel.SelectedRange))
        {
            dispatcher.Post(() =>
            {
                ShowRecent();
                _ = RefreshChartAsync();
            });
        }
    }

    private void ShowRecent()
    {
        var now = timeProvider.GetUtcNow();
        Reading? latest = recent.Count > 0 ? recent[^1] : null;
        var lastHour = recent.Where(reading => reading.Timestamp >= now - miniCardWindow).ToList();

        hero.Update(latest, recent, now);
        gauge.Update(latest?.Co2Ppm);
        miniCards.Update(lastHour, tabs.SelectedMetric, now);
    }

    private async Task RefreshChartAsync()
    {
        chartRefresh?.Cancel();
        chartRefresh?.Dispose();
        chartRefresh = CancellationTokenSource.CreateLinkedTokenSource(lifetime);

        try
        {
            await chartCard.RefreshAsync(tabs.SelectedMetric, tabs.SelectedRange, chartRefresh.Token);
        }
        catch (OperationCanceledException)
        {
            // A newer refresh or shutdown replaced this one.
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Refreshing the Live chart failed.");
            }
        }
    }
}
