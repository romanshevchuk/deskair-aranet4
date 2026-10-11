using System.Globalization;

namespace Deskair.App.Localization;

/// <summary>Plural categories for whole numbers (docs/design/localization.md, technical rule 3).</summary>
public static class PluralRules
{
    public static PluralCategory Select(CultureInfo culture, long count)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (culture.TwoLetterISOLanguageName == "uk")
        {
            return SelectUkrainian(count);
        }

        return SelectEnglish(count);
    }

    private static PluralCategory SelectEnglish(long count)
    {
        return count == 1 ? PluralCategory.One : PluralCategory.Other;
    }

    private static PluralCategory SelectUkrainian(long count)
    {
        var lastDigit = Math.Abs(count % 10);
        var lastTwoDigits = Math.Abs(count % 100);

        if (lastDigit == 1 && lastTwoDigits != 11)
        {
            return PluralCategory.One;
        }

        if (lastDigit is >= 2 and <= 4 && lastTwoDigits is < 12 or > 14)
        {
            return PluralCategory.Few;
        }

        return PluralCategory.Many;
    }
}
