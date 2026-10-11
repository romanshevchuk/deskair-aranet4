using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;

namespace Deskair.Domain.Analytics.Trend;

public static class TrendCalculator
{
    // TODO(LIVE-01a) [M, haiku]
    // Spec: docs/product/behaviour-rules.md "Live values" (Trend).
    // Input : recent readings (any order), the current instant.
    // Output: TrendResult for CO₂.
    // Rules : average of the last 6 min minus the average 30 min earlier; under 30 ppm is Steady; Window is 30 min.
    // Edge  : if either average has no readings, return Steady with Change 0 (see questions.md Q1).
    // Done  : unskip tests Domain/Analytics/TrendCalculatorTests.Co2_* and make them pass.
    public static TrendResult Co2(IReadOnlyList<Reading> readings, DateTimeOffset now)
        => throw new NotImplementedException("TODO(LIVE-01a)");

    // TODO(LIVE-01b) [S, haiku]
    // Spec: docs/product/behaviour-rules.md "Live values" (Trend, second sentence).
    // Input : readings, metric (Temperature, Humidity or Pressure), the current instant.
    // Output: TrendResult over the last hour.
    // Rules : Steady unless the change reaches 0.5 °C, 3 % or 1 hPa.
    // Edge  : Metric.Co2 throws ArgumentOutOfRangeException; no readings returns Steady.
    // Done  : unskip tests Domain/Analytics/TrendCalculatorTests.Other_* and make them pass.
    public static TrendResult Other(IReadOnlyList<Reading> readings, Metric metric, DateTimeOffset now)
        => throw new NotImplementedException("TODO(LIVE-01b)");
}
