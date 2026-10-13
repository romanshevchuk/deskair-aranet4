using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Alerts;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public sealed class SnoozeServiceTests : IDisposable
{
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 13, 12);

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

    private readonly StrongReferenceMessenger messenger = new();
    private readonly List<AlertsSnoozeChanged> announced = [];
    private readonly FakeClock clock = new(noon);
    private readonly AlertEngine engine;
    private readonly SnoozeService snooze;

    public SnoozeServiceTests()
    {
        engine = new AlertEngine(new NoNotifier(), new NoDoNotDisturb(), new SettingsService(new DefaultSettingsStore(), messenger), clock);
        snooze = new SnoozeService(engine, clock, new LocalCalendar(TimeZoneInfo.Utc), messenger);
        messenger.Register<AlertsSnoozeChanged>(this, (_, message) => announced.Add(message));
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    [Fact]
    public void Nothing_is_snoozed_at_first()
    {
        Assert.Null(snooze.Until);
    }

    [Fact]
    public void Snoozing_for_an_hour_mutes_the_alerts_and_says_so()
    {
        snooze.ForOneHour();

        Assert.Equal(noon.AddHours(1), snooze.Until);
        Assert.Equal(noon.AddHours(1), engine.SnoozedUntil);
        Assert.Equal([new AlertsSnoozeChanged(noon.AddHours(1))], announced);
    }

    [Theory]
    [InlineData(12, 0, 14, 7)]
    [InlineData(3, 0, 13, 7)]
    [InlineData(6, 59, 13, 7)]
    [InlineData(7, 0, 14, 7)]
    [InlineData(23, 30, 14, 7)]
    public void Until_tomorrow_means_the_next_seven_in_the_morning(int hour, int minute, int expectedDay, int expectedHour)
    {
        clock.Advance(new DateTimeOffset(2026, 10, 13, hour, minute, 0, TimeSpan.Zero) - noon);

        snooze.UntilTomorrow();

        Assert.Equal(new DateTimeOffset(2026, 10, expectedDay, expectedHour, 0, 0, TimeSpan.Zero), snooze.Until);
    }

    [Fact]
    public void Until_tomorrow_follows_local_time_across_the_autumn_clock_change()
    {
        var kyiv = Samples.Kyiv;
        var local = new FakeClock(new DateTimeOffset(2026, 10, 24, 20, 0, 0, TimeSpan.FromHours(3)), kyiv);
        using var localEngine = new AlertEngine(new NoNotifier(), new NoDoNotDisturb(), new SettingsService(new DefaultSettingsStore(), messenger), local);
        var service = new SnoozeService(localEngine, local, new LocalCalendar(kyiv), messenger);

        service.UntilTomorrow();

        // The clocks go back on 25 October, so 07:00 that morning is at UTC+2.
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 7, 0, 0, TimeSpan.FromHours(2)), service.Until);
    }

    [Fact]
    public void A_snooze_that_has_ended_is_no_longer_reported()
    {
        snooze.ForOneHour();
        clock.Advance(TimeSpan.FromMinutes(61));

        Assert.Null(snooze.Until);
    }

    [Fact]
    public void Resuming_clears_the_snooze_and_says_so()
    {
        snooze.ForOneHour();

        snooze.Resume();

        Assert.Null(snooze.Until);
        Assert.Null(engine.SnoozedUntil);
        Assert.Equal(new AlertsSnoozeChanged(null), announced[^1]);
    }

    [Fact]
    public void A_new_snooze_replaces_the_old_one()
    {
        snooze.UntilTomorrow();

        snooze.ForOneHour();

        Assert.Equal(noon.AddHours(1), snooze.Until);
    }
}
