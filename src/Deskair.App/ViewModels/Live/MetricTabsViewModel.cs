using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    /// <summary>Shows a metric in the chart, keeping the range (clicking a mini card).</summary>
    [RelayCommand]
    private void SelectMetric(Metric metric)
    {
        SelectedMetric = metric;
    }

    public void Select(Metric metric, LiveRange range)
    {
        SelectedMetric = metric;
        SelectedRange = range;
    }
}
