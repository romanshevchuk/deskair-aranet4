using Deskair.Domain.Metrics;

namespace Deskair.Tests.Domain.Metrics;

public class MetricDescriptorTests
{
    [Fact]
    public void All_has_exactly_the_four_v1_metrics()
    {
        var metrics = MetricDescriptor.All.Select(descriptor => descriptor.Metric);

        Assert.Equal(new[] { Metric.Co2, Metric.Temperature, Metric.Humidity, Metric.Pressure }, metrics);
    }

    [Theory]
    [InlineData(Metric.Temperature, 20, 24)]
    [InlineData(Metric.Humidity, 30, 50)]
    public void Comfort_ranges_match_the_behaviour_rules(Metric metric, double min, double max)
    {
        var descriptor = MetricDescriptor.For(metric);

        Assert.Equal(min, descriptor.IdealMin);
        Assert.Equal(max, descriptor.IdealMax);
    }

    [Theory]
    [InlineData(Metric.Co2)]
    [InlineData(Metric.Pressure)]
    public void Metrics_without_an_ideal_range_have_none(Metric metric)
    {
        var descriptor = MetricDescriptor.For(metric);

        Assert.Null(descriptor.IdealMin);
        Assert.Null(descriptor.IdealMax);
    }

    [Fact]
    public void Colour_tokens_are_unique()
    {
        var tokens = MetricDescriptor.All.Select(descriptor => descriptor.ColorToken);

        Assert.Equal(MetricDescriptor.All.Count, tokens.Distinct().Count());
    }
}
