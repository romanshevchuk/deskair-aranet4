using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

public sealed partial class TypicalDayViewModel : ObservableObject
{
    private readonly ILocalizer? localizer;

    [ObservableProperty]
    private IReadOnlyList<HourBar> bars = [];

    /// <summary>"worst 15–18h": the worst three hours, shown above the dashed outline around them. Empty when there is none.</summary>
    [ObservableProperty]
    private string worstText = string.Empty;

    public TypicalDayViewModel(ILocalizer? localizer = null)
    {
        this.localizer = localizer;
    }

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

        WorstText = worstStart is { } first && localizer is not null
            ? localizer.Format("History_TypicalDay_Worst", ("from", $"{first:00}"), ("to", $"{(first + 3) % 24:00}"))
            : string.Empty;
    }
}
