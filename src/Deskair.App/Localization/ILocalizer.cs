using System.Globalization;

namespace Deskair.App.Localization;

public interface ILocalizer
{
    CultureInfo Culture { get; }

    /// <summary>Returns the text for a key, or "[key]" when the key is missing.</summary>
    string Get(string key);

    /// <summary>Replaces named placeholders such as {count} using the selected culture for numbers and dates.</summary>
    string Format(string key, params (string Name, object? Value)[] values);

    /// <summary>Picks the plural form (Key_One, Key_Few, Key_Many or Key_Other) and fills {count}.</summary>
    string Plural(string baseKey, long count, params (string Name, object? Value)[] values);
}
