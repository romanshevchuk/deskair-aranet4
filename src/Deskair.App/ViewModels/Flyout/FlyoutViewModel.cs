using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.Live;
using Deskair.App.ViewModels.Shell;
using Deskair.Domain.Analytics.Ribbon;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Services.Alerts;
using Deskair.Services.Platform;

namespace Deskair.App.ViewModels.Flyout;

/// <summary>
/// What the tray flyout shows: the room, the CO₂ value with its zone, trend and advice, the 24 h ribbon at its own size with the time spent in each zone,
/// and the other readings. The window calls <see cref="Update"/> each time the flyout opens, so the texts are as fresh as the readings.
/// </summary>
public sealed partial class FlyoutViewModel : ObservableObject
{
    private static readonly TimeSpan ribbonLength = TimeSpan.FromHours(24);

    private readonly AirSummaryBuilder summaries;
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;
    private readonly LocalCalendar calendar;
    private readonly IMainWindowController windows;
    private readonly SnoozeService snooze;
    private readonly FriendlyTime time;
    private readonly IBluetoothRadio radio;

    [ObservableProperty]
    private Zone? zone;

    [ObservableProperty]
    private string? zoneText;

    [ObservableProperty]
    private double? co2Ppm;

    /// <summary>The CO₂ value as shown ("1,068").</summary>
    [ObservableProperty]
    private string? valueText;

    [ObservableProperty]
    private string? unitText;

    [ObservableProperty]
    private string? roomName;

    /// <summary>When the shown reading was taken. Null when there is no reading.</summary>
    [ObservableProperty]
    private DateTimeOffset? updatedAt;

    /// <summary>"updated 12 s ago" or "updated 3 min ago".</summary>
    [ObservableProperty]
    private string? updatedText;

    [ObservableProperty]
    private TrendDirection trend;

    [ObservableProperty]
    private string? trendText;

    [ObservableProperty]
    private string? trendDetail;

    [ObservableProperty]
    private string? adviceTitle;

    [ObservableProperty]
    private string? adviceText;

    [ObservableProperty]
    private bool showNotConnected;

    /// <summary>The likely reason under "Can't reach your Aranet4": Bluetooth off, or out of range or asleep.</summary>
    [ObservableProperty]
    private string reasonText = string.Empty;

    /// <summary>The one recovery step: "Turn on Bluetooth" when the radio is off, otherwise waking the sensor.</summary>
    [ObservableProperty]
    private string stepText = string.Empty;

    /// <summary>True when not connected but an earlier reading is known: it is shown faded.</summary>
    [ObservableProperty]
    private bool showLastReading;

    /// <summary>The last known CO₂ value while not connected ("1160").</summary>
    [ObservableProperty]
    private string? lastValueText;

    /// <summary>"ppm at 12:35": the unit and the time of the last known reading.</summary>
    [ObservableProperty]
    private string? lastDetailText;

    [ObservableProperty]
    private IReadOnlyList<ChartZoneSegment> ribbonSegments = [];

    /// <summary>The right end of the ribbon (now).</summary>
    [ObservableProperty]
    private DateTimeOffset? ribbonEnd;

    /// <summary>The times under the ribbon at their true positions: the round hours that fall inside it, then "Now".</summary>
    [ObservableProperty]
    private IReadOnlyList<FlyoutRibbonLabel> ribbonLabels = [];

    /// <summary>The time in each zone over the last 24 hours, for the zones that occurred.</summary>
    [ObservableProperty]
    private IReadOnlyList<FlyoutZoneTime> zoneTimes = [];

    /// <summary>Temperature, humidity and pressure.</summary>
    [ObservableProperty]
    private IReadOnlyList<FlyoutReading> otherReadings = [];

    /// <summary>True while alerts are snoozed: the button then says until when and the menu offers to resume.</summary>
    [ObservableProperty]
    private bool isSnoozed;

    /// <summary>"Snooze", or "Snoozed until 14:30".</summary>
    [ObservableProperty]
    private string snoozeButtonText = string.Empty;

    [ObservableProperty]
    private string snoozeHourText = string.Empty;

    [ObservableProperty]
    private string snoozeTomorrowText = string.Empty;

    [ObservableProperty]
    private string resumeText = string.Empty;

    public FlyoutViewModel(ILocalizer localizer, MetricFormatter formatter, LocalCalendar calendar, IMainWindowController windows, SnoozeService snooze, FriendlyTime time, IBluetoothRadio radio)
    {
        this.radio = radio;
        this.localizer = localizer;
        this.formatter = formatter;
        this.calendar = calendar;
        this.windows = windows;
        this.snooze = snooze;
        this.time = time;
        summaries = new AirSummaryBuilder(localizer);
        RefreshSnooze();
    }

    [RelayCommand]
    private void Open()
    {
        windows.Show();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        windows.Show(ShellSection.Settings);
    }

