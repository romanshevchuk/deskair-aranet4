using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.Coverage;

namespace Deskair.App.ViewModels.History;

public sealed partial class StatsViewModel : ObservableObject
{
    [ObservableProperty]
    private double? average;

    [ObservableProperty]
    private double? highest;

    [ObservableProperty]
    private double? lowest;

    /// <summary>Readings present divided by expected, from 0 to 1.</summary>
    [ObservableProperty]
    private double coverage;

    public void Update(IReadOnlyList<Bucket> buckets)
    {
        var withData = buckets.Where(bucket => bucket.ReadingCount > 0 && bucket.Average is not null).ToList();
        var readings = withData.Sum(bucket => bucket.ReadingCount);

        Average = readings == 0 ? null : withData.Sum(bucket => bucket.Average!.Value * bucket.ReadingCount) / readings;
        Highest = withData.Count == 0 ? null : withData.Max(bucket => bucket.Maximum);
        Lowest = withData.Count == 0 ? null : withData.Min(bucket => bucket.Minimum);
        Coverage = CoverageCalculator.Fraction(readings, buckets.Sum(bucket => bucket.ExpectedCount));
    }
}
