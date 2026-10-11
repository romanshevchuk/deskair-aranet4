using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Deskair.App.Localization;

namespace Deskair.Tests.Localization;

public class LocalizerTests
{
    private static readonly CultureInfo english = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo ukrainian = CultureInfo.GetCultureInfo("uk-UA");

    private static Localizer Create(CultureInfo culture)
    {
        var localizer = new Localizer();
        localizer.SetCulture(culture);

        return localizer;
    }

    private static HashSet<string> Keys(CultureInfo culture)
    {
        var manager = new ResourceManager("Deskair.App.Localization.Strings", typeof(Localizer).Assembly);
        using var set = manager.GetResourceSet(NeutralFor(culture), createIfNotExists: true, tryParents: false);

        Assert.NotNull(set);

        return set!.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key).ToHashSet();
    }

    private static CultureInfo NeutralFor(CultureInfo culture)
    {
        return culture.TwoLetterISOLanguageName == "uk" ? CultureInfo.GetCultureInfo("uk") : CultureInfo.InvariantCulture;
    }

    private static string BaseKey(string key) => Regex.Replace(key, "_(One|Few|Many|Other)$", string.Empty);

    [Fact]
    public void Get_returns_the_text_for_the_selected_language()
    {
        Assert.Equal("Live", Create(english).Get("Nav_Live"));
        Assert.Equal("Наживо", Create(ukrainian).Get("Nav_Live"));
    }

    [Fact]
    public void Get_marks_a_missing_key()
    {
        Assert.Equal("[No_Such_Key]", Create(english).Get("No_Such_Key"));
    }

    [Fact]
    public void Every_English_key_has_a_Ukrainian_key_and_the_other_way_round()
    {
        var englishBases = Keys(english).Select(BaseKey).ToHashSet();
        var ukrainianBases = Keys(ukrainian).Select(BaseKey).ToHashSet();

        Assert.Empty(englishBases.Except(ukrainianBases));
        Assert.Empty(ukrainianBases.Except(englishBases));
    }

    [Fact]
    public void Plural_keys_have_the_forms_each_language_needs()
    {
        var englishKeys = Keys(english);
        var ukrainianKeys = Keys(ukrainian);

        var pluralBases = englishKeys.Where(key => key.EndsWith("_One", StringComparison.Ordinal)).Select(BaseKey);

        foreach (var key in pluralBases)
        {
            Assert.Contains($"{key}_Other", englishKeys);
            Assert.Contains($"{key}_One", ukrainianKeys);
            Assert.Contains($"{key}_Few", ukrainianKeys);
            Assert.Contains($"{key}_Many", ukrainianKeys);
        }
    }

    [Fact]
    public void Placeholders_match_between_English_and_Ukrainian()
    {
        var manager = new ResourceManager("Deskair.App.Localization.Strings", typeof(Localizer).Assembly);

        // The English "Other" form has no Ukrainian twin (Ukrainian uses Few and Many instead).
        var sharedKeys = Keys(english).Where(key => !key.EndsWith("_Other", StringComparison.Ordinal));

        foreach (var key in sharedKeys)
        {
            var englishPlaceholders = Placeholders(manager.GetString(key, english)!);
            var ukrainianPlaceholders = Placeholders(manager.GetString(key, ukrainian)!);

            Assert.True(englishPlaceholders.SetEquals(ukrainianPlaceholders), $"Placeholders differ for {key}");
        }
    }

    private static HashSet<string> Placeholders(string text)
    {
        return Regex.Matches(text, @"\{(\w+)\}").Select(match => match.Groups[1].Value).ToHashSet();
    }

    [Fact]
    public void Format_fills_named_placeholders()
    {
        var text = Create(english).Format("Banner_Stale", ("min", 7));

        Assert.Equal("Last reading 7 min ago", text);
    }

    [Fact]
    public void Format_uses_the_selected_culture_for_numbers()
    {
        var englishText = Create(english).Format("Trend_Detail", ("ppm", 1068), ("min", 30));
        var ukrainianText = Create(ukrainian).Format("Trend_Detail", ("ppm", 1068), ("min", 30));

        Assert.Equal("1,068 ppm over 30 min", englishText);
        Assert.Equal("1" + ukrainian.NumberFormat.NumberGroupSeparator + "068 ppm за 30 хв", ukrainianText);
    }

    [Theory]
    [InlineData(1, "1 day with no data")]
    [InlineData(2, "2 days with no data")]
    public void Plural_picks_the_English_form(int count, string expected)
    {
        Assert.Equal(expected, Create(english).Plural("Gap_DaysNoData", count));
    }

    [Theory]
    [InlineData(1, "1 день без даних")]
    [InlineData(3, "3 дні без даних")]
    [InlineData(5, "5 днів без даних")]
    [InlineData(11, "11 днів без даних")]
    [InlineData(21, "21 день без даних")]
    public void Plural_picks_the_Ukrainian_form(int count, string expected)
    {
        Assert.Equal(expected, Create(ukrainian).Plural("Gap_DaysNoData", count));
    }

    [Fact]
    public void Changing_the_culture_raises_PropertyChanged_for_the_indexer()
    {
        var localizer = Create(english);
        var raised = new List<string?>();
        localizer.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        localizer.SetCulture(ukrainian);

        Assert.Contains("Item[]", raised);
    }

    [Fact]
    public void Setting_the_same_culture_raises_nothing()
    {
        var localizer = Create(english);
        var raised = 0;
        localizer.PropertyChanged += (_, _) => raised++;

        localizer.SetCulture(english);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Untranslated_names_stay_the_same_in_both_languages()
    {
        Assert.Equal(Create(english).Get("Metric_Co2"), Create(ukrainian).Get("Metric_Co2"));
        Assert.Equal("Aranet4", Create(ukrainian).Get("Sensor_Aranet4_Name"));
    }
}
