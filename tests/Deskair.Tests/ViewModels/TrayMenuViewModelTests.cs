using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.Shell;
using Deskair.App.ViewModels.Tray;
using Deskair.Domain.Time;
using Deskair.Services.Alerts;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public sealed class TrayMenuViewModelTests : IDisposable
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 13, 12);



    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeClock clock = new(now);
    private readonly FakeWindowController windows = new();
    private readonly FakeSettingsStore settingsStore = new();
    private readonly ListLogger<TrayMenuViewModel> logger = new();
    private readonly SettingsService settings;
    private readonly AlertEngine engine;
    private readonly SnoozeService snooze;

    public TrayMenuViewModelTests()
    {
        settings = new SettingsService(settingsStore, messenger);
        (engine, snooze) = TestSnooze.Create(clock, messenger, settings);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    private TrayMenuViewModel Create(ILocalizer? localizer = null)
    {
        localizer ??= new FakeLocalizer();

        return new TrayMenuViewModel(
            localizer,
            windows,
            snooze,
            settings,
            new FriendlyTime(localizer, new LocalCalendar(TimeZoneInfo.Utc), clock),
            messenger,
            new FakeUiDispatcher(),
            logger);
    }

    [Fact]
    public void The_menu_texts_come_from_the_localizer()
    {
        var menu = Create();

        Assert.Equal("Action_OpenApp", menu.OpenText);
        Assert.Equal("Nav_Settings", menu.SettingsText);
        Assert.Equal("Tray_Quit", menu.QuitText);
        Assert.Equal("Toast_Snooze", menu.SnoozeHourText);
        Assert.Equal("Tray_SnoozeTomorrow", menu.SnoozeTomorrowText);
        Assert.Equal("Settings_TrayValue", menu.ShowValueText);
    }

    [Fact]
    public void A_new_language_rebuilds_every_text()
    {
        var localizer = new SwitchableLocalizer();
        var menu = Create(localizer);
        var changed = new List<string?>();
        menu.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        localizer.Prefix = "uk:";

        messenger.Send(new LanguageChanged("uk"));

        Assert.All(
            new[] { nameof(TrayMenuViewModel.OpenText), nameof(TrayMenuViewModel.SettingsText), nameof(TrayMenuViewModel.QuitText), nameof(TrayMenuViewModel.SnoozeHourText), nameof(TrayMenuViewModel.SnoozeTomorrowText), nameof(TrayMenuViewModel.ShowValueText) },
            name => Assert.Contains(name, changed));
    }

    [Fact]
    public void Open_Settings_and_Quit_ask_the_window_controller()
    {
        var menu = Create();

        menu.OpenCommand.Execute(null);
        menu.OpenSettingsCommand.Execute(null);
        menu.QuitCommand.Execute(null);

        Assert.Equal([null, ShellSection.Settings], windows.Shown);
        Assert.Equal(1, windows.QuitRequests);
    }

    [Fact]
    public void Snoozing_for_an_hour_from_the_menu_mutes_alerts_and_offers_to_resume()
    {
        var menu = Create();
        Assert.False(menu.IsSnoozed);

        menu.SnoozeHourCommand.Execute(null);

        Assert.Equal(now.AddHours(1), snooze.Until);
        Assert.True(menu.IsSnoozed);
        Assert.Equal("Tray_ResumeUntil(time=13:00)", menu.ResumeText);
    }

    [Fact]
    public void Snoozing_until_tomorrow_from_the_menu_mutes_alerts_until_the_morning()
    {
        var menu = Create();

        menu.SnoozeTomorrowCommand.Execute(null);

        Assert.Equal(Samples.Utc(2026, 10, 14, 7), snooze.Until);
        Assert.True(menu.IsSnoozed);
    }

    [Fact]
    public void Resuming_ends_the_snooze()
    {
        var menu = Create();
        menu.SnoozeHourCommand.Execute(null);

        menu.ResumeCommand.Execute(null);

        Assert.Null(snooze.Until);
        Assert.False(menu.IsSnoozed);
    }

    [Fact]
    public void A_snooze_made_elsewhere_such_as_a_toast_button_shows_in_the_menu()
    {
        var menu = Create();

        snooze.ForOneHour();

        Assert.True(menu.IsSnoozed);
    }

    [Fact]
    public void Opening_the_menu_after_the_snooze_ended_no_longer_offers_to_resume()
    {
        var menu = Create();
        menu.SnoozeHourCommand.Execute(null);
        clock.Advance(TimeSpan.FromMinutes(61));

        menu.Refresh();

        Assert.False(menu.IsSnoozed);
    }

    [Fact]
    public async Task The_value_item_shows_the_setting_and_a_change_elsewhere_updates_it()
    {
        var menu = Create();
        Assert.False(menu.ShowValue);

        await settings.UpdateAsync(current => current with { ShowValueOnTrayIcon = true }, TestContext.Current.CancellationToken);

        Assert.True(menu.ShowValue);
    }

    [Fact]
    public async Task Choosing_the_value_item_saves_the_opposite_of_what_it_shows()
    {
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        var menu = Create();

        await menu.ToggleShowValueCommand.ExecuteAsync(null);
        Assert.True(settingsStore.Stored.ShowValueOnTrayIcon);

        await menu.ToggleShowValueCommand.ExecuteAsync(null);
        Assert.False(settingsStore.Stored.ShowValueOnTrayIcon);
    }

    [Fact]
    public async Task A_setting_that_cannot_be_saved_is_logged_not_thrown()
    {
        settingsStore.FailSaves = new IOException("disk full");
        var menu = Create();

        await menu.ToggleShowValueCommand.ExecuteAsync(null);

        Assert.Contains(logger.Entries, entry => entry.Exception is IOException);
    }
}
