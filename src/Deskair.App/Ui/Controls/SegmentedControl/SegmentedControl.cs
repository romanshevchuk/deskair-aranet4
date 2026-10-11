using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// One selected option out of a few: tabs, ranges, filters. Items are ordinary list items, so
/// arrow keys move the selection and screen readers announce a selectable list.
/// </summary>
public class SegmentedControl : ListBox
{
    static SegmentedControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentedControl), new FrameworkPropertyMetadata(typeof(SegmentedControl)));
    }
}
