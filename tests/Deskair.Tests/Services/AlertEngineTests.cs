using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Alerts;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public sealed class AlertEngineTests : IDisposable
{
    private static readonly DeviceId office = new("aranet4", "AA:01");
    private static readonly DeviceId bedroom = new("aranet4", "AA:02");
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 9, 12);

    private readonly FakeNotifier notifier = new();
    private readonly FakeDoNotDisturb doNotDisturb = new();
    private readonly SettingsService settings = new(new FakeSettingsStore(), new StrongReferenceMessenger());
    private readonly FakeClock clock;
    private readonly AlertEngine engine;

    public AlertEngineTests()
    {
        clock = new FakeClock(start);
        engine = new AlertEngine(notifier, doNotDisturb, settings, clock);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    private Task Send(double ppm, DeviceId? device = null)
    {
        var reading = Samples.Co2(clock.GetUtcNow(), ppm);

        return engine.OnReadingAsync(device ?? office, "Office", reading, CancellationToken.None);
    }

    private async Task SendAfter(double ppm, params int[] minutesFromNow)
    {
        foreach (var minute in minutesFromNow)
        {
            clock.Advance(TimeSpan.FromMinutes(minute));
            await Send(ppm);
        }
    }

    [Fact]
    public async Task Episode_notifies_only_after_the_five_minute_dwell()
    {
        await Send(1100);
        await SendAfter(1100, 4);

        Assert.Empty(notifier.Sent);

        await SendAfter(1100, 1);

        var sent = Assert.Single(notifier.Sent);
        Assert.Equal(Zone.Elevated, sent.Zone);
        Assert.Equal(office, sent.Device);
        Assert.Equal("Office", sent.RoomName);
        Assert.Equal(1100, sent.Co2Ppm);
    }

    [Fact]
    public async Task Episode_sends_one_toast_while_the_zone_stays_the_same()
    {
        await Send(1100);
        await SendAfter(1100, 5, 5, 10, 30, 30);

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task Episode_notifies_again_on_escalation_to_High_after_the_dwell()
    {
        await Send(1100);
        await SendAfter(1100, 5);
        await SendAfter(1500, 5);

        Assert.Single(notifier.Sent);

        await SendAfter(1500, 4);
        Assert.Single(notifier.Sent);

        await SendAfter(1500, 1);

        Assert.Equal(new[] { Zone.Elevated, Zone.High }, notifier.Sent.Select(sent => sent.Zone));
    }

    [Fact]
    public async Task Episode_repeats_only_after_60_minutes_while_still_High()
    {
        await Send(1600);
        await SendAfter(1600, 5);

        Assert.Single(notifier.Sent);

        await SendAfter(1600, 30, 29);
        Assert.Single(notifier.Sent);

        await SendAfter(1600, 1);
        Assert.Equal(2, notifier.Sent.Count);
    }

    [Fact]
    public async Task Episode_ends_after_ten_minutes_of_Good_and_a_new_episode_notifies_again()
    {
        await Send(1100);
        await SendAfter(1100, 5);
        await SendAfter(800, 2, 2, 2, 2, 2, 2);
        await SendAfter(1100, 2);
        await SendAfter(1100, 4);
        Assert.Single(notifier.Sent);

        await SendAfter(1100, 1);

        Assert.Equal(2, notifier.Sent.Count);
    }

    [Fact]
    public async Task A_value_hovering_at_the_boundary_stays_one_episode_with_one_toast()
    {
        await Send(1003);
        await SendAfter(1003, 5);
        Assert.Single(notifier.Sent);

        // 995 and 1005 alternate every two minutes for half an hour: one dip is not a recovery.
        for (var step = 0; step < 15; step++)
        {
            await SendAfter(step % 2 == 0 ? 995 : 1005, 2);
        }

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task A_pressed_toast_is_not_repeated_in_the_same_episode()
    {
        await Send(1100);
        await SendAfter(1100, 5);
        engine.AcknowledgeAll();

        await SendAfter(800, 2);
        await SendAfter(1100, 2, 6, 30);

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task A_pressed_toast_still_allows_the_escalation_to_High()
    {
        await Send(1100);
        await SendAfter(1100, 5);
        engine.AcknowledgeAll();

        await SendAfter(1500, 1);
        await SendAfter(1500, 5);

        Assert.Equal([Zone.Elevated, Zone.High], notifier.Sent.Select(sent => sent.Zone));
    }

    [Fact]
    public async Task A_pressed_High_toast_is_not_repeated_after_60_minutes()
    {
        await Send(1500);
        await SendAfter(1500, 5);
        engine.AcknowledgeAll();

        await SendAfter(1500, 30, 31);

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task A_new_toast_after_a_press_can_be_pressed_again()
    {
        await Send(1100);
        await SendAfter(1100, 5);
        engine.AcknowledgeAll();
        await SendAfter(1500, 1);
        await SendAfter(1500, 5);
        engine.AcknowledgeAll();

        await SendAfter(1500, 61);

        Assert.Equal(2, notifier.Sent.Count);
    }

    [Fact]
    public async Task Pressing_a_toast_does_not_affect_an_episode_that_has_not_sent_one()
    {
        await Send(1100);
        engine.AcknowledgeAll();

        await SendAfter(1100, 5);

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task Episode_never_notifies_when_alerts_are_off()
    {
        await settings.UpdateAsync(current => current with { AlertsEnabled = false }, CancellationToken.None);

        await Send(1600);
        await SendAfter(1600, 10, 70);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Episode_skips_Elevated_when_that_toast_is_off_but_still_warns_at_High()
    {
        await settings.UpdateAsync(current => current with { NotifyAtElevated = false }, CancellationToken.None);

        await Send(1100);
        await SendAfter(1100, 10);
        Assert.Empty(notifier.Sent);

        await SendAfter(1500, 1);

        Assert.Equal(Zone.High, Assert.Single(notifier.Sent).Zone);
    }

    [Fact]
    public async Task Episode_skips_High_when_that_toast_is_off()
    {
        await settings.UpdateAsync(current => current with { NotifyAtHigh = false }, CancellationToken.None);

        await Send(1600);
        await SendAfter(1600, 10);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Episode_is_held_back_by_Do_Not_Disturb_and_sent_when_it_ends_if_the_air_is_still_bad()
    {
        doNotDisturb.IsActive = true;
        await Send(1100);
        await SendAfter(1100, 6);
        Assert.Empty(notifier.Sent);

        doNotDisturb.IsActive = false;
        await SendAfter(1100, 2);

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task Episode_is_dropped_when_the_air_gets_Good_during_Do_Not_Disturb()
    {
        doNotDisturb.IsActive = true;
        await Send(1100);
        await SendAfter(1100, 6);
        await SendAfter(700, 2);

        doNotDisturb.IsActive = false;
        await SendAfter(700, 2);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Episode_is_muted_in_quiet_hours_by_local_time()
    {
        // 20:30 UTC in January is 22:30 in Kyiv.
        var evening = new FakeClock(Samples.Utc(2026, 1, 15, 20, 30), Samples.Kyiv);
        using var quietEngine = new AlertEngine(notifier, doNotDisturb, settings, evening);
        await settings.UpdateAsync(current => current with { QuietHoursEnabled = true }, CancellationToken.None);

        await quietEngine.OnReadingAsync(office, "Office", Samples.Co2(evening.GetUtcNow(), 1100), CancellationToken.None);
        evening.Advance(TimeSpan.FromMinutes(10));
        await quietEngine.OnReadingAsync(office, "Office", Samples.Co2(evening.GetUtcNow(), 1100), CancellationToken.None);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Episode_is_not_muted_outside_quiet_hours_or_when_they_are_off()
    {
        await settings.UpdateAsync(current => current with { QuietHoursEnabled = true }, CancellationToken.None);

        await Send(1100);
        await SendAfter(1100, 5);

        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task Episode_is_held_back_by_snooze_until_it_ends()
    {
        await Send(1100);
        engine.SnoozeUntil(clock.GetUtcNow().AddHours(1));
        await SendAfter(1100, 10);
        Assert.Empty(notifier.Sent);

        await SendAfter(1100, 50);
        Assert.Single(notifier.Sent);
    }

    [Fact]
    public async Task Episode_is_tracked_per_device()
    {
        await Send(1100, office);
        clock.Advance(TimeSpan.FromMinutes(3));
        await Send(1100, bedroom);
        clock.Advance(TimeSpan.FromMinutes(3));
        await Send(1100, office);
        await Send(1100, bedroom);

        var sent = Assert.Single(notifier.Sent);
        Assert.Equal(office, sent.Device);
    }

    [Fact]
    public async Task Episode_ignores_a_reading_that_is_not_a_number()
    {
        await Send(1100);
        await SendAfter(1100, 3);
        await Send(double.NaN);
        await SendAfter(1100, 2);

        Assert.Single(notifier.Sent);
    }

    [Theory]
    [InlineData(22, 0, true)]
    [InlineData(23, 30, true)]
    [InlineData(3, 0, true)]
    [InlineData(6, 59, true)]
    [InlineData(7, 0, false)]
    [InlineData(12, 0, false)]
    [InlineData(21, 59, false)]
    public void QuietHours_cross_midnight_and_the_end_is_exclusive(int hour, int minute, bool expected)
    {
        var time = new TimeOnly(hour, minute);

        Assert.Equal(expected, AlertEngine.IsQuietHour(time, new TimeOnly(22, 0), new TimeOnly(7, 0)));
    }

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(12, 0, true)]
    [InlineData(17, 0, false)]
    [InlineData(8, 59, false)]
    public void QuietHours_within_one_day(int hour, int minute, bool expected)
    {
        var time = new TimeOnly(hour, minute);

        Assert.Equal(expected, AlertEngine.IsQuietHour(time, new TimeOnly(9, 0), new TimeOnly(17, 0)));
    }

    [Fact]
    public void QuietHours_with_equal_start_and_end_are_off()
    {
        Assert.False(AlertEngine.IsQuietHour(new TimeOnly(12, 0), new TimeOnly(22, 0), new TimeOnly(22, 0)));
    }
}
