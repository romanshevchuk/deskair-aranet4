using System.Globalization;
using Deskair.App.Localization;

namespace Deskair.Tests.Fakes;

/// <summary>Returns the key, followed by the placeholder values, so tests can see which text a view model asked for.</summary>
public sealed class FakeLocalizer : ILocalizer
{
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-US");

    public string Get(string key) => key;

    public string Format(string key, params (string Name, object? Value)[] values)
        => values.Length == 0 ? key : $"{key}({string.Join(", ", values.Select(value => $"{value.Name}={value.Value}"))})";

    public string Plural(string baseKey, long count, params (string Name, object? Value)[] values)
        => $"{baseKey}[{count}]";
}
