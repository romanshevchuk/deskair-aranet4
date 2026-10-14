using System.Windows;
using System.Windows.Controls;
using Deskair.App.Localization;
using Deskair.App.Ui.Markup;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A pill that says how much of the period the readings cover: "Based on 82 % of expected readings". It takes the fraction (0 to 1) and writes its own text,
/// in the selected language, and follows a language change. A null fraction shows nothing. The view model decides whether to show the chip at all.
/// </summary>
public class CoverageChip : Control
{
    public static readonly DependencyProperty FractionProperty = DependencyProperty.Register(
        nameof(Fraction),
        typeof(double?),
        typeof(CoverageChip),
        new PropertyMetadata(null, OnFractionChanged));

    private static readonly DependencyPropertyKey textPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Text),
        typeof(string),
        typeof(CoverageChip),
        new PropertyMetadata(null));

    public static readonly DependencyProperty TextProperty = textPropertyKey.DependencyProperty;

    private Localizer? followed;

    static CoverageChip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CoverageChip), new FrameworkPropertyMetadata(typeof(CoverageChip)));
    }

    public CoverageChip()
    {
        Loaded += (_, _) => Follow();
        Unloaded += (_, _) => Unfollow();
    }

    public double? Fraction
    {
        get => (double?)GetValue(FractionProperty);
        set => SetValue(FractionProperty, value);
    }

    /// <summary>The words on the chip, or null when there is no fraction.</summary>
    public string? Text => (string?)GetValue(TextProperty);

    private static void OnFractionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        => ((CoverageChip)sender).UpdateText();

    private static Localizer? FindLocalizer()
        => Application.Current?.TryFindResource(TExtension.LocalizerResourceKey) as Localizer;

    private void Follow()
    {
        Unfollow();
        followed = FindLocalizer();

        if (followed is not null)
        {
            followed.PropertyChanged += OnLocalizerChanged;
        }

        UpdateText();
    }

    private void Unfollow()
    {
        if (followed is not null)
        {
            followed.PropertyChanged -= OnLocalizerChanged;
            followed = null;
        }
    }

    private void OnLocalizerChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Localizer.Culture))
        {
            Dispatcher.Invoke(UpdateText);
        }
    }

    private void UpdateText()
    {
        var localizer = followed ?? FindLocalizer();
        var text = Fraction is { } fraction && localizer is not null
            ? localizer.Format("Coverage_Chip", ("percent", NumberFormat.Percent(fraction, localizer.Culture)))
            : null;

        SetValue(textPropertyKey, text);
    }
}
