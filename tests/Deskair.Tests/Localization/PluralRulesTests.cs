using System.Globalization;
using Deskair.App.Localization;

namespace Deskair.Tests.Localization;

public class PluralRulesTests
{
    private static readonly CultureInfo english = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo ukrainian = CultureInfo.GetCultureInfo("uk-UA");

    [Theory]
    [InlineData(0, PluralCategory.Other)]
    [InlineData(1, PluralCategory.One)]
    [InlineData(2, PluralCategory.Other)]
    [InlineData(21, PluralCategory.Other)]
    public void English_has_one_and_other(long count, PluralCategory expected)
    {
        Assert.Equal(expected, PluralRules.Select(english, count));
    }

    [Theory]
    [InlineData(1, PluralCategory.One)]
    [InlineData(21, PluralCategory.One)]
    [InlineData(101, PluralCategory.One)]
    [InlineData(2, PluralCategory.Few)]
    [InlineData(3, PluralCategory.Few)]
    [InlineData(4, PluralCategory.Few)]
    [InlineData(23, PluralCategory.Few)]
    [InlineData(104, PluralCategory.Few)]
    [InlineData(0, PluralCategory.Many)]
    [InlineData(5, PluralCategory.Many)]
    [InlineData(11, PluralCategory.Many)]
    [InlineData(12, PluralCategory.Many)]
    [InlineData(14, PluralCategory.Many)]
    [InlineData(20, PluralCategory.Many)]
    [InlineData(111, PluralCategory.Many)]
    [InlineData(112, PluralCategory.Many)]
    public void Ukrainian_has_one_few_and_many(long count, PluralCategory expected)
    {
        Assert.Equal(expected, PluralRules.Select(ukrainian, count));
    }

    [Fact]
    public void Negative_numbers_follow_their_absolute_value()
    {
        Assert.Equal(PluralCategory.Few, PluralRules.Select(ukrainian, -3));
    }

    [Fact]
    public void Missing_culture_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => PluralRules.Select(null!, 1));
    }
}
