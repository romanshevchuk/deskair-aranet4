using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

public sealed partial class TypicalDayViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<HourBar> bars = [];

    public void Update(IReadOnlyList<HourOfDayAverage> hours)
    {
        var worstStart = TypicalDayCalculator.WorstWindowStartHour(hours);

        Bars = hours
            .Select(hour => new HourBar(
                hour.Hour,
                hour.Average,
                hour.Average is { } average ? ZoneClassifier.Classify(average) : null,
                worstStart is { } start && (hour.Hour - start + 24) % 24 < 3))
            .ToList();
    }
}
