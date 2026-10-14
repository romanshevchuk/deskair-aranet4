using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.Domain.Metrics;

namespace Deskair.App.ViewModels.Live;

/// <summary>
/// What the Live chart shows: the metric and the range. The metric is chosen with the hero (CO₂) and the mini cards (POL-06); there are no metric tabs.
/// </summary>
public sealed partial class MetricTabsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRibbon))]
    [NotifyPropertyChangedFor(nameof(IsCo2Selected))]
    private Metric selectedMetric;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRibbon))]
    private LiveRange selectedRange;

    public bool ShowRibbon => SelectedMetric == Metric.Co2 && SelectedRange is LiveRange.OneHour or LiveRange.SixHours;

    /// <summary>The hero card is the chosen one (it chooses CO₂).</summary>
    public bool IsCo2Selected => SelectedMetric == Metric.Co2;

    /// <summary>Shows a metric in the chart, keeping the range (clicking the hero or a mini card).</summary>
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
