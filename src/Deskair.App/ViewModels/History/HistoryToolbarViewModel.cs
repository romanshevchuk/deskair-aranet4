using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

public sealed partial class HistoryToolbarViewModel : ObservableObject
{
    [ObservableProperty]
    private Metric metric;

    [ObservableProperty]
    private Period range;

    [ObservableProperty]
    private HoursFilterMode hours;

    [ObservableProperty]
    private Period grouping;

    public IReadOnlyList<Period> GroupingOptions()
    {
        return Range switch
        {
            Period.Month => [Period.Day, Period.Week],
            Period.Year => [Period.Week, Period.Month],
            _ => []
        };
    }

    partial void OnRangeChanged(Period value)
    {
        var options = GroupingOptions();
        if (options.Count > 0 && !options.Contains(Grouping))
        {
            Grouping = options[0];
        }
    }
}
