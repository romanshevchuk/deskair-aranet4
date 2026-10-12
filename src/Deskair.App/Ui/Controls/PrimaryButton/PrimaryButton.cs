using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

public class PrimaryButton : Button
{
    static PrimaryButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PrimaryButton), new FrameworkPropertyMetadata(typeof(PrimaryButton)));
    }
}
