using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.ViewModels.Settings;
using Deskair.Services.Messages;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.ViewModels;

public class SleepSettingsViewModelTests
{
    private readonly FakeSettingsStore store = new();
    private readonly StrongReferenceMessenger messenger = new();
    private readonly SettingsService settings;
    private readonly SleepSettingsViewModel viewModel;

    public SleepSettingsViewModelTests()
    {
        settings = new SettingsService(store, messenger);
        viewModel = new SleepSettingsViewModel(settings, NullLogger<SleepSettingsViewModel>.Instance);
    }

    [Fact]
    public void Load_shows_the_default_night()
    {
        viewModel.Load();

        Assert.Equal("22:00", viewModel.NightStart.Text);
        Assert.Equal("07:00", viewModel.NightEnd.Text);
        Assert.False(viewModel.IsEmptyWindow);
    }

    [Fact]
    public async Task Load_shows_the_night_the_user_chose()
    {
        store.Stored = AppSettings.Default with { NightStart = new TimeOnly(23, 30), NightEnd = new TimeOnly(8, 0) };
        await settings.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Load();

        Assert.Equal("23:30", viewModel.NightStart.Text);
        Assert.Equal("08:00", viewModel.NightEnd.Text);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void A_start_time_typed_is_saved_at_once_and_announced()
    {
        viewModel.Load();
        var changed = 0;
        messenger.Register<SettingsChanged>(this, (_, _) => changed++);

        viewModel.NightStart.Text = "23:00";

        Assert.Equal(new TimeOnly(23, 0), store.Stored.NightStart);
        Assert.Equal(new TimeOnly(7, 0), store.Stored.NightEnd);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void An_end_time_typed_is_saved_at_once()
    {
        viewModel.Load();

        viewModel.NightEnd.Text = "9:15";

        Assert.Equal(new TimeOnly(9, 15), store.Stored.NightEnd);
    }

    [Fact]
    public void A_time_that_is_not_a_time_is_flagged_and_nothing_is_saved()
    {
        viewModel.Load();

        viewModel.NightEnd.Text = "noon";

        Assert.True(viewModel.NightEnd.IsInvalid);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void A_night_that_starts_when_it_ends_is_flagged_and_not_saved()
    {
        viewModel.Load();

        viewModel.NightEnd.Text = "22:00";

        Assert.True(viewModel.IsEmptyWindow);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void Choosing_different_times_again_clears_the_flag_and_saves()
    {
        viewModel.Load();
        viewModel.NightEnd.Text = "22:00";

        viewModel.NightEnd.Text = "06:00";

        Assert.False(viewModel.IsEmptyWindow);
        Assert.Equal(new TimeOnly(6, 0), store.Stored.NightEnd);
    }

    [Fact]
    public void A_night_inside_one_day_is_fine()
    {
        viewModel.Load();
        viewModel.NightStart.Text = "02:00";

        viewModel.NightEnd.Text = "10:00";

        Assert.False(viewModel.IsEmptyWindow);
        Assert.Equal(new TimeOnly(2, 0), store.Stored.NightStart);
        Assert.Equal(new TimeOnly(10, 0), store.Stored.NightEnd);
    }

    [Fact]
    public async Task Apply_keeps_everything_else()
    {
        store.Stored = AppSettings.Default with { Theme = ThemeChoice.Dark };
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.Load();

        viewModel.NightStart.Text = "21:00";

        Assert.Equal(ThemeChoice.Dark, Assert.Single(store.Saved).Theme);
    }

    [Fact]
    public void A_save_that_fails_is_logged_not_thrown()
    {
        viewModel.Load();
        store.FailSaves = new IOException("disk full");

        var thrown = Record.Exception(() => viewModel.NightStart.Text = "21:00");

        Assert.Null(thrown);
    }
}
