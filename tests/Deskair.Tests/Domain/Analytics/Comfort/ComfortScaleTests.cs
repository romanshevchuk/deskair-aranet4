using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Metrics;

namespace Deskair.Tests.Domain.Analytics.Comfort;

public class ComfortScaleTests
{
    [Theory]
    [InlineData(17.9, 0)]
    [InlineData(18.0, 1)]
    [InlineData(19.9, 1)]
    [InlineData(20.0, 2)]
    [InlineData(23.9, 2)]
    [InlineData(24.0, 2)]
    [InlineData(24.1, 3)]
    [InlineData(25.9, 3)]
    [InlineData(26.0, 3)]
    [InlineData(26.1, 4)]
    public void Temperature_steps_put_a_value_on_a_limit_in_the_step_nearer_the_comfortable_range(double celsius, int step)
        => Assert.Equal(step, ComfortScale.Temperature.Step(celsius));

    [Theory]
    [InlineData(24, 0)]
    [InlineData(25, 1)]
    [InlineData(30, 2)]
    [InlineData(49, 2)]
    [InlineData(50, 2)]
    [InlineData(51, 3)]
    [InlineData(60, 3)]
    [InlineData(61, 4)]
    public void Humidity_steps_follow_the_limits(double percent, int step)
        => Assert.Equal(step, ComfortScale.Humidity.Step(percent));

    [Fact]
    public void Only_temperature_and_humidity_have_a_scale()
    {
        Assert.Same(ComfortScale.Temperature, ComfortScale.For(Metric.Temperature));
        Assert.Same(ComfortScale.Humidity, ComfortScale.For(Metric.Humidity));
        Assert.Null(ComfortScale.For(Metric.Co2));
        Assert.Null(ComfortScale.For(Metric.Pressure));
    }

    [Fact]
    public void Side_and_distance_outside_the_range()
    {
        var scale = ComfortScale.Temperature;

        Assert.Equal(ComfortSide.Low, scale.Side(19.9));
        Assert.Equal(ComfortSide.Inside, scale.Side(20));
        Assert.Equal(ComfortSide.Inside, scale.Side(23.9));
        Assert.Equal(ComfortSide.Inside, scale.Side(24));
        Assert.Equal(ComfortSide.High, scale.Side(24.1));
        Assert.Equal(1.5, scale.Outside(18.5));
        Assert.Equal(0, scale.Outside(22));
        Assert.Equal(2, scale.Outside(26));
        Assert.Equal(0, scale.Outside(24));
    }

    [Fact]
    public void Share_splits_the_values_and_leaves_out_missing_ones()
    {
        var share = ComfortScale.Temperature.Share([18, 21, 22, 25, double.NaN]);

        Assert.NotNull(share);
        Assert.Equal(0.25, share.Low);
        Assert.Equal(0.5, share.Inside);
        Assert.Equal(0.25, share.High);
    }

    [Fact]
    public void Share_is_null_without_values()
        => Assert.Null(ComfortScale.Humidity.Share([]));
}
