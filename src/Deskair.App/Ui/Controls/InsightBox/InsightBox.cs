using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>A quiet panel for one sentence of insight (neutral background, no border). Empty content collapses it.</summary>
public class InsightBox : ContentControl
{
    static InsightBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InsightBox), new FrameworkPropertyMetadata(typeof(InsightBox)));
    }
}
