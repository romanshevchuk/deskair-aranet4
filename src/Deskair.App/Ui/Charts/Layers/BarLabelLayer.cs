using System.Globalization;
using System.Windows.Media;
using Deskair.App.Localization;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.App.Ui.Charts.Layers;

/// <summary>
/// The labels under bars that each stand for one day (prototype): the weekday ("Fri"), "Today" in bold for the last bar, and below it a row with the average of every bar
/// ("avg  1,001  1,025 ..."; "–" for a day with no data). The row is drawn only when <see cref="RowLabel"/> is set. Weekdays are in <see cref="TimeZone"/> and follow <see cref="Culture"/>.
/// </summary>
public sealed class BarLabelLayer : IChartLayer
{
    private const double LabelGap = 8;
    private const double RowGap = 6;
    private const string NoValue = "–";

    public IReadOnlyList<ChartBar>? Bars { get; set; }

    public CultureInfo? Culture { get; set; }

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The label of the last bar, which ends now ("Today"). Null labels it with its weekday like the others.</summary>
    public string? TodayLabel { get; set; }

    /// <summary>The word at the start of the average row ("avg"). Null draws no row.</summary>
    public string? RowLabel { get; set; }

    public string Format { get; set; } = NumberFormat.WholeFormat;

    public void Draw(DrawingContext context, ChartArea area)
    {
        if (Bars is not { Count: > 0 } bars)
        {
            return;
        }

        var culture = Culture ?? CultureInfo.CurrentUICulture;
        var quiet = area.Brush("Text.Tertiary.Brush");
        var ink = area.Brush("Text.Primary.Brush");
        var labelTop = area.Plot.Bottom + LabelGap;
        var rowTop = labelTop + area.Text("0", quiet).Height + RowGap;

        for (var index = 0; index < bars.Count; index++)
        {
            var bar = bars[index];
            var middle = BarGeometry.Middle(area, bar);
            var today = index == bars.Count - 1 && TodayLabel is not null;
            var name = today ? TodayLabel! : TimeZoneInfo.ConvertTime(bar.Start, TimeZone).ToString("ddd", culture);
            var label = area.Text(name, today ? ink : quiet, bold: today);

            context.DrawText(label, new System.Windows.Point(middle - (label.Width / 2), labelTop));

            if (RowLabel is not null)
            {
                var value = bar.State != BucketState.NoData && bar.Average is { } average ? average.ToString(Format, culture) : NoValue;
                var text = area.Text(value, ink, bold: true);

                context.DrawText(text, new System.Windows.Point(middle - (text.Width / 2), rowTop));
            }
        }

        if (RowLabel is not null)
        {
            var caption = area.Text(RowLabel, quiet);

            context.DrawText(caption, new System.Windows.Point(area.Plot.Left - LabelGap - caption.Width, rowTop));
        }
    }
}
