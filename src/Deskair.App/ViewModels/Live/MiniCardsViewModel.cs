using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Comfort;
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

    /// <summary>
    /// Always temperature, humidity and pressure, in that order: CO₂ is the hero. The cards never move; the one the chart shows is marked
    /// selected (screens-and-states.md "Live").
    /// </summary>
    public void Update(IReadOnlyList<Reading> recent, Metric selected, DateTimeOffset now)
    {
        Items = MetricDescriptor.All
            .Where(descriptor => descriptor.Metric != Metric.Co2)
            .Select(descriptor => ToItem(descriptor, recent, now) with { IsSelected = descriptor.Metric == selected })
            .ToList();
    }

    private MiniCardItem ToItem(MetricDescriptor descriptor, IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        var title = localizer.Get(descriptor.NameKey);
        var unit = formatter.Unit(descriptor.Metric);

        if (recent.Count == 0)
        {
            return new MiniCardItem(descriptor.Metric, title, null, null, [], unit, AccessibleName: title);
        }

        var metric = descriptor.Metric;
        var latest = recent.MaxBy(reading => reading.Timestamp).ValueOf(metric);
        var sparkline = recent.Select(reading => new ChartPoint(reading.Timestamp, reading.ValueOf(metric))).ToList();
        var value = formatter.Value(metric, latest);
        var subtitle = Subtitle(descriptor, latest, recent, now);
        var name = localizer.Format("Live_Card_AccessibleName", ("metric", title), ("value", value), ("unit", unit), ("status", subtitle));

        return new MiniCardItem(metric, title, value, subtitle, sparkline, unit, AccessibleName: name);
    }

    private string Subtitle(MetricDescriptor descriptor, double latest, IReadOnlyList<Reading> recent, DateTimeOffset now)
    {
        if (ComfortScale.For(descriptor.Metric) is { } scale)
        {
            // Judge the value as the card shows it: a reading of 19.96 Â°C is shown as 20.0 and must not be called too cool.
            var shown = Math.Round(latest, descriptor.Metric == Metric.Humidity ? 0 : 1, MidpointRounding.AwayFromZero);

            if (scale.Side(shown) != ComfortSide.Inside)
            {
                var side = scale.Side(shown) == ComfortSide.Low ? "Low" : "High";

                // Humidity is "a bit" off up to the scale's far-outside distance and "too" off beyond it (D-0021); temperature is always "too".
                var slight = descriptor.Metric == Metric.Humidity && !scale.IsFarOutside(shown) ? "ABit" : string.Empty;

                return localizer.Format(
                    $"Mini_{descriptor.Metric}_{slight}{side}",
                    ("min", Math.Round(formatter.ToDisplay(descriptor.Metric, scale.Low))),
                    ("max", Math.Round(formatter.ToDisplay(descriptor.Metric, scale.High))),
                    ("unit", formatter.Unit(descriptor.Metric)));
            }
        }
        var trend = descriptor.Metric == Metric.Co2
            ? TrendCalculator.Co2(recent, now)
            : TrendCalculator.Other(recent, descriptor.Metric, now);

        return localizer.Get($"Trend_{trend.Direction}");
    }
}
