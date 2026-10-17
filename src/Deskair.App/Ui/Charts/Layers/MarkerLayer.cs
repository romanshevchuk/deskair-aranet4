using System.Windows;
using System.Windows.Media;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>Small rings on a line chart (the "air improved" markers): a ring with the surface colour inside and the primary ink around it, with a quiet label centred just above.</summary>
public sealed class MarkerLayer : IChartLayer
{
    public const double Radius = 4;
    private const double LabelGap = 6;

    public IReadOnlyList<ChartMarker>? Markers { get; set; }

    /// <summary>The word above each ring, already localized. None when null.</summary>
    public string? Label { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Markers is not { Count: > 0 } markers)
        {
            return;
        }

        var ring = new Pen(area.Brush("Text.Primary.Brush"), 1.5);
        var surface = area.Brush("Surface.Brush");
        var quiet = area.Brush("Text.Secondary.Brush");

        foreach (var marker in markers.Where(item => item.Time >= new DateTimeOffset((long)area.XMin, TimeSpan.Zero) && item.Time <= new DateTimeOffset((long)area.XMax, TimeSpan.Zero)))
        {
            var centre = new Point(area.X(marker.Time), area.Y(marker.Value));

            context.DrawEllipse(surface, ring, centre, Radius, Radius);

            if (!string.IsNullOrEmpty(Label))
            {
                var text = area.Text(Label, quiet);
                var left = Math.Clamp(centre.X - (text.Width / 2), area.Plot.Left, Math.Max(area.Plot.Left, area.Plot.Right - text.Width));

                context.DrawText(text, new Point(left, Math.Max(area.Plot.Top, centre.Y - Radius - LabelGap - text.Height)));
            }
        }
    }
}
