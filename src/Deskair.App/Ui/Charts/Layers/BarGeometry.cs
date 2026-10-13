using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Where a History bar sits: a pill 56 % as wide as its slot, at most 40 px, centred in the slot (prototype). The average tick and the labels follow the same middle.</summary>
internal static class BarGeometry
{
    private const double WidthShare = 0.56;
    private const double MaximumWidth = 40;
    private const double MinimumWidth = 1;

    public static (double Left, double Right) Horizontal(ChartArea area, ChartBar bar)
    {
        var left = area.X(bar.Start);
        var right = area.X(bar.End);
        var width = Math.Max(MinimumWidth, Math.Min((right - left) * WidthShare, MaximumWidth));
        var middle = (left + right) / 2;

        return (middle - (width / 2), middle + (width / 2));
    }

    public static double Middle(ChartArea area, ChartBar bar) => (area.X(bar.Start) + area.X(bar.End)) / 2;

    /// <summary>The zone of a CO₂ bar, from its average, or null when it has none.</summary>
    public static Zone? ZoneOf(ChartBar bar) => bar.Average is { } average ? ZoneClassifier.Classify(average) : null;
}
