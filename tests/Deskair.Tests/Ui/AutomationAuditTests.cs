using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Deskair.App.Ui.Views.Shell;
using Deskair.Domain.Metrics;
using Deskair.Services.Connection;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

/// <summary>
/// A cheap stand-in for a screen-reader session: walks the automation tree of a screen and fails on any enabled, interactive control that has no name,
/// because Narrator would announce it as just "button" or "edit" (accessibility.md "Screen readers").
/// </summary>
public class AutomationAuditTests
{
    private static readonly AutomationControlType[] interactive =
    [
        AutomationControlType.Button,
        AutomationControlType.CheckBox,
        AutomationControlType.ComboBox,
        AutomationControlType.Edit,
        AutomationControlType.ListItem,
        AutomationControlType.MenuItem,
        AutomationControlType.RadioButton,
        AutomationControlType.Slider,
        AutomationControlType.TabItem,
        AutomationControlType.SplitButton
    ];

    [Fact]
    public void Every_interactive_control_on_Live_has_a_name()
    {
        Audit(() => MainWindowRenderTests.BuildShell(ConnectionState.Normal, TemperatureUnit.Celsius), minimum: 8);
    }

    [Fact]
    public void Every_interactive_control_on_the_offline_screen_has_a_name()
    {
        Audit(() => MainWindowRenderTests.BuildShell(ConnectionState.NotConnected, TemperatureUnit.Celsius), minimum: 3);
    }

    [Fact]
    public void Every_interactive_control_on_Settings_has_a_name()
    {
        Audit(() => SettingsRenderTests.BuildShell(ThemeChoice.Light, "en-US"), minimum: 10);
    }

    /// <summary>Fails when a control has no name, or when fewer than <paramref name="minimum"/> interactive controls were found (the walk itself would be broken).</summary>
    private static void Audit(Func<Deskair.App.ViewModels.Shell.ShellViewModel> build, int minimum)
    {
        var (unnamed, count) = Unnamed(build);

        Assert.Empty(unnamed);
        Assert.True(count >= minimum, $"Only {count} interactive controls found; expected at least {minimum}.");
    }

    private static (List<string> Unnamed, int Count) Unnamed(Func<Deskair.App.ViewModels.Shell.ShellViewModel> build)
    {
        return UiHost.Run(() =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo("en-US"));

            var shell = build();
            var window = new MainWindow(shell);
            var content = (FrameworkElement)window.Content;

            content.DataContext = shell;
            window.Content = null;
            content.Measure(new Size(1200, 700));
            content.Arrange(new Rect(0, 0, 1200, 700));
            content.UpdateLayout();

            var found = new List<string>();
            var count = 0;
            Walk(content, found, ref count, depth: 0);

            return (found, count);
        });
    }

    /// <summary>Visits every element of the visual tree and asks it for its peer: the root and most layout panels have none, so the peer tree alone would stop at once.</summary>
    private static void Walk(DependencyObject element, List<string> found, ref int count, int depth)
    {
        // A collapsed or hidden element cannot be reached, and neither can its children.
        if (depth > 200 || (element is UIElement { Visibility: not Visibility.Visible }))
        {
            return;
        }

        // Parts that cannot take the keyboard focus (the scroll bar track) are left out.
        if (element is UIElement { Focusable: true } ui && UIElementAutomationPeer.CreatePeerForElement(ui) is { } peer && peer.IsEnabled() && interactive.Contains(peer.GetAutomationControlType()))
        {
            count++;

            if (string.IsNullOrWhiteSpace(peer.GetName()))
            {
                found.Add($"{peer.GetClassName()} id={peer.GetAutomationId()} content={(ui as ContentControl)?.Content ?? "-"} parent={(System.Windows.Media.VisualTreeHelper.GetParent(ui) as FrameworkElement)?.GetType().Name}/{(ui as FrameworkElement)?.DataContext?.GetType().Name}");
            }
        }

        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(element); index++)
        {
            Walk(System.Windows.Media.VisualTreeHelper.GetChild(element, index), found, ref count, depth + 1);
        }
    }
}