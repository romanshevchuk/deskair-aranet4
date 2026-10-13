using System.Globalization;
using Deskair.App.Localization;

namespace Deskair.Tests.Fakes;

/// <summary>A localizer whose texts change when a test changes <see cref="Prefix"/>, like the real one does when the language changes.</summary>
public sealed class SwitchableLocalizer : ILocalizer
{
    public string Prefix { get; set; } = string.Empty;

    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-US");

    public string Get(string key) => Prefix + key;

    public string Format(string key, params (string Name, object? Value)[] values) => Prefix + key;

    public string Plural(string baseKey, long count, params (string Name, object? Value)[] values) => Prefix + baseKey;
}
