using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// Shared base of all charts: the plot area, ordered layers, hover and hit testing.
/// A subclass gives its data ranges, its layers and (for hover) a hit test; drawing lives in the layers.
/// </summary>
public abstract class ChartBase : FrameworkElement
{
    private static readonly Thickness defaultMargin = new(44, 8, 14, 24);

    private static readonly DependencyPropertyKey hoverKey = DependencyProperty.RegisterReadOnly(
        nameof(Hover),
        typeof(ChartHover),
        typeof(ChartBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnHoverChanged));

    public static readonly DependencyProperty HoverProperty = hoverKey.DependencyProperty;

    private ChartArea? lastArea;

    public event EventHandler? HoverChanged;

    /// <summary>What the mouse is over, or null when it is outside the chart.</summary>
    public ChartHover? Hover => (ChartHover?)GetValue(HoverProperty);

    /// <summary>Space around the plot for axis labels. Charts without axes return zero.</summary>
    protected virtual Thickness PlotMargin
        => TryFindResource("Space.Chart.Margin") is Thickness margin ? margin : defaultMargin;

    /// <summary>The layers to draw, back to front. Called on every render, so it sees the latest data and hover.</summary>
    protected abstract IReadOnlyList<IChartLayer> CreateLayers();

    /// <summary>The data ranges to show. Called on every render.</summary>
    protected virtual ChartRanges GetDataRanges() => new(0, 1, 0, 1);

    /// <summary>What the mouse is over, using the area of the last render. Return null for nothing.</summary>
    protected virtual ChartHover? HitTest(Point position, ChartArea area) => null;

    /// <summary>The plot area for a control size and data ranges, with the margins, brush lookup, font and DPI of this chart.</summary>
    protected ChartArea CreateArea(Size size, double xMin, double xMax, double yMin, double yMax)
    {
        var typeface = TryFindResource("Font.Family") is FontFamily family ? new Typeface(family.Source) : new Typeface("Segoe UI");
        var fontSize = TryFindResource("Font.Size.Caption") is double caption ? caption : 12;

        return ChartArea.Create(size, PlotMargin, xMin, xMax, yMin, yMax) with
        {
            Resources = key => TryFindResource(key),
            Typeface = typeface,
            FontSize = fontSize,
            PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip
        };
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var ranges = GetDataRanges();
        var area = CreateArea(RenderSize, ranges.XMin, ranges.XMax, ranges.YMin, ranges.YMax);
        lastArea = area;

        // A transparent fill makes the whole control hit-testable for the mouse.
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

        if (area.IsEmpty)
        {
            return;
        }

        foreach (var layer in CreateLayers())
        {
            layer.Draw(drawingContext, area);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (lastArea is not { IsEmpty: false } area)
        {
            return;
        }

        SetValue(hoverKey, HitTest(e.GetPosition(this), area));
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        SetValue(hoverKey, null);
    }

    private static void OnHoverChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ChartBase)sender).HoverChanged?.Invoke(sender, EventArgs.Empty);
    }
}
