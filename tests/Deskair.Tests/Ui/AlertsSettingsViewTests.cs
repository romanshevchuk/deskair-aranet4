using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Ui.Controls;
using Deskair.App.Ui.Views.Settings;
using Deskair.App.ViewModels.Settings;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deskair.Tests.Ui;

/// <summary>Notify at Elevated and High, and quiet hours, only matter while alerts are on, so they turn off with the Alerts switch (UX-30).</summary>
public class AlertsSettingsViewTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_rows_under_Alerts_follow_the_Alerts_switch(bool alertsEnabled)
    {
        var enabled = UiHost.Run(() =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo("en-US"));

            var view = new AlertsSettingsView { DataContext = CreateViewModel(alertsEnabled) };
            view.Measure(new Size(720, 600));
            view.Arrange(new Rect(0, 0, 720, 600));
            view.UpdateLayout();

            return Switches(view).ToDictionary(toggle => AutomationProperties.GetName(toggle), toggle => toggle.IsEnabled);
        });

        Assert.True(enabled["Alerts"]);
        Assert.Equal(alertsEnabled, enabled["Notify at Elevated (1000 ppm)"]);
        Assert.Equal(alertsEnabled, enabled["Notify at High (1400 ppm)"]);
        Assert.Equal(alertsEnabled, enabled["Quiet hours"]);
    }

    private static AlertsSettingsViewModel CreateViewModel(bool alertsEnabled)
    {
        var messenger = new StrongReferenceMessenger();
        var store = new FakeSettingsStore { Stored = AppSettings.Default with { AlertsEnabled = alertsEnabled } };
        var settings = new SettingsService(store, messenger);
        settings.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        var viewModel = new AlertsSettingsViewModel(settings, new FakeDoNotDisturb(), messenger, new FakeUiDispatcher(), NullLogger<AlertsSettingsViewModel>.Instance);
        viewModel.Load();

        return viewModel;
    }

    private static IEnumerable<ToggleSwitch> Switches(DependencyObject element)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);

            if (child is ToggleSwitch toggle)
            {
                yield return toggle;
            }

            foreach (var nested in Switches(child))
            {
                yield return nested;
            }
        }
    }
}
