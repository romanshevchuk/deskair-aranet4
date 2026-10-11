namespace Deskair.Domain.Zones;

public static class ZoneClassifier
{
    public static Zone Classify(double co2Ppm) => Classify(co2Ppm, ZoneThresholds.Default);

    public static Zone Classify(double co2Ppm, ZoneThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        if (double.IsNaN(co2Ppm))
        {
            throw new ArgumentOutOfRangeException(nameof(co2Ppm), "A reading must be a number.");
        }

        if (co2Ppm > thresholds.HighAbovePpm)
        {
            return Zone.High;
        }

        if (co2Ppm >= thresholds.ElevatedFromPpm)
        {
            return Zone.Elevated;
        }

        return Zone.Good;
    }
}
