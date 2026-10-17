using System.Windows.Input;
using Deskair.App.ViewModels.Shell;

namespace Deskair.App.Ui.Views.Shell;

/// <summary>The keyboard shortcuts of the main window (accessibility.md "Keyboard"): Ctrl+1, 2 and 3 open Live, History and Settings; F6 moves between the header and the page.</summary>
public static class ShellShortcuts
{
    /// <summary>The section a Ctrl+digit shortcut opens, or null for any other key.</summary>
    public static ShellSection? SectionFor(Key key, ModifierKeys modifiers)
    {
        if (modifiers != ModifierKeys.Control)
        {
            return null;
        }

        return key switch
        {
            Key.D1 or Key.NumPad1 => ShellSection.Live,
            Key.D2 or Key.NumPad2 => ShellSection.History,
            Key.D3 or Key.NumPad3 => ShellSection.Settings,
            _ => null
        };
    }

    /// <summary>True for F6 and Shift+F6, which jump between the header and the page (the Windows convention for moving between regions).</summary>
    public static bool IsRegionSwitch(Key key, ModifierKeys modifiers) => key == Key.F6 && (modifiers is ModifierKeys.None or ModifierKeys.Shift);
}
