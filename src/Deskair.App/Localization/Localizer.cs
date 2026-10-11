using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Text;

namespace Deskair.App.Localization;

/// <summary>
/// Looks strings up in Strings.resx and Strings.uk.resx. The indexer raises PropertyChanged when the
/// culture changes, so bindings made by the {loc:T} markup extension refresh without a restart.
/// </summary>
public sealed class Localizer : ILocalizer, INotifyPropertyChanged
{
    private readonly ResourceManager resources;
    private CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

    public Localizer()
        : this(new ResourceManager("Deskair.App.Localization.Strings", typeof(Localizer).Assembly))
    {
    }

    public Localizer(ResourceManager resources)
    {
        this.resources = resources;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture => culture;

    public string this[string key] => Get(key);

    public void SetCulture(CultureInfo newCulture)
    {
        ArgumentNullException.ThrowIfNull(newCulture);

        if (newCulture.Equals(culture))
        {
            return;
        }

        culture = newCulture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
    }

    public string Get(string key)
    {
        return resources.GetString(key, culture) ?? $"[{key}]";
    }

    public string Format(string key, params (string Name, object? Value)[] values)
    {
        var text = new StringBuilder(Get(key));

        foreach (var (name, value) in values)
        {
            text.Replace("{" + name + "}", FormatValue(value));
        }

        return text.ToString();
    }

    public string Plural(string baseKey, long count, params (string Name, object? Value)[] values)
    {
        var category = PluralRules.Select(culture, count);
        var all = values.Append(("count", count)).ToArray();

        return Format($"{baseKey}_{category}", all);
    }

    private string FormatValue(object? value)
    {
        // Whole numbers get thousands separators (1,068 and 1 068); decimals keep at most two places.
        return value switch
        {
            null => string.Empty,
            int or long or short or byte => ((IFormattable)value).ToString("N0", culture),
            double or float or decimal => ((IFormattable)value).ToString("#,##0.##", culture),
            IFormattable formattable => formattable.ToString(null, culture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
