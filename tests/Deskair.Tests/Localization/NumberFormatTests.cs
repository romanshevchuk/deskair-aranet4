using System.Globalization;
using Deskair.App.Localization;
using Deskair.Domain.Metrics;

namespace Deskair.Tests.Localization;

public class NumberFormatTests
{
    private static readonly CultureInfo englishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo ukrainianCulture = CultureInfo.GetCultureInfo("uk-UA");

    [Theory]
    [InlineData(1151.4, "1151")]
    [InlineData(412, "412")]
    [InlineData(12345.6, "12346")]
    public void Whole_numbers_have_no_group_separator(double value, string expected)
    {
        Assert.Equal(expected, NumberFormat.Whole(value, englishCulture));
        Assert.Equal(expected, NumberFormat.Whole(value, ukrainianCulture));
    }

    [Fact]
    public void One_decimal_has_no_group_separator_and_follows_the_decimal_mark()
    {
        Assert.Equal("1013.2", NumberFormat.OneDecimal(1013.24, englishCulture));
        Assert.Equal("1013,2", NumberFormat.OneDecimal(1013.24, ukrainianCulture));
    }

    [Theory]
    [InlineData(Metric.Co2, 1151.4, "1151", "1151")]
    [InlineData(Metric.Temperature, 21.14, "21.1", "21,1")]
    [InlineData(Metric.Humidity, 59.4, "59", "59")]
    [InlineData(Metric.Pressure, 1013.24, "1013.2", "1013,2")]
    public void Metric_values_use_the_metric_precision(Metric metric, double value, string english, string ukrainian)
    {
        Assert.Equal(english, NumberFormat.Value(metric, value, englishCulture));
        Assert.Equal(ukrainian, NumberFormat.Value(metric, value, ukrainianCulture));
    }

    [Theory]
    [InlineData(0.59, "59%", "59 %")]
    [InlineData(0, "0%", "0 %")]
    [InlineData(1, "100%", "100 %")]
    [InlineData(0.585, "59%", "59 %")]
    public void Percent_is_attached_in_english_and_spaced_in_ukrainian(double fraction, string english, string ukrainian)
    {
        Assert.Equal(english, NumberFormat.Percent(fraction, englishCulture));
        Assert.Equal(ukrainian, NumberFormat.Percent(fraction, ukrainianCulture));
    }

    [Theory]
    [InlineData(-0.2, "0%")]
    [InlineData(1.4, "100%")]
    public void Percent_is_clamped_to_the_range(double fraction, string expected)
        => Assert.Equal(expected, NumberFormat.Percent(fraction, englishCulture));
}
