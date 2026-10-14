using System.Globalization;
using System.Windows;
using Deskair.App.Localization;
using Deskair.App.Ui.Charts.Layers;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Charts;

/// <summary>
/// The typical day: 24 bars, one per hour of the day, coloured by the zone of the average CO₂ at that hour, with an outline around the worst three hours.
/// Hovering an hour reads out its average. Hours without data draw no bar.
/// </summary>
public sealed class TypicalDayChart : ChartBase
{
    private const double Minimum = 400;
    private const double MinimumTop = 1600;
    private const double OutlineHeadroom = 24;

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

    public static readonly DependencyProperty WorstTextProperty = DependencyProperty.Register(
        nameof(WorstText),
        typeof(string),
        typeof(TypicalDayChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The text above the worst three hours ("worst 15–18h"). Already localized.</summary>
    public string? WorstText
    {
        get => (string?)GetValue(WorstTextProperty);
        set => SetValue(WorstTextProperty, value);
    }

    /// <summary>Room above the plot for the dashed outline of the worst hours and its text.</summary>
    protected override Thickness PlotMargin
    {
        get
        {
            var margin = base.PlotMargin;

            return new Thickness(margin.Left, Math.Max(margin.Top, OutlineHeadroom), margin.Right, margin.Bottom);
        }
    }

    private bool HasData => Hours?.Any(hour => hour.Average is not null) == true;

    protected override ChartRanges GetDataRanges() => new(0, 24, Minimum, Top());

    protected override IReadOnlyList<IChartLayer> CreateLayers()
    {
        double[] ticks = [ZoneThresholds.Default.ElevatedFromPpm, ZoneThresholds.Default.HighAbovePpm];
        var layers = new List<IChartLayer>
        {
            new GridLayer { YTicks = ticks, Dashed = true },
            new HourBarLayer { Hours = Hours, WorstText = WorstText }
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

        return new ChartHover(time, average, area.X(hourOfDay + 0.5), area.Y(average), $"{label} · {average.ToString(NumberFormat.WholeFormat, CultureInfo.CurrentUICulture)} {Unit}");
    }

    private double Top()
    {
        var highest = Hours?.Where(hour => hour.Average is not null).Select(hour => hour.Average!.Value).DefaultIfEmpty(0).Max() ?? 0;

        return Math.Max(MinimumTop, Math.Ceiling(highest / 200) * 200);
    }
}
