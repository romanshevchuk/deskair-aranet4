using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;

namespace Deskair.App.ViewModels.Live;

public sealed partial class HeroViewModel : ObservableObject
{
    private readonly AirSummaryBuilder summaries;
    private readonly HeroStatsBuilder statsBuilder;
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;

    [ObservableProperty]
    private Zone? zone;

    [ObservableProperty]
    private double? co2Ppm;

    /// <summary>The value as text in the selected language ("1,068"). Null without a reading.</summary>
    [ObservableProperty]
    private string? valueText;

    [ObservableProperty]
    private string? unitText;

    /// <summary>The zone in words, for the pill ("Elevated").</summary>
    [ObservableProperty]
    private string? zoneText;

    [ObservableProperty]
    private TrendDirection trend;

    [ObservableProperty]
    private string? trendText;

    /// <summary>The observed change, for example "254 ppm over 30 min". Null when the trend is steady.</summary>
    [ObservableProperty]
    private string? trendDetail;

    [ObservableProperty]
    private string? adviceTitle;

    [ObservableProperty]
    private string? adviceText;

    [ObservableProperty]
    private IReadOnlyList<StatItem> stats = [];

    /// <summary>What Narrator reads for the hero, which chooses CO₂ for the chart ("CO₂, 1151 ppm, Elevated"). Just "CO₂" without a reading.</summary>
    [ObservableProperty]
    private string? accessibleName;

    public HeroViewModel(ILocalizer localizer, HeroStatsBuilder statsBuilder, MetricFormatter formatter)
    {
        summaries = new AirSummaryBuilder(localizer);
        this.statsBuilder = statsBuilder;
        this.localizer = localizer;
        this.formatter = formatter;
        UnitText = formatter.Unit(Domain.Metrics.Metric.Co2);
        AccessibleName = localizer.Get("Metric_Co2");
    }

    public void Update(Reading? reading, IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        if (reading is not { } latest)
        {
            Clear();
            return;
        }

        var summary = summaries.Build(latest, recent, now);

        Zone = summary.Zone;
        Co2Ppm = summary.Co2Ppm;
        ValueText = formatter.Value(Domain.Metrics.Metric.Co2, summary.Co2Ppm);
        ZoneText = localizer.Get($"Zone_{summary.Zone}");
        Trend = summary.Trend;
        TrendText = summary.TrendText;
        TrendDetail = summary.TrendDetail;
        AdviceTitle = summary.AdviceTitle;
        AdviceText = summary.AdviceText;
        Stats = statsBuilder.Build(recent, now);
        AccessibleName = localizer.Format(
            "Live_Card_AccessibleName",
            ("metric", localizer.Get("Metric_Co2")),
            ("value", ValueText),
            ("unit", UnitText),
            ("status", ZoneText));
    }

    private void Clear()
    {
        Zone = null;
        Co2Ppm = null;
        ValueText = null;
        ZoneText = null;
        Trend = TrendDirection.Steady;
        TrendText = null;
        TrendDetail = null;
        AdviceTitle = null;
        AdviceText = null;
        Stats = [];
        AccessibleName = localizer.Get("Metric_Co2");
    }
}
