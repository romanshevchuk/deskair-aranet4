using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Flyout;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Services.Alerts;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class FlyoutViewModelTests : IDisposable
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);

    private readonly FlyoutViewModel viewModel;
    private readonly FakeWindowController windows = new();
    private readonly FakeClock clock = new(now);
    private readonly FakeBluetoothRadio radio = new();
    private readonly AlertEngine engine;
    private readonly SnoozeService snooze;

    public FlyoutViewModelTests()
    {
        var localizer = new FakeLocalizer();
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        (engine, snooze) = TestSnooze.Create(clock, new StrongReferenceMessenger());
        viewModel = new FlyoutViewModel(localizer, TestFormatter.Create(localizer), calendar, windows, snooze, new FriendlyTime(localizer, calendar, clock), radio);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    private static List<Reading> History(double earlierPpm, double recentPpm)
    {
        var readings = new List<Reading>();

        for (var minute = 36; minute >= 30; minute -= 2)
        {
            readings.Add(Samples.Co2(now.AddMinutes(-minute), earlierPpm));
        }

        for (var minute = 6; minute >= 0; minute -= 2)
        {
            readings.Add(Samples.Co2(now.AddMinutes(-minute), recentPpm));
        }

        return readings;
    }

    [Fact]
    public void A_reading_gives_zone_value_room_and_advice()
    {
        var recent = History(1300, 1050);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.Equal(Zone.Elevated, viewModel.Zone);
        Assert.Equal(1050, viewModel.Co2Ppm);
        Assert.Equal("Office", viewModel.RoomName);
        Assert.Equal(recent[^1].Timestamp, viewModel.UpdatedAt);
        Assert.Equal(TrendDirection.Falling, viewModel.Trend);
        Assert.Equal("Advice_Elevated_Title", viewModel.AdviceTitle);
        Assert.Equal("Advice_Elevated_Text_Falling", viewModel.AdviceText);
        Assert.False(viewModel.ShowNotConnected);
    }

    [Fact]
    public void The_advice_is_the_same_as_the_hero_gives()
    {
        var recent = History(1050, 1900);
        var hero = TestHero.Create();

        viewModel.Update(recent[^1], recent, now, "Office");
        hero.Update(recent[^1], recent, now);

        Assert.Equal(hero.AdviceTitle, viewModel.AdviceTitle);
        Assert.Equal(hero.AdviceText, viewModel.AdviceText);
        Assert.Equal(hero.Zone, viewModel.Zone);
    }

    [Fact]
    public void Not_connected_offers_to_wake_the_sensor_while_bluetooth_is_on()
    {
        viewModel.Update(null, [], now, "Office");

        Assert.Equal("Offline_Text", viewModel.ReasonText);
        Assert.Equal("Offline_Step_Wake", viewModel.StepText);
    }

    [Fact]
    public void Not_connected_offers_to_turn_on_bluetooth_while_the_radio_is_off()
    {
        radio.IsOn = false;

        viewModel.Update(null, [], now, "Office");

        Assert.Equal("Offline_Text_BluetoothOff", viewModel.ReasonText);
        Assert.Equal("Offline_Bluetooth", viewModel.StepText);
    }

    [Fact]
    public void Connecting_says_so_and_offers_no_step()
    {
        viewModel.Update(null, [], now, "Office", connecting: true);

        Assert.True(viewModel.ShowNotConnected);
        Assert.Equal("Flyout_Connecting_Title", viewModel.NotConnectedTitle);
        Assert.Equal("Flyout_Connecting_Text", viewModel.ReasonText);
        Assert.False(viewModel.ShowStep);
    }

    [Fact]
    public void Not_connected_keeps_its_title_and_its_step()
    {
        viewModel.Update(null, [], now, "Office");

        Assert.Equal("Offline_Title", viewModel.NotConnectedTitle);
        Assert.True(viewModel.ShowStep);
    }

    [Fact]
    public void No_reading_shows_the_not_connected_state_but_keeps_the_room()
    {
        var recent = History(1300, 1050);
        viewModel.Update(recent[^1], recent, now, "Office");

        viewModel.Update(null, [], now, "Office");

        Assert.True(viewModel.ShowNotConnected);
        Assert.Equal("Office", viewModel.RoomName);
        Assert.Null(viewModel.Zone);
        Assert.Null(viewModel.Co2Ppm);
        Assert.Null(viewModel.UpdatedAt);
        Assert.Null(viewModel.AdviceTitle);
        Assert.Null(viewModel.AdviceText);
    }

    [Fact]
    public void A_new_reading_clears_the_not_connected_state()
    {
        viewModel.Update(null, [], now, "Office");
        var recent = History(600, 600);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.False(viewModel.ShowNotConnected);
    }

    [Fact]
    public void The_trend_and_the_zone_are_given_as_text()
    {
        var recent = History(1300, 1050);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.Equal("Trend_Falling", viewModel.TrendText);
        Assert.StartsWith("Trend_Detail(ppm=250", viewModel.TrendDetail);
        Assert.Equal("Zone_Elevated", viewModel.ZoneText);
        Assert.Equal(("1050", "ppm"), (viewModel.ValueText, viewModel.UnitText));
    }

    [Fact]
    public void The_updated_text_counts_seconds_and_then_minutes()
    {
        viewModel.Update(Samples.Co2(now.AddSeconds(-12), 700), [], now, "Office");
        Assert.Equal("Flyout_Updated(sec=12)", viewModel.UpdatedText);

        viewModel.Update(Samples.Co2(now.AddMinutes(-3), 700), [], now, "Office");
        Assert.Equal("Flyout_UpdatedMinutes(min=3)", viewModel.UpdatedText);
    }

    [Fact]
    public void The_other_readings_follow_the_unit_shown()
    {
        var reading = new Reading(now, 700, 22.3, 44, 975.3);

        viewModel.Update(reading, [reading], now, "Office");

        Assert.Equal(
            [("Metric_Temperature", "22.3", "°C"), ("Metric_Humidity", "44", "%"), ("Metric_Pressure", "975.3", "hPa")],
            viewModel.OtherReadings.Select(item => (item.Label, item.Value, item.Unit)));
    }

    [Fact]
    public void The_ribbon_covers_the_last_24_hours_with_labels_at_round_hours()
    {
        var recent = History(700, 700);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.Equal(now, viewModel.RibbonEnd);
        Assert.NotEmpty(viewModel.RibbonSegments);
        Assert.Equal(
            [("18:00", 0.25), ("00:00", 0.5), ("06:00", 0.75), ("Chart_Now", 1.0)],
            viewModel.RibbonLabels.Select(label => (label.Text, label.Position)));
    }

    [Fact]
    public void Not_connected_has_no_ribbon_or_other_readings()
    {
        var recent = History(1300, 1050);
        viewModel.Update(recent[^1], recent, now, "Office");

        viewModel.Update(null, [], now, "Office");

        Assert.Empty(viewModel.RibbonSegments);
        Assert.Empty(viewModel.OtherReadings);
        Assert.Null(viewModel.UpdatedText);
        Assert.Null(viewModel.ZoneText);
        Assert.Null(viewModel.TrendText);
    }

    [Fact]
    public void Not_connected_keeps_the_last_reading_of_the_day_and_when_it_was_seen()
    {
        var recent = History(1300, 1050);

        viewModel.Update(null, recent, now.AddMinutes(25), "Office");

        Assert.True(viewModel.ShowNotConnected);
        Assert.True(viewModel.ShowLastReading);
        Assert.Equal("1050", viewModel.LastValueText);
        Assert.Equal("Flyout_LastReadingAt(unit=ppm, time=12:00)", viewModel.LastDetailText);
        Assert.Equal("Flyout_LastSeen(ago=Duration_Minutes(min=25))", viewModel.UpdatedText);
        Assert.Empty(viewModel.OtherReadings);
        Assert.Empty(viewModel.RibbonLabels);
    }

    [Fact]
    public void Not_connected_without_any_reading_shows_no_last_reading()
    {
        viewModel.Update(null, [], now, "Office");

        Assert.False(viewModel.ShowLastReading);
        Assert.Null(viewModel.LastValueText);
        Assert.Null(viewModel.LastDetailText);
        Assert.Null(viewModel.UpdatedText);
    }

    [Fact]
    public void A_connected_flyout_has_no_last_reading_text()
    {
        viewModel.Update(null, History(1300, 1050), now, "Office");
        var recent = History(700, 700);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.False(viewModel.ShowLastReading);
        Assert.Null(viewModel.LastValueText);
    }

    [Fact]
    public void Open_and_Settings_ask_the_window_controller()
    {
        viewModel.OpenCommand.Execute(null);
        viewModel.OpenSettingsCommand.Execute(null);

        Assert.Equal([null, ShellSection.Settings], windows.Shown);
    }

    [Fact]
    public void Alerts_that_are_not_snoozed_show_a_plain_Snooze_button()
    {
        Assert.False(viewModel.IsSnoozed);
        Assert.Equal("Flyout_Snooze", viewModel.SnoozeButtonText);
        Assert.Equal(("Toast_Snooze", "Tray_SnoozeTomorrow", "Flyout_Resume"), (viewModel.SnoozeHourText, viewModel.SnoozeTomorrowText, viewModel.ResumeText));
    }

    [Fact]
    public void Snoozing_for_an_hour_shows_until_when()
    {
        viewModel.SnoozeHourCommand.Execute(null);

        Assert.Equal(now.AddHours(1), snooze.Until);
        Assert.True(viewModel.IsSnoozed);
        Assert.Equal("Flyout_SnoozedUntil(time=13:00)", viewModel.SnoozeButtonText);
    }

    [Fact]
    public void Snoozing_until_tomorrow_shows_the_morning()
    {
        viewModel.SnoozeTomorrowCommand.Execute(null);

        Assert.Equal(Samples.Utc(2026, 10, 13, 7), snooze.Until);
        Assert.Equal("Flyout_SnoozedUntil(time=13 Oct, 07:00)", viewModel.SnoozeButtonText);
    }

    [Fact]
    public void Resuming_goes_back_to_the_plain_button()
    {
        viewModel.SnoozeHourCommand.Execute(null);

        viewModel.ResumeCommand.Execute(null);

        Assert.False(viewModel.IsSnoozed);
        Assert.Equal("Flyout_Snooze", viewModel.SnoozeButtonText);
        Assert.Null(snooze.Until);
    }

    [Fact]
    public void Opening_the_flyout_looks_at_the_snooze_again()
    {
        snooze.ForOneHour();
        Assert.False(viewModel.IsSnoozed);

        viewModel.Update(null, [], now, "Office");

        Assert.True(viewModel.IsSnoozed);
    }

    [Fact]
    public void A_snooze_that_ended_is_gone_the_next_time_the_flyout_opens()
    {
        viewModel.SnoozeHourCommand.Execute(null);
        clock.Advance(TimeSpan.FromMinutes(61));

        viewModel.Update(null, [], now.AddMinutes(61), "Office");

        Assert.False(viewModel.IsSnoozed);
    }
}
