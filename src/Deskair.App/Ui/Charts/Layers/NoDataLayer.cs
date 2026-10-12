using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>A message centred in the plot ("No data"), so an empty chart says why it is empty instead of showing a bare frame.</summary>
public sealed class NoDataLayer : IChartLayer
{
    public string? Text { get; set; }

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        var text = area.Text(Text, area.Brush("Text.Tertiary.Brush"));

        context.DrawText(text, new Point(area.Plot.Left + ((area.Plot.Width - text.Width) / 2), area.Plot.Top + ((area.Plot.Height - text.Height) / 2)));
    }
}
