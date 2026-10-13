using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Deskair.App.Ui.Markup;

/// <summary>
/// A little markup for localized sentences that start with a bold lead ("**Worst hours: 15:00–18:00** (avg 1,249 ppm). Opening a window ..."). <c>**</c> switches bold on and off;
/// anything else is plain text. Set <see cref="TextProperty"/> on a TextBlock instead of its Text. An odd number of markers leaves the rest in bold, and an empty or null text clears the block.
/// </summary>
public static class InlineMarkup
{
    private const string Marker = "**";

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(InlineMarkup),
        new PropertyMetadata(null, OnTextChanged));

    public static string? GetText(DependencyObject target) => (string?)target.GetValue(TextProperty);

    public static void SetText(DependencyObject target, string? value) => target.SetValue(TextProperty, value);

    /// <summary>The pieces of a text, each with whether it is bold. Empty pieces are left out.</summary>
    public static IReadOnlyList<(string Text, bool Bold)> Parse(string? text)
    {
        var pieces = new List<(string Text, bool Bold)>();

        if (string.IsNullOrEmpty(text))
        {
            return pieces;
        }

        var bold = false;

        foreach (var part in text.Split(Marker))
        {
            if (part.Length > 0)
            {
                pieces.Add((part, bold));
            }

            bold = !bold;
        }

        return pieces;
    }

    private static void OnTextChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not TextBlock block)
        {
            return;
        }

        block.Inlines.Clear();

        foreach (var (text, bold) in Parse((string?)e.NewValue))
        {
            block.Inlines.Add(bold ? new Bold(new Run(text)) : new Run(text));
        }
    }
}
