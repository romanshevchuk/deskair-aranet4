using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Platform.Notifications;
using Deskair.App.ViewModels.Shell;
using Deskair.Services.Alerts;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Platform;

public sealed class ToastActionHandlerTests : IDisposable
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 13, 12);

    private sealed class NoNotifier : INotifier
    {
        public Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoDoNotDisturb : IDoNotDisturb
    {
        public bool IsActive => false;
    }

    private sealed class DefaultSettingsStore : ISettingsStore
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(AppSettings.Default);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private readonly FakeClock clock = new(now);
    private readonly FakeWindowController windows = new();
    private readonly FakeUiDispatcher dispatcher = new();
    private readonly AlertEngine engine;
    private readonly ToastActionHandler handler;

    public ToastActionHandlerTests()
    {
        engine = new AlertEngine(new NoNotifier(), new NoDoNotDisturb(), new SettingsService(new DefaultSettingsStore(), new StrongReferenceMessenger()), clock);
        handler = new ToastActionHandler(engine, windows, clock, dispatcher, NullLogger<ToastActionHandler>.Instance);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    [Fact]
    public void Snooze_mutes_alerts_for_an_hour()
    {
        handler.Handle(ToastArguments.Snooze);

        Assert.Equal(now.AddHours(1), engine.SnoozedUntil);
        Assert.Empty(windows.Shown);
    }

    [Fact]
    public void Open_shows_the_main_window_on_the_ui_thread()
    {
        handler.Handle(ToastArguments.Open);

        Assert.Equal([(ShellSection?)null], windows.Shown);
        Assert.Equal(1, dispatcher.Posted);
        Assert.Null(engine.SnoozedUntil);
    }

    [Fact]
    public void A_second_snooze_counts_from_now()
    {
        handler.Handle(ToastArguments.Snooze);
        clock.Advance(TimeSpan.FromMinutes(20));

        handler.Handle(ToastArguments.Snooze);

        Assert.Equal(now.AddMinutes(80), engine.SnoozedUntil);
    }

    [Theory]
    [InlineData("")]
    [InlineData("dismiss")]
    [InlineData("action=whatever")]
    public void Arguments_that_are_not_ours_do_nothing(string arguments)
    {
        handler.Handle(arguments);

        Assert.Null(engine.SnoozedUntil);
        Assert.Empty(windows.Shown);
    }
}
