using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.History;

/// <summary>
/// The sleep card (History, Day plus Night): the verdict on last night's air and three numbers. For CO₂ the numbers are the time at or above the Elevated limit,
/// the peak with its time and the deep sleep average; other metrics have no verdict and show their lowest value first. Times are local. A night without readings says so.
/// </summary>
public sealed partial class SleepViewModel : ObservableObject
{
    private const string Dash = "–";

    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;
    private readonly LocalCalendar calendar;

    [ObservableProperty]
    private bool hasData;

    [ObservableProperty]
    private bool hasVerdict;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string verdictText = string.Empty;

    [ObservableProperty]
    private Zone verdictZone;

    [ObservableProperty]
    private string firstLabel = string.Empty;

    [ObservableProperty]
    private string firstValue = string.Empty;

    [ObservableProperty]
    private string peakLabel = string.Empty;

    [ObservableProperty]
    private string peakValue = string.Empty;

    [ObservableProperty]
    private string deepLabel = string.Empty;

    [ObservableProperty]
    private string deepValue = string.Empty;

    [ObservableProperty]
    private string tip = string.Empty;

    [ObservableProperty]
    private string? partialNote;

    public SleepViewModel(ILocalizer localizer, MetricFormatter formatter, LocalCalendar calendar)
    {
        this.localizer = localizer;
        this.formatter = formatter;
        this.calendar = calendar;
    }

    public bool ShowPartialNote => PartialNote is not null;

    partial void OnPartialNoteChanged(string? value) => OnPropertyChanged(nameof(ShowPartialNote));

    /// <summary>Shows the night, or nothing when there is none (the night window is empty).</summary>
    public void Update(SleepSummary? summary)
    {
        if (summary is null)
        {
            Clear();
            return;
        }

        Title = localizer.Format("Sleep_Title", ("from", Clock(summary.Start)), ("to", Clock(summary.End)));
        HasData = summary.HasData;
        HasVerdict = summary.Verdict is not null;

        if (!summary.HasData)
        {
            ClearNumbers();
            Tip = localizer.Get("Sleep_Empty");
            return;
        }

        var metric = summary.Metric;
        VerdictText = summary.Verdict is { } verdict ? localizer.Get($"Sleep_Verdict_{verdict}") : string.Empty;
        VerdictZone = summary.Verdict switch
        {
            SleepVerdict.Poor => Zone.High,
            SleepVerdict.Fair => Zone.Elevated,
            _ => Zone.Good
        };

        if (summary.TimeElevated is { } elevated)
        {
            var limit = formatter.Value(Metric.Co2, ZoneThresholds.Default.ElevatedFromPpm);
            FirstLabel = localizer.Format("Sleep_TimeAbove", ("limit", limit), ("unit", formatter.Unit(Metric.Co2)));
            FirstValue = localizer.Format("Sleep_HoursOf", ("hours", Hours(elevated)), ("total", Hours(summary.Recorded)));
            Tip = localizer.Get(elevated > TimeSpan.FromHours(1) ? "Sleep_Tip_Ajar" : "Sleep_Tip_Fresh");
        }
        else
        {
            FirstLabel = localizer.Get("Stats_Lowest");
            FirstValue = Value(metric, summary.Lowest?.Value);
            Tip = localizer.Get("Sleep_Tip_Other");
        }

        PeakLabel = localizer.Format("Sleep_Peak", ("time", Clock(summary.Peak?.Time ?? summary.Start)));
        PeakValue = Value(metric, summary.Peak?.Value);
        DeepLabel = localizer.Get("Sleep_Deep");
        DeepValue = Value(metric, summary.DeepSleepAverage);
        PartialNote = summary.IsPartial
            ? localizer.Format("Sleep_Partial", ("recorded", Hours(summary.Recorded)), ("total", Hours(summary.Expected)))
            : null;
    }

    private void Clear()
    {
        HasData = false;
        HasVerdict = false;
        Title = string.Empty;
        ClearNumbers();
        Tip = string.Empty;
    }

    private void ClearNumbers()
    {
        VerdictText = string.Empty;
        FirstLabel = string.Empty;
        FirstValue = string.Empty;
        PeakLabel = string.Empty;
        PeakValue = string.Empty;
        DeepLabel = string.Empty;
        DeepValue = string.Empty;
        PartialNote = null;
    }

    private string Clock(DateTimeOffset instant)
        => TimeZoneInfo.ConvertTime(instant, calendar.Zone).ToString("HH:mm", localizer.Culture);

    private string Hours(TimeSpan span)
        => Math.Round(span.TotalHours, 1).ToString("0.#", localizer.Culture);

    private string Value(Metric metric, double? stored)
        => stored is { } value ? $"{formatter.Value(metric, value)} {formatter.Unit(metric)}" : Dash;
}
