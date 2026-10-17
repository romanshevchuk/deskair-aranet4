using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// The line above the main History chart: the metric ("CO₂"), what one bar or point stands for ("daily range &amp; average", "· nights only") and, on the right,
/// how to read the chart: the three zones for CO₂, "Bar = min–max · tick = average" or "Line = hourly average" for the others, and the hatched "No data" swatch when there are gaps.
/// </summary>
public sealed partial class ChartHeaderViewModel : ObservableObject
{
    private readonly ILocalizer localizer;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private bool showZoneLegend;

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

    /// <summary>"average by hour, last 7 days": the line next to the typical day title. The days are the ones the typical day is built from.</summary>
    [ObservableProperty]
    private string typicalDaySubtitle = string.Empty;

    public ChartHeaderViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    /// <summary>The History grid (D-0018): "CO₂ · by hour", or "by day" for the Year grid; the grid draws its own legend, so the header shows none.</summary>
    public void UpdateForGrid(Metric metric, bool byDay = false)
    {
        Title = localizer.Get($"Metric_{metric}");
        Subtitle = localizer.Get(byDay ? "History_Chart_ByDay" : "History_Chart_ByHour");
        ShowZoneLegend = false;
        LegendText = string.Empty;
        ShowNoDataLegend = false;
    }

    /// <summary>The Pressure line for Week, Month and Year: "Pressure · hourly averages", or "daily averages" for Year. Call after <see cref="Update"/>.</summary>
    public void UpdateForLine(bool byDay)
    {
        Subtitle = localizer.Get(byDay ? "History_Chart_MeansByDay" : "History_Chart_MeansByHour");
        LegendText = string.Empty;
    }

    public void Update(Metric metric, Period range, Period grouping, bool hasGaps)
    {
        var hourly = range == Period.Day;
        var what = localizer.Get(hourly ? "History_Chart_Hourly" : grouping switch
        {
            Period.Month => "History_Chart_Monthly",
            Period.Week => "History_Chart_Weekly",
            _ => "History_Chart_Daily"
        });

        Title = localizer.Get($"Metric_{metric}");

        Subtitle = what;
        ShowDayLegend = hourly && metric != Metric.Pressure;
        ShowDayCo2Legend = hourly && metric == Metric.Co2;
        ShowDayTypicalLegend = hourly && metric is Metric.Co2 or Metric.Temperature or Metric.Humidity;
        ShowComfortLegend = hourly && metric is Metric.Temperature or Metric.Humidity;
        LegendMetric = metric;
        ShowZoneLegend = metric == Metric.Co2 && !hourly;
        LegendText = metric == Metric.Co2 ? string.Empty : localizer.Get(hourly ? "History_Legend_Line" : "History_Legend_Bars");
        ShowNoDataLegend = hasGaps;
        TypicalDaySubtitle = localizer.Format("History_TypicalDay_Subtitle", ("period", localizer.Get(range switch
        {
            Period.Day => "History_TypicalDay_Before7",
            Period.Week => "History_TypicalDay_These7",
            Period.Month => "History_TypicalDay_These30",
            _ => "History_TypicalDay_These12Months"
        })));
    }
}
