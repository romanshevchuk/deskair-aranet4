using System.Windows;
using Deskair.App.ViewModels.ChartModels;
using Deskair.Domain.Zones;

namespace Deskair.App.Ui.Charts;

public sealed class ZoneRibbon : ChartBase
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments),
        typeof(IReadOnlyList<ChartZoneSegment>),
        typeof(ZoneRibbon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));


    public IReadOnlyList<ChartZoneSegment>? Segments
    {
        get => (IReadOnlyList<ChartZoneSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    // TODO(CHART-23) [M, sonnet]
    // Spec: docs/design/components.md "24 h ribbon"; docs/design/screens-and-states.md "Screens" (Flyout: drawn at native size)
    // Input : Zone segments for the last 24 hours (144 of 10 minutes).
    // Output: A ribbon of zone-coloured segments; a window glyph at airing events is a later feature.
    // Rules : Draw at the size it is given; the flyout uses a 22 px tall version, never a scaled-down copy.
    // Edge  : Missing segments draw as a gap.
    // Done  : The gallery shows the ribbon at Live size and at flyout size.
    protected override IReadOnlyList<IChartLayer> CreateLayers()
        => throw new NotImplementedException("TODO(CHART-23)");
}
