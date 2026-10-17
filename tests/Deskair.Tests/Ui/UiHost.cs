using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Deskair.App.Localization;
using Deskair.App.Ui.Markup;
using Deskair.App.Ui.Motion;
using Deskair.App.Ui.Theme;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Ui;

/// <summary>
/// WPF allows one Application per process and ties it to one thread, so every UI test runs on this
/// single STA thread. The application resources are set up the same way App.xaml and the startup do.
/// </summary>
public static class UiHost
{
    private static readonly FakeSystemTheme systemTheme = new();
    private static readonly Dispatcher dispatcher;

    static UiHost()
    {
        Dispatcher? uiDispatcher = null;
        ThemeService? themeService = null;
        Localizer? localizerService = null;
        using var ready = new ManualResetEventSlim();
        var fakeTheme = systemTheme;
        var dictionaries = new[]
        {
            "Ui/Theme/Tokens/Typography.xaml",
            "Ui/Theme/Tokens/Spacing.xaml",
            "Ui/Theme/Tokens/Radii.xaml",
            "Ui/Controls/Focus/FocusVisuals.xaml",
            "Ui/Theme/Styles/BaseStyles.xaml"
        };

        // Renders and layout tests see every control in its final state; the motion tests switch animations on for themselves.
        MotionPolicy.Override = false;

        var thread = new Thread(() =>
        {
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            foreach (var name in dictionaries)
            {
                var source = new Uri($"pack://application:,,,/Deskair.App;component/{name}", UriKind.Absolute);
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = source });
            }

            themeService = new ThemeService(fakeTheme);
            localizerService = new Localizer();
            application.Resources[TExtension.LocalizerResourceKey] = localizerService;

            uiDispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        ready.Wait();

        dispatcher = uiDispatcher!;
        Theme = themeService!;
        Localizer = localizerService!;
    }

    public static ThemeService Theme { get; }

    public static Localizer Localizer { get; }


    public static void Run(Action action)
    {
        Run(ThemeChoice.Light, action);
    }

    public static void Run(ThemeChoice choice, Action action)
    {
        Exception? error = null;

        dispatcher.Invoke(() =>
        {
            try
            {
                Theme.Apply(choice);
                Localizer.SetCulture(System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                action();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        });

        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    public static T Run<T>(ThemeChoice choice, Func<T> function)
    {
        T result = default!;

        Run(choice, (Action)(() => result = function()));

        return result;
    }

    /// <summary>Code-created elements get their style and template only after initialization.</summary>
    public static T Prepare<T>(T element)
        where T : FrameworkElement
    {
        element.BeginInit();
        element.EndInit();
        element.ApplyTemplate();

        return element;
    }

    public static T Run<T>(Func<T> function)
    {
        T result = default!;

        Run((Action)(() => result = function()));

        return result;
    }
}
