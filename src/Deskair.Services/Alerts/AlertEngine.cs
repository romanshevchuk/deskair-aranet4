using Deskair.Domain.Readings;
using Deskair.Services.Settings;

namespace Deskair.Services.Alerts;

public sealed class AlertEngine
{
    private readonly INotifier notifier;
    private readonly IDoNotDisturb doNotDisturb;
    private readonly SettingsService settings;
    private readonly TimeProvider timeProvider;
    private readonly AlertPolicy policy;

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

    // TODO(ALERT-01a) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Alerts" (quiet hours).
    // Input : a local time of day, quiet-hours start and end.
    // Output: true when the time is inside quiet hours.
    // Rules : start inclusive, end exclusive; the range may cross midnight (22:00 to 07:00).
    // Edge  : start equals end means no quiet hours (see questions.md Q5).
    // Done  : unskip tests Services/AlertEngineTests.QuietHours_* and make them pass.
    public static bool IsQuietHour(TimeOnly time, TimeOnly start, TimeOnly end)
        => throw new NotImplementedException("TODO(ALERT-01a)");

    // TODO(ALERT-01b) [L, sonnet]
    // Spec: docs/product/behaviour-rules.md "Alerts".
    // Input : the device, its room name, a new reading.
    // Output: calls INotifier at most once per episode.
    // Rules : notify after a 5 min dwell in Elevated or High; again only on escalation to High or after 60 min still High;
    //         respect quiet hours, snooze and IDoNotDisturb; never notify when alerts are off.
    // Edge  : a dip to Good ends the episode; the tray colour is not this class's job.
    // Done  : add Services/AlertEngineTests.Episode_* with fake notifier, clock and Do Not Disturb.
    public Task OnReadingAsync(
        DeviceId device,
        string roomName,
        Reading reading,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(ALERT-01b)");
}
