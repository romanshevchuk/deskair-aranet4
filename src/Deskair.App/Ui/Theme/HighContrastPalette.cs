using System.Collections;
using System.Windows;
using System.Windows.Media;

namespace Deskair.App.Ui.Theme;

/// <summary>The Windows High Contrast colours the palette is built from.</summary>
public readonly record struct HighContrastColors(Color Window, Color WindowText, Color Highlight, Color HighlightText, Color GrayText, Color HotTrack)
{
    public static HighContrastColors FromSystem() => new(
        SystemColors.WindowColor,
        SystemColors.WindowTextColor,
        SystemColors.HighlightColor,
        SystemColors.HighlightTextColor,
        SystemColors.GrayTextColor,
        SystemColors.HotTrackColor);
}

/// <summary>
/// The tokens while Windows High Contrast is on (accessibility.md "High Contrast"): surfaces are the window colour, text and outlines the window text colour, the accent the
/// highlight pair. Colours that only mean something by hue (zones, metrics, the heat steps) become the text colour at stepped opacities; every value is also written as text.
/// </summary>
public static class HighContrastPalette
{
    private static readonly string[] heatCo2 = ["Heat.G1", "Heat.G2", "Heat.E1", "Heat.E2", "Heat.H1", "Heat.H2"];

    /// <summary>The opacity of each CO₂ heat step, lightest to darkest.</summary>
    private static readonly double[] heatCo2Opacity = [0.2, 0.35, 0.5, 0.65, 0.8, 1];

    /// <summary>The overriding colour for a token (its key without ".Color"), or null to leave it as it is.</summary>
    public static Color? Map(string name, HighContrastColors colors)
    {
        var text = colors.WindowText;

        switch (name)
        {
            case "Window.Bg" or "Surface" or "Segment.Bg" or "Segment.Selected" or "Insight.Bg" or "Flyout.Band" or "Toggle.Knob" or "Caption.Hover":
                return colors.Window;
            case "Toggle.Off":
                return colors.GrayText;
            case "Accent":
                return colors.Highlight;
            case "Accent.On":
                return colors.HighlightText;
            case "Focus.Ring":
                return colors.HotTrack;
            case "Border" or "Border.Window" or "Card.Border" or "Card.Border.Hover":
                return text;
            case "Heat.None.Line":
                return colors.GrayText;
            case "Divider" or "Grid":
                return With(text, 0.5);
            case "Trend.Falling" or "Trend.Rising":
                return text;
            case "Glow.Offline":
                return With(text, 0);
        }

        if (name.StartsWith("Text.", StringComparison.Ordinal))
        {
            return text;
        }

        if (name.StartsWith("Zone.", StringComparison.Ordinal))
        {
            return Zone(name, colors);
        }

        if (name.StartsWith("Chart.Band.", StringComparison.Ordinal))
        {
            return With(text, 0);
        }

        if (name.StartsWith("Metric.", StringComparison.Ordinal))
        {
            return name.EndsWith(".Fill", StringComparison.Ordinal) ? With(text, 0.07) : name.EndsWith(".Ideal", StringComparison.Ordinal) ? With(text, 0.17) : text;
        }

        if (name.StartsWith("Heat.", StringComparison.Ordinal))
        {
            return With(text, HeatOpacity(name));
        }

        return null;
    }

    /// <summary>An overlay dictionary with the High Contrast colour and brush for every colour token the sources define.</summary>
    public static ResourceDictionary Create(IEnumerable<ResourceDictionary> sources, HighContrastColors colors)
    {
        var list = sources.ToList();
        var overlay = new ResourceDictionary();

        foreach (var source in list)
        {
            foreach (DictionaryEntry entry in source)
            {
                if (entry.Key is not string key || !key.EndsWith(".Color", StringComparison.Ordinal) || Map(key[..^".Color".Length], colors) is not { } color)
                {
                    continue;
                }

                var brushKey = $"{key[..^".Color".Length]}.Brush";
                overlay[key] = color;

                if (list.Any(candidate => candidate.Contains(brushKey)))
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    overlay[brushKey] = brush;
                }
            }
        }

        return overlay;
    }

    private static Color? Zone(string name, HighContrastColors colors)
    {
        var text = colors.WindowText;
        var part = name[(name.LastIndexOf('.') + 1)..];
        var zone = name.Split('.')[1];

        return part switch
        {
            "PillBg" or "AdviceBg" => colors.Window,
            "PillText" or "AdviceIcon" or "AdviceBorder" => text,
            "Glow" => With(text, 0),
            "Dot" or "Segment" or "Line" => With(text, zone switch { "Good" => 0.45, "Elevated" => 0.7, _ => 1 }),
            _ => null
        };
    }

    private static double HeatOpacity(string name)
    {
        var step = Array.IndexOf(heatCo2, name);

        if (step >= 0)
        {
            return heatCo2Opacity[step];
        }

        return name switch
        {
            "Heat.Cool2" or "Heat.Warm2" or "Heat.Dry2" or "Heat.Humid2" => 0.9,
            "Heat.Cool1" or "Heat.Warm1" or "Heat.Dry1" or "Heat.Humid1" => 0.5,
            _ => 0.15
        };
    }

    private static Color With(Color color, double opacity) => Color.FromArgb((byte)Math.Round(opacity * 255), color.R, color.G, color.B);
}
