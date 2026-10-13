using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Time;
using Deskair.Services.Alerts;
using Deskair.Services.Settings;

namespace Deskair.Tests.Fakes;

/// <summary>Builds a real <see cref="SnoozeService"/> on a real <see cref="AlertEngine"/> whose toast goes nowhere, for tests of screens that offer Snooze. Dispose the engine with the test.</summary>
public static class TestSnooze
{
    public static (AlertEngine Engine, SnoozeService Snooze) Create(FakeClock clock, IMessenger messenger, SettingsService? settings = null, TimeZoneInfo? zone = null)
    {
        var engine = new AlertEngine(
            new NoNotifier(),
            new NoDoNotDisturb(),
            settings ?? new SettingsService(new FakeSettingsStore(), messenger),
            clock);

        return (engine, new SnoozeService(engine, clock, new LocalCalendar(zone ?? TimeZoneInfo.Utc), messenger));
    }

    private sealed class NoNotifier : INotifier
    {
        public Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoDoNotDisturb : IDoNotDisturb
    {
        public bool IsActive => false;
    }
}
