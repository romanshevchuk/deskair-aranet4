namespace Deskair.App.Ui.Charts;

/// <summary>Value ticks for the y axis.</summary>
public static class ChartTicks
{
    private static readonly double[] steps = [0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000];

    /// <summary>
    /// <paramref name="count"/> evenly spaced values from <paramref name="min"/> to <paramref name="max"/>, both included.
    /// Zero or less gives none; one gives the minimum.
    /// </summary>
    public static IReadOnlyList<double> Even(double min, double max, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        if (count == 1)
        {
            return [min];
        }

        return Enumerable.Range(0, count).Select(index => min + ((max - min) * index / (count - 1))).ToList();
    }

    private static List<double> Multiples(double min, double max, double step)
    {
        var ticks = new List<double>();

        for (var value = Math.Ceiling((min - 1e-9) / step) * step; value <= max + 1e-9; value += step)
        {
            ticks.Add(Math.Round(value, 6));
        }

        return ticks;
    }

    /// <summary>Round values (multiples of 0.1, 0.2, 0.5, 1, 2, 5, 10 and so on) inside the range, at most <paramref name="maxCount"/> of them.</summary>
    public static IReadOnlyList<double> Nice(double min, double max, int maxCount)
    {
        if (max <= min || maxCount <= 0)
        {
            return [];
        }

        var step = steps.FirstOrDefault(candidate => Multiples(min, max, candidate).Count <= maxCount);

        return Multiples(min, max, step == 0 ? steps[^1] : step);
    }
}
