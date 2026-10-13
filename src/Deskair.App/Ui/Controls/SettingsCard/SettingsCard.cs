using System.Windows;
using System.Windows.Controls;

namespace Deskair.App.Ui.Controls;

/// <summary>
/// A card with a section label as its header and <see cref="SettingsRow"/> rows as content. Each row draws a divider above itself;
/// the card hides the divider of the first row. Without a header the label line is not shown.
/// </summary>
public class SettingsCard : HeaderedContentControl
{
    static SettingsCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingsCard), new FrameworkPropertyMetadata(typeof(SettingsCard)));
    }
}
