using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Services.Settings;

namespace Deskair.Tests.Services;

public class AppSettingsTests
{
    [Fact]
    public void Default_notifies_at_Elevated_and_High_with_quiet_hours_off_and_the_night_from_22_00_to_07_00()
    {
        var settings = AppSettings.Default;

        Assert.True(settings.AlertsEnabled);
        Assert.True(settings.NotifyAtElevated);
        Assert.True(settings.NotifyAtHigh);
        Assert.False(settings.QuietHoursEnabled);
        Assert.Equal(new TimeOnly(22, 0), settings.QuietHoursStart);
        Assert.Equal(new TimeOnly(7, 0), settings.QuietHoursEnd);
        Assert.Equal(Deskair.Domain.Metrics.TemperatureUnit.Celsius, settings.TemperatureUnit);
        Assert.Null(settings.SelectedDevice);
        Assert.False(settings.ShowValueOnTrayIcon);
        Assert.Equal(new TimeOnly(22, 0), settings.NightStart);
        Assert.Equal(new TimeOnly(7, 0), settings.NightEnd);
        Assert.Equal(NightWindow.Default, settings.Night);
    }

    [Fact]
    public void Settings_with_the_same_values_are_equal()
    {
        var changed = AppSettings.Default with { QuietHoursEnabled = true };

        Assert.NotEqual(AppSettings.Default, changed);
        Assert.Equal(changed, AppSettings.Default with { QuietHoursEnabled = true });
    }
}
