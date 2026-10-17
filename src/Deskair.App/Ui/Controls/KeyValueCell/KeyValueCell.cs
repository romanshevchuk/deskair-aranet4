using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>A caption and a value. By default the caption is above the value; with <see cref="ValueFirst"/> the value is above the caption (the three hero stats).</summary>
public class KeyValueCell : Control
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(KeyValueCell),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(KeyValueCell),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ValueFirstProperty = DependencyProperty.Register(
        nameof(ValueFirst),
        typeof(bool),
        typeof(KeyValueCell),
        new PropertyMetadata(false));

    static KeyValueCell()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyValueCell), new FrameworkPropertyMetadata(typeof(KeyValueCell)));
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool ValueFirst
    {
        get => (bool)GetValue(ValueFirstProperty);
        set => SetValue(ValueFirstProperty, value);
    }

    /// <summary>The width the value needs on one line, without the caption. A panel that shares width between cells uses it so a value never wraps while a caption may.</summary>
    public double MeasureValueWidth()
    {
        ApplyTemplate();

        if (GetTemplateChild("Value") is not TextBlock value)
        {
            return 0;
        }

        value.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        return value.DesiredSize.Width;
    }
}