using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Services.Settings;

namespace Deskair.Tests.Fakes;

/// <summary>Builds a <see cref="MetricFormatter"/> with the temperature unit a test needs.</summary>
public static class TestFormatter
{
    public static MetricFormatter Create(ILocalizer localizer, TemperatureUnit unit = TemperatureUnit.Celsius)
    {
        var store = new FakeSettingsStore { Stored = AppSettings.Default with { TemperatureUnit = unit } };
        var settings = new SettingsService(store, new StrongReferenceMessenger());
        settings.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        return new MetricFormatter(localizer, settings);
    }
}
