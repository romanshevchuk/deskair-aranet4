using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>Three stats side by side (value, then label) with neutral dividers between them; laid out by <see cref="StatTriplePanel"/>.</summary>
public class StatTriple : ItemsControl
{
    static StatTriple()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatTriple), new FrameworkPropertyMetadata(typeof(StatTriple)));
    }
}
