using System.Windows;
using System.Windows.Controls;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Controls;

// TODO(CTRL-04) [M, haiku]
// Spec: docs/design/components.md "Metric mini card"; docs/design/design-tokens.md "Metric colours", "Typography"
// Input : Metric, title, value text, subtitle, selected flag; content slot for a Sparkline.
// Output: Name, value (32 px, weight 500), subtitle and sparkline; when selected, an outline in the Metric.*.Brush colour.
// Rules : Template of a Button so it is keyboard and screen reader operable.
// Edge  : A missing value shows empty text, never 0.
// Done  : The gallery shows it in all states, light and dark, English and Ukrainian; build and tests pass.
public class MetricCard : Button
{
    public static readonly DependencyProperty MetricProperty = DependencyProperty.Register(
        nameof(Metric),
        typeof(Metric?),
        typeof(MetricCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(MetricCard),
        new PropertyMetadata(default(bool)));

    static MetricCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MetricCard), new FrameworkPropertyMetadata(typeof(MetricCard)));
    }

    public Metric? Metric
    {
        get => (Metric?)GetValue(MetricProperty);
        set => SetValue(MetricProperty, value);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}
