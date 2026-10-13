using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Analytics.HoursFilter;
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

    /// <summary>"average by hour, last 7 days": the line next to the typical day title. The days are the ones the typical day is built from.</summary>
    [ObservableProperty]
    private string typicalDaySubtitle = string.Empty;

    public ChartHeaderViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
    }

    public void Update(Metric metric, Period range, Period grouping, HoursFilterMode hours, bool hasGaps)
    {
        var hourly = range == Period.Day;
        var what = localizer.Get(hourly ? "History_Chart_Hourly" : grouping switch
        {
            Period.Month => "History_Chart_Monthly",
            Period.Week => "History_Chart_Weekly",
            _ => "History_Chart_Daily"
        });

        Title = localizer.Get($"Metric_{metric}");

        // The Day line always shows the last 24 hours (the night filter then belongs to the sleep card), so it never says "nights only".
        Subtitle = hours == HoursFilterMode.Night && !hourly ? localizer.Format("History_Chart_NightsOnly", ("what", what)) : what;
        ShowZoneLegend = metric == Metric.Co2;
        LegendText = metric == Metric.Co2 ? string.Empty : localizer.Get(hourly ? "History_Legend_Line" : "History_Legend_Bars");
        ShowNoDataLegend = hasGaps;
        TypicalDaySubtitle = localizer.Format("History_TypicalDay_Subtitle", ("period", localizer.Get(range switch
        {
            Period.Month => "History_TypicalDay_Last30",
            Period.Year => "History_TypicalDay_Last90",
            _ => "History_TypicalDay_Last7"
        })));
    }
}
