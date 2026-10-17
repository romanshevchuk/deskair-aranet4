using System.Windows;
using Deskair.App.Ui.Motion;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// One selected option out of a few: tabs, ranges, filters. Items are ordinary list items, so
/// arrow keys move the selection and screen readers announce a selectable list. The highlight slides to the chosen segment.
/// </summary>
public class SegmentedControl : SlidingSelector
{
    static SegmentedControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentedControl), new FrameworkPropertyMetadata(typeof(SegmentedControl)));
    }
}
