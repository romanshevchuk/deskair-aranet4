using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Deskair.App.Localization;

namespace Deskair.App.Ui.Markup;

/// <summary>
/// <c>{loc:T Key}</c>: the text for a resource key, refreshed when the language changes.
/// The localizer is found in the application resources under <see cref="LocalizerResourceKey"/>.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension : MarkupExtension
{
    public const string LocalizerResourceKey = "Deskair.Localizer";

    public TExtension()
    {
    }

    public TExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Application.Current?.TryFindResource(LocalizerResourceKey) is not Localizer localizer)
        {
            return $"[{Key}]";
        }

        var binding = new Binding($"[{Key}]")
        {
            Source = localizer,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
