using Deskair.App.Ui.Charts;
using Deskair.Domain.Metrics;

namespace Deskair.Tests.Ui;

public class SparklineRangeTests
{
    [Theory]
    [InlineData(Metric.Temperature, 1)]
    [InlineData(Metric.Humidity, 6)]
    [InlineData(Metric.Pressure, 2)]
    [InlineData(Metric.Co2, 0)]
    public void Each_metric_has_its_minimum_span(Metric metric, double expected)
        => Assert.Equal(expected, SparklineRange.MinimumSpan(metric));

    [Fact]
    public void A_steady_temperature_is_stretched_to_one_degree_around_its_middle()
    {
        var (low, high) = SparklineRange.Fit(Metric.Temperature, 20.6, 21.0);

        Assert.Equal(20.3, low, 6);
        Assert.Equal(21.3, high, 6);
    }

    [Fact]
    public void A_flat_pressure_is_centred_in_two_hectopascals()
    {
        var (low, high) = SparklineRange.Fit(Metric.Pressure, 990.4, 990.4);

        Assert.Equal(989.4, low, 6);
        Assert.Equal(991.4, high, 6);
    }

    [Fact]
    public void Humidity_with_two_percent_of_movement_gets_six()
    {
        var (low, high) = SparklineRange.Fit(Metric.Humidity, 50, 52);

        Assert.Equal(6, high - low, 6);
        Assert.Equal(51, (low + high) / 2, 6);
    }

    [Fact]
    public void Data_wider_than_the_minimum_keeps_its_own_range()
    {
        Assert.Equal((19.0, 23.5), SparklineRange.Fit(Metric.Temperature, 19.0, 23.5));
    }

    [Fact]
    public void Co2_keeps_its_own_range()
    {
        Assert.Equal((800.0, 810.0), SparklineRange.Fit(Metric.Co2, 800, 810));
    }
}
