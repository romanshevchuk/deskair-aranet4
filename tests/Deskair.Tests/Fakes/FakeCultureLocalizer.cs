using System.Globalization;
using Deskair.App.Localization;

namespace Deskair.Tests.Fakes;

/// <summary>Like <see cref="FakeLocalizer"/> but with a culture the test chooses, for tests of number and date formats.</summary>
public sealed class FakeCultureLocalizer : ILocalizer
{
    public FakeCultureLocalizer(CultureInfo culture)
    {
        Culture = culture;
    }

    public CultureInfo Culture { get; }

    public string Get(string key) => key;

    public string Format(string key, params (string Name, object? Value)[] values) => key;

    public string Plural(string baseKey, long count, params (string Name, object? Value)[] values) => baseKey;
}
