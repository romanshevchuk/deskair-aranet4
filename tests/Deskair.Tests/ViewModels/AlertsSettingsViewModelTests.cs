using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public class AlertsSettingsViewModelTests
{
    private readonly FakeSettingsStore store = new();
    private readonly SettingsService settings;
    private readonly StrongReferenceMessenger messenger = new();
    private readonly FakeDoNotDisturb doNotDisturb = new();
    private readonly AlertsSettingsViewModel viewModel;

    public AlertsSettingsViewModelTests()
    {
        settings = new SettingsService(store, messenger);
        viewModel = new AlertsSettingsViewModel(settings, doNotDisturb, messenger, new FakeUiDispatcher(), NullLogger<AlertsSettingsViewModel>.Instance);
    }

    [Fact]
    public async Task Load_shows_every_alert_setting()
    {
        store.Stored = AppSettings.Default with
        {
            AlertsEnabled = false,
            NotifyAtElevated = false,
            NotifyAtHigh = true,
            QuietHoursEnabled = true,
            QuietHoursStart = new TimeOnly(23, 30),
            QuietHoursEnd = new TimeOnly(6, 15)
        };
        await settings.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Load();

        Assert.False(viewModel.AlertsEnabled);
        Assert.False(viewModel.NotifyAtElevated);
        Assert.True(viewModel.NotifyAtHigh);
        Assert.True(viewModel.QuietHoursEnabled);
        Assert.Equal(new TimeOnly(23, 30), viewModel.QuietHoursStart.Value);
        Assert.Equal(new TimeOnly(6, 15), viewModel.QuietHoursEnd.Value);
    }

    [Fact]
    public void A_change_on_the_screen_is_saved_at_once()
    {
        viewModel.Load();
        viewModel.NotifyAtElevated = false;
        viewModel.QuietHoursEnabled = true;
        viewModel.QuietHoursStart.Text = "21:00";

        var saved = store.Stored;
        Assert.False(saved.NotifyAtElevated);
        Assert.True(saved.QuietHoursEnabled);
        Assert.Equal(new TimeOnly(21, 0), saved.QuietHoursStart);
        Assert.Equal(new TimeOnly(7, 0), saved.QuietHoursEnd);
    }

    [Fact]
    public async Task Apply_keeps_language_and_theme()
    {
        store.Stored = AppSettings.Default with { Theme = ThemeChoice.Dark };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();
        viewModel.AlertsEnabled = false;

        await viewModel.ApplyAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ThemeChoice.Dark, Assert.Single(store.Saved).Theme);
    }

    [Fact]
    public async Task Unchanged_values_do_nothing()
    {
        viewModel.Load();

        await viewModel.ApplyAsync(TestContext.Current.CancellationToken);

        Assert.Empty(store.Saved);
    }

    [Fact]
    public void A_time_typed_in_quiet_hours_is_saved_at_once()
    {
        viewModel.Load();

        viewModel.QuietHoursEnd.Text = "06:30";

        Assert.Equal(new TimeOnly(6, 30), store.Stored.QuietHoursEnd);
    }

    [Fact]
    public void A_time_that_is_not_a_time_is_flagged_and_nothing_is_saved()
    {
        viewModel.Load();

        viewModel.QuietHoursEnd.Text = "noon";

        Assert.True(viewModel.QuietHoursEnd.IsInvalid);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void Windows_holding_alerts_is_shown_while_alerts_are_on()
    {
        doNotDisturb.IsActive = true;
        viewModel.Load();

        Assert.True(viewModel.IsPausedByWindows);

        messenger.Send(new DoNotDisturbChanged(false));

        Assert.False(viewModel.IsPausedByWindows);
    }

    [Fact]
    public void With_alerts_off_nothing_is_paused_by_windows()
    {
        doNotDisturb.IsActive = true;
        viewModel.Load();

        viewModel.AlertsEnabled = false;

        Assert.False(viewModel.IsPausedByWindows);
    }
}
