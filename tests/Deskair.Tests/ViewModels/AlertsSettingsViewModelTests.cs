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
        Assert.Equal(new TimeOnly(23, 30), viewModel.QuietHoursStart);
        Assert.Equal(new TimeOnly(6, 15), viewModel.QuietHoursEnd);
    }

    [Fact]
    public void A_change_on_the_screen_is_saved_at_once()
    {
        viewModel.Load();
        viewModel.NotifyAtElevated = false;
        viewModel.QuietHoursEnabled = true;
        viewModel.QuietHoursStart = new TimeOnly(21, 0);

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
    public async Task Load_shows_the_times_as_hours_and_minutes()
    {
        store.Stored = AppSettings.Default with { QuietHoursStart = new TimeOnly(23, 30), QuietHoursEnd = new TimeOnly(6, 5) };
        await settings.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Load();

        Assert.Equal("23:30", viewModel.QuietHoursStartText);
        Assert.Equal("06:05", viewModel.QuietHoursEndText);
    }

    [Theory]
    [InlineData("21:15", 21, 15)]
    [InlineData("9:05", 9, 5)]
    [InlineData(" 22:00 ", 22, 0)]
    public void A_valid_time_typed_is_saved_at_once(string text, int hour, int minute)
    {
        viewModel.Load();

        viewModel.QuietHoursStartText = text;

        Assert.Equal(new TimeOnly(hour, minute), viewModel.QuietHoursStart);
        Assert.Equal(new TimeOnly(hour, minute), store.Stored.QuietHoursStart);
        Assert.False(viewModel.QuietHoursStartInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("noon")]
    public void A_time_that_is_not_a_time_is_flagged_and_not_saved(string text)
    {
        viewModel.Load();

        viewModel.QuietHoursEndText = text;

        Assert.True(viewModel.QuietHoursEndInvalid);
        Assert.Equal(new TimeOnly(7, 0), viewModel.QuietHoursEnd);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void Fixing_a_flagged_time_clears_the_flag()
    {
        viewModel.Load();
        viewModel.QuietHoursEndText = "noon";

        viewModel.QuietHoursEndText = "08:00";

        Assert.False(viewModel.QuietHoursEndInvalid);
        Assert.Equal(new TimeOnly(8, 0), store.Stored.QuietHoursEnd);
    }

    [Fact]
    public void Load_clears_a_flag_left_by_a_typo()
    {
        viewModel.Load();
        viewModel.QuietHoursStartText = "noon";

        viewModel.Load();

        Assert.False(viewModel.QuietHoursStartInvalid);
        Assert.Equal("22:00", viewModel.QuietHoursStartText);
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
