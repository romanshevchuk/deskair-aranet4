using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Metrics;

namespace Deskair.App.ViewModels.Live;

public sealed partial class MetricTabsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRibbon))]
    private Metric selectedMetric;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRibbon))]
    private LiveRange selectedRange;

    public bool ShowRibbon => SelectedMetric == Metric.Co2 && SelectedRange is LiveRange.OneHour or LiveRange.SixHours;

    public void Select(Metric metric, LiveRange range)
    {
        SelectedMetric = metric;
        SelectedRange = range;
    }
}
