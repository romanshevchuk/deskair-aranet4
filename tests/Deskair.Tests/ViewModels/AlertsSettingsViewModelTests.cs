using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class AlertsSettingsViewModelTests
{
    private readonly FakeSettingsStore store = new();
    private readonly SettingsService settings;
    private readonly AlertsSettingsViewModel viewModel;

    public AlertsSettingsViewModelTests()
    {
        settings = new SettingsService(store, new StrongReferenceMessenger());
        viewModel = new AlertsSettingsViewModel(settings);
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
    public async Task Apply_saves_the_new_values()
    {
        viewModel.Load();
        viewModel.NotifyAtElevated = false;
        viewModel.QuietHoursEnabled = true;
        viewModel.QuietHoursStart = new TimeOnly(21, 0);

        await viewModel.ApplyAsync(TestContext.Current.CancellationToken);

        var saved = Assert.Single(store.Saved);
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
}
