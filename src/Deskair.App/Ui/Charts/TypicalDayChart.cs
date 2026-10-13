using System.Globalization;
using System.Windows;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The typical day: 24 bars, one per hour of the day, coloured by the zone of the average CO₂ at that hour, with an outline around the worst three hours.
/// Hovering an hour reads out its average. Hours without data draw no bar.
/// </summary>
public sealed class TypicalDayChart : ChartBase
{
    private const double Minimum = 400;
    private const double MinimumTop = 1600;

    public static readonly DependencyProperty HoursProperty = DependencyProperty.Register(
        nameof(Hours),
        typeof(IReadOnlyList<HourBar>),
        typeof(TypicalDayChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit),
        typeof(string),
        typeof(TypicalDayChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NoDataTextProperty = DependencyProperty.Register(
        nameof(NoDataText),
        typeof(string),
        typeof(TypicalDayChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<HourBar>? Hours
    {
        get => (IReadOnlyList<HourBar>?)GetValue(HoursProperty);
        set => SetValue(HoursProperty, value);
    }

    /// <summary>The unit text in the hover readout ("ppm").</summary>
    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>Shown in the middle of the plot with no data, and in the hover readout over an hour without one. Already localized.</summary>
    public string? NoDataText
    {
        get => (string?)GetValue(NoDataTextProperty);
        set => SetValue(NoDataTextProperty, value);
    }

    private bool HasData => Hours?.Any(hour => hour.Average is not null) == true;

    protected override ChartRanges GetDataRanges() => new(0, 24, Minimum, Top());

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        var ticks = ChartTicks.Nice(Minimum, Top(), 5);
        var layers = new List<IChartLayer>
        {
            new GridLayer { YTicks = ticks },
            new HourBarLayer { Hours = Hours }
        };

        if (!HasData)
        {
            layers.Add(new NoDataLayer { Text = NoDataText });
        }

        layers.Add(new HourAxisLayer { Culture = CultureInfo.CurrentUICulture, YTicks = ticks });

        return layers;
    }

    protected override ChartHover? HitTest(Point position, ChartArea area)
    {
        if (Hours is null || !area.ContainsX(position.X))
        {
            return null;
        }

        var hourOfDay = Math.Clamp((int)Math.Floor(area.DataX(position.X)), 0, 23);
        var hour = Hours.FirstOrDefault(candidate => candidate.Hour == hourOfDay);
        var time = DateTimeOffset.UnixEpoch.AddHours(hourOfDay);
        var label = $"{hourOfDay:00}:00";

        if (hour?.Average is not { } average)
        {
            return new ChartHover(time, null, area.X(hourOfDay + 0.5), null, $"{label} · {NoDataText}");
        }

        return new ChartHover(time, average, area.X(hourOfDay + 0.5), area.Y(average), $"{label} · {average.ToString("N0", CultureInfo.CurrentUICulture)} {Unit}");
    }

    private double Top()
    {
        var highest = Hours?.Where(hour => hour.Average is not null).Select(hour => hour.Average!.Value).DefaultIfEmpty(0).Max() ?? 0;

        return Math.Max(MinimumTop, Math.Ceiling(highest / 200) * 200);
    }
}
