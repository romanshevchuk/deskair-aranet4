using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// The line above the main History chart: the metric ("CO₂"), what one point or square stands for ("hourly", "by hour") and, on the right, the legend of the Day line (line,
/// typical day, night band, comfortable range) and the hatched "No data" swatch when there are gaps. The grid draws its own legend.
/// </summary>
public sealed partial class ChartHeaderViewModel : ObservableObject
{
    private readonly ILocalizer localizer;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private string legendText = string.Empty;

    [ObservableProperty]
    private bool showNoDataLegend;

    /// <summary>The Day legend: the CO₂ line, the typical day (CO₂ only) and the night band.</summary>
    [ObservableProperty]
    private bool showDayLegend;

    /// <summary>The CO₂ line and the typical day are in the Day legend; for the other metrics the legend has the night band only.</summary>
    [ObservableProperty]
    private bool showDayCo2Legend;

    /// <summary>The Day legend shows the typical day line: CO₂, Temperature and Humidity.</summary>
    [ObservableProperty]
    private bool showDayTypicalLegend;

    /// <summary>The Day legend shows the comfortable range swatch: Temperature and Humidity.</summary>
    [ObservableProperty]
    private bool showComfortLegend;

    /// <summary>The metric of the Day chart, for the colour of the comfort range swatch.</summary>
    [ObservableProperty]
    private Metric legendMetric;

    public ChartHeaderViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    /// <summary>The Day line: "CO₂ · hourly", with the legend of what the line, the typical day and the bands mean.</summary>
    public void UpdateForDay(Metric metric, bool hasGaps)
    {
        Title = localizer.Get($"Metric_{metric}");
        Subtitle = localizer.Get("History_Chart_Hourly");
        ShowDayLegend = metric != Metric.Pressure;
        ShowDayCo2Legend = metric == Metric.Co2;
        ShowDayTypicalLegend = metric is Metric.Co2 or Metric.Temperature or Metric.Humidity;
        ShowComfortLegend = metric is Metric.Temperature or Metric.Humidity;
        LegendMetric = metric;
        LegendText = metric != Metric.Co2 ? localizer.Get("History_Legend_Line") : string.Empty;
        ShowNoDataLegend = hasGaps;
    }

    /// <summary>The History grid (D-0018): "CO₂ · by hour", or "by day" for the Year grid; the grid draws its own legend, so the header shows none.</summary>
    public void UpdateForGrid(Metric metric, bool byDay = false)
    {
        ShowChartTitle(metric, byDay ? "History_Chart_ByDay" : "History_Chart_ByHour", hasGaps: false);
    }

    /// <summary>The Pressure line for Week, Month and Year: "Pressure · hourly averages", or "daily averages" for Year.</summary>
    public void UpdateForLine(Metric metric, bool byDay, bool hasGaps)
    {
        ShowChartTitle(metric, byDay ? "History_Chart_MeansByDay" : "History_Chart_MeansByHour", hasGaps);
    }

    private void ShowChartTitle(Metric metric, string subtitleKey, bool hasGaps)
    {
        Title = localizer.Get($"Metric_{metric}");
        Subtitle = localizer.Get(subtitleKey);
        ShowDayLegend = false;
        ShowDayCo2Legend = false;
        ShowDayTypicalLegend = false;
        ShowComfortLegend = false;
        LegendText = string.Empty;
        ShowNoDataLegend = hasGaps;
    }
}
