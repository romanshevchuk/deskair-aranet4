using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;

namespace Deskair.App.ViewModels.Live;

public sealed partial class MiniCardsViewModel : ObservableObject
{
    private readonly ILocalizer localizer;
    private readonly MetricFormatter formatter;

    [ObservableProperty]
    private IReadOnlyList<MiniCardItem> items = [];

    public MiniCardsViewModel(ILocalizer localizer, MetricFormatter formatter)
    {
        this.localizer = localizer;
        this.formatter = formatter;
    }

    public void Update(IReadOnlyList<Reading> recent, Metric selected, DateTimeOffset now)
    {
        Items = MetricDescriptor.All
            .Where(descriptor => descriptor.Metric != selected)
            .Select(descriptor => ToItem(descriptor, recent, now))
            .ToList();
    }

    private MiniCardItem ToItem(MetricDescriptor descriptor, IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        var title = localizer.Get(descriptor.NameKey);

        if (recent.Count == 0)
        {
            return new MiniCardItem(descriptor.Metric, title, null, null, [], formatter.Unit(descriptor.Metric));
        }

        var metric = descriptor.Metric;
        var latest = recent.MaxBy(reading => reading.Timestamp).ValueOf(metric);
        var sparkline = recent.Select(reading => new ChartPoint(reading.Timestamp, reading.ValueOf(metric))).ToList();

        return new MiniCardItem(metric, title, formatter.Value(metric, latest), Subtitle(descriptor, latest, recent, now), sparkline, formatter.Unit(metric));
    }

    private string Subtitle(MetricDescriptor descriptor, double latest, IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        if (descriptor is { IdealMin: { } min, IdealMax: { } max } && (latest < min || latest > max))
        {
            var side = latest < min ? "Low" : "High";

            return localizer.Format(
                $"Mini_{descriptor.Metric}_{side}",
                ("min", Math.Round(formatter.ToDisplay(descriptor.Metric, min))),
                ("max", Math.Round(formatter.ToDisplay(descriptor.Metric, max))),
                ("unit", formatter.Unit(descriptor.Metric)));
        }

        var trend = descriptor.Metric == Metric.Co2
            ? TrendCalculator.Co2(recent, now)
            : TrendCalculator.Other(recent, descriptor.Metric, now);

        return localizer.Get($"Trend_{trend.Direction}");
    }
}
