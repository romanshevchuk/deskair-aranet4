using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Settings;

namespace Deskair.Services.Alerts;

public sealed class AlertEngine : IDisposable
{
    private readonly INotifier notifier;
    private readonly IDoNotDisturb doNotDisturb;
    private readonly SettingsService settings;
    private readonly TimeProvider timeProvider;
    private readonly AlertPolicy policy;
    private readonly Dictionary<DeviceId, Episode> episodes = [];
    private readonly SemaphoreSlim gate = new(1, 1);

    public AlertEngine(
        INotifier notifier,
        IDoNotDisturb doNotDisturb,
        SettingsService settings,
        TimeProvider timeProvider)
    {
        this.notifier = notifier;
        this.doNotDisturb = doNotDisturb;
        this.settings = settings;
        this.timeProvider = timeProvider;
        policy = AlertPolicy.Default;
    }

    public void Dispose()
    {
        gate.Dispose();
    }

    public DateTimeOffset? SnoozedUntil { get; private set; }

    public void SnoozeUntil(DateTimeOffset until)
    {
        SnoozedUntil = until;
    }

    public static bool IsQuietHour(TimeOnly time, TimeOnly start, TimeOnly end)
    {
        if (start == end)
        {
            return false;
        }

        return start < end
            ? time >= start && time < end
            : time >= start || time < end;
    }

    public async Task OnReadingAsync(
        DeviceId device,
        string roomName,
        Reading reading,
        CancellationToken cancellationToken)
    {
        if (!double.IsFinite(reading.Co2Ppm))
        {
            return;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            var zone = ZoneClassifier.Classify(reading.Co2Ppm);
            var now = timeProvider.GetUtcNow();

            if (zone == Zone.Good)
            {
                episodes.Remove(device);
                return;
            }

            if (!episodes.TryGetValue(device, out var episode))
            {
                episode = new Episode(zone, now);
                episodes[device] = episode;
            }
            else if (episode.Zone != zone)
            {
                episode.Zone = zone;
                episode.ZoneSince = now;
            }

            if (!IsDue(episode, now) || !IsAllowed(zone, now))
            {
                return;
            }

            await notifier.NotifyAsync(new AlertNotification(device, roomName, zone, reading.Co2Ppm, now), cancellationToken);
            episode.Notified = zone;
            episode.LastNotified = now;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool IsDue(Episode episode, DateTimeOffset now) => episode.Notified switch
    {
        null => now - episode.StartedAt >= policy.Dwell,
        Zone.Elevated => episode.Zone == Zone.High && now - episode.ZoneSince >= policy.Dwell,
        _ => episode.Zone == Zone.High && now - episode.LastNotified >= policy.RepeatWhileHigh
    };

    private bool IsAllowed(Zone zone, DateTimeOffset now)
    {
        var current = settings.Current;

        if (!current.AlertsEnabled
            || (zone == Zone.Elevated && !current.NotifyAtElevated)
            || (zone == Zone.High && !current.NotifyAtHigh)
            || doNotDisturb.IsActive
            || now < SnoozedUntil)
        {
            return false;
        }

        if (!current.QuietHoursEnabled)
        {
            return true;
        }

        var localTime = TimeOnly.FromTimeSpan(timeProvider.GetLocalNow().TimeOfDay);

        return !IsQuietHour(localTime, current.QuietHoursStart, current.QuietHoursEnd);
    }

    private sealed class Episode
    {
        public Episode(Zone zone, DateTimeOffset now)
        {
            Zone = zone;
            StartedAt = now;
            ZoneSince = now;
        }

        public Zone Zone { get; set; }

        public DateTimeOffset StartedAt { get; }

        public DateTimeOffset ZoneSince { get; set; }

        public Zone? Notified { get; set; }

        public DateTimeOffset LastNotified { get; set; }
    }
}
