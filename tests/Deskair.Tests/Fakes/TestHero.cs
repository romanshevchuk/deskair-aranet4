using Deskair.App.Localization;
using Deskair.App.ViewModels.Live;
using Deskair.Domain.Time;

namespace Deskair.Tests.Fakes;

/// <summary>Builds a <see cref="HeroViewModel"/> with fake text and a UTC calendar.</summary>
public static class TestHero
{
    public static HeroViewModel Create(ILocalizer? localizer = null)
    {
        var text = localizer ?? new FakeLocalizer();

        var formatter = TestFormatter.Create(text);

        return new HeroViewModel(text, new HeroStatsBuilder(text, formatter, new LocalCalendar(TimeZoneInfo.Utc)), formatter);
    }
}
