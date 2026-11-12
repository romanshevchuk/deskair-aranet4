using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Platform.Tray;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Deskair.Tests.Ui;

namespace Deskair.Tests.Platform;

public class MainWindowPlacesTests
{
    private static readonly WindowPlace stored = new(150, 120, 640, 480, false);

    private static T Run<T>(Func<T> work) => UiHost.Run(ThemeChoice.Light, work);

    private static Window NewWindow() => new() { ShowActivated = false, Width = 100, Height = 100 };

    [Fact]
    public void A_restored_window_opens_in_the_stored_place_and_size()
    {
        var shown = Run(() =>
        {
            var window = NewWindow();
            MainWindowPlaces.Restore(window, stored);

            window.Show();
            var place = MainWindowPlaces.Read(window);
            window.Close();

            return place;
        });

        Assert.Equal(stored, shown);
    }

    [Fact]
    public void A_window_stored_as_maximized_opens_maximized_and_keeps_its_normal_place()
    {
        var result = Run(() =>
        {
            // WPF refuses to show a maximized window without activating it.
            var window = new Window { Width = 100, Height = 100 };
            MainWindowPlaces.Restore(window, stored with { Maximized = true });

            window.Show();
            var state = window.WindowState;
            var place = MainWindowPlaces.Read(window);
            window.Close();

            return (state, place);
        });

        Assert.Equal(WindowState.Maximized, result.state);
        Assert.Equal(stored with { Maximized = true }, result.place);
    }

    [Fact]
    public void With_nothing_stored_the_window_opens_in_the_centre_of_the_screen()
    {
        var location = Run(() =>
        {
            var window = NewWindow();
            MainWindowPlaces.Restore(window, null);
            var startup = window.WindowStartupLocation;
            window.Close();

            return startup;
        });

        Assert.Equal(WindowStartupLocation.CenterScreen, location);
    }

    [Fact]
    public void A_place_on_a_monitor_that_is_gone_is_moved_back_onto_a_screen()
    {
        var shown = Run(() =>
        {
            var window = NewWindow();
            MainWindowPlaces.Restore(window, stored with { Left = -30000, Top = -30000 });

            window.Show();
            var place = MainWindowPlaces.Read(window);
            window.Close();

            return place;
        });

        Assert.NotNull(shown);
        Assert.True(shown.Left > -30000 && shown.Top > -30000);
    }

    [Fact]
    public void Closing_a_shown_window_stores_where_it_was()
    {
        var store = new FakeSettingsStore();
        var settings = new SettingsService(store, new StrongReferenceMessenger());
        var expected = Run(() =>
        {
            var window = NewWindow();
            new MainWindowPlaces(settings, new ListLogger<MainWindowPlaces>()).Attach(window);
            window.Show();
            window.Left = 210;
            window.Top = 160;
            var place = MainWindowPlaces.Read(window);

            window.Close();

            return place;
        });

        Assert.NotNull(expected);
        Assert.Equal(expected, store.Stored.MainWindowPlace);
    }

    [Fact]
    public void Closing_a_window_that_was_never_shown_stores_nothing()
    {
        var store = new FakeSettingsStore();
        var settings = new SettingsService(store, new StrongReferenceMessenger());
        Run(() =>
        {
            var window = NewWindow();
            new MainWindowPlaces(settings, new ListLogger<MainWindowPlaces>()).Attach(window);

            window.Close();

            return 0;
        });

        Assert.Empty(store.Saved);
    }
}