    /// <param name="reading">The latest reading, or null when the sensor is not connected.</param>
    /// <param name="recent">The readings of the last 24 hours, in time order; the trend and the ribbon come from them. When not connected, the last of them is shown faded.</param>
    public void Update(Reading? reading, IReadOnlyList<Reading> recent, DateTimeOffset now, string? room)
    {
        RoomName = room;
        RefreshSnooze();

        if (reading is not { } latest)
        {
            ShowNoReading(recent, now);
            return;
        }

        var summary = summaries.Build(latest, recent, now);

        Zone = summary.Zone;
        ZoneText = localizer.Get($"Zone_{summary.Zone}");
        Co2Ppm = summary.Co2Ppm;
        ValueText = formatter.Value(Metric.Co2, summary.Co2Ppm);
        UnitText = formatter.Unit(Metric.Co2);
        UpdatedAt = latest.Timestamp;
        UpdatedText = UpdatedSince(now - latest.Timestamp);
        Trend = summary.Trend;
        TrendText = summary.TrendText;
        TrendDetail = summary.TrendDetail;
        AdviceTitle = summary.AdviceTitle;
        AdviceText = summary.AdviceText;
        ShowNotConnected = false;
        ShowLastReading = false;
        LastValueText = null;
        LastDetailText = null;

        var spans = RibbonCalculator.Build(recent, now);

        RibbonSegments = [.. spans.Select(span => new ChartZoneSegment(span.Start, span.End, span.Zone))];
        RibbonEnd = now;
        RibbonLabels = FlyoutRibbonLabels.Build(now, calendar.Zone, localizer.Culture, localizer.Get("Chart_Now"));
        ZoneTimes = ZoneTimesOf(spans);
        OtherReadings =
        [
            Other(Metric.Temperature, latest.TemperatureC),
            Other(Metric.Humidity, latest.HumidityPct),
            Other(Metric.Pressure, latest.PressureHpa)
        ];
    }

    [RelayCommand]
    private void SnoozeHour()
    {
        snooze.ForOneHour();
        RefreshSnooze();
    }

    [RelayCommand]
    private void SnoozeTomorrow()
    {
        snooze.UntilTomorrow();
        RefreshSnooze();
    }

    [RelayCommand]
    private void Resume()
    {
        snooze.Resume();
        RefreshSnooze();
    }

    /// <summary>Looks at the snooze again and rebuilds its texts. A snooze ends without any event, so this runs each time the flyout opens.</summary>
    private void RefreshSnooze()
    {
        var until = snooze.Until;

        IsSnoozed = until is not null;
        SnoozeButtonText = until is { } end ? localizer.Format("Flyout_SnoozedUntil", ("time", time.Text(end))) : localizer.Get("Flyout_Snooze");
        SnoozeHourText = localizer.Get("Toast_Snooze");
        SnoozeTomorrowText = localizer.Get("Tray_SnoozeTomorrow");
        ResumeText = localizer.Get("Flyout_Resume");
    }

    private void ShowNoReading(IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        var last = recent.Count > 0 ? recent[^1] : (Reading?)null;

        Zone = null;
        ZoneText = null;
        Co2Ppm = null;
        ValueText = null;
        UnitText = null;
        UpdatedAt = last?.Timestamp;
        UpdatedText = last is { } seen ? localizer.Format("Flyout_LastSeen", ("ago", Duration(now - seen.Timestamp))) : null;
        Trend = TrendDirection.Steady;
        TrendText = null;
        TrendDetail = null;
        AdviceTitle = null;
        AdviceText = null;
        ShowNotConnected = true;

        var bluetoothOff = !radio.IsOn;

        ReasonText = localizer.Get(bluetoothOff ? "Offline_Text_BluetoothOff" : "Offline_Text");
        StepText = localizer.Get(bluetoothOff ? "Offline_Bluetooth" : "Offline_Step_Wake");
        ShowLastReading = last is not null;
        LastValueText = last is { } shown ? formatter.Value(Metric.Co2, shown.Co2Ppm) : null;
        LastDetailText = last is { } at
            ? localizer.Format("Flyout_LastReadingAt", ("unit", formatter.Unit(Metric.Co2)), ("time", TimeZoneInfo.ConvertTime(at.Timestamp, calendar.Zone).ToString("HH:mm", localizer.Culture)))
            : null;
        RibbonSegments = [];
        RibbonEnd = null;
        RibbonLabels = [];
        ZoneTimes = [];
        OtherReadings = [];
    }

    private string UpdatedSince(TimeSpan age)
    {
        return age < TimeSpan.FromMinutes(1)
            ? localizer.Format("Flyout_Updated", ("sec", Math.Max(0, (int)age.TotalSeconds)))
            : localizer.Format("Flyout_UpdatedMinutes", ("min", (int)age.TotalMinutes));
    }

    private List<FlyoutZoneTime> ZoneTimesOf(IReadOnlyList<ZoneSpan> spans)
    {
        return [.. spans
            .GroupBy(span => span.Zone)
            .OrderBy(group => group.Key)
            .Select(group => new FlyoutZoneTime(group.Key, Duration(TimeSpan.FromTicks(group.Sum(span => (span.End - span.Start).Ticks)))))];
    }

    private string Duration(TimeSpan time) => localizer.Duration((int)Math.Round(time.TotalMinutes));

    private FlyoutReading Other(Metric metric, double stored)
    {
        return new FlyoutReading(localizer.Get($"Metric_{metric}"), formatter.Value(metric, stored), formatter.Unit(metric));
    }
}
