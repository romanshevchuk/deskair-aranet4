using System.Windows;
using Deskair.Domain.Metrics;

namespace Deskair.App.Ui.Controls;

/// <summary>A Live mini card: name, value, subtitle and a sparkline. Choosing it shows its metric on the Live chart; the chosen card has an accent ring.</summary>
public class MetricCard : SelectableCard
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

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(MetricCard),
        new PropertyMetadata(null));

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

    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
}
