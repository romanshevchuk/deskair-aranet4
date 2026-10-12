using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

public class SecondaryButton : Button
{
    static SecondaryButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SecondaryButton), new FrameworkPropertyMetadata(typeof(SecondaryButton)));
    }
}
