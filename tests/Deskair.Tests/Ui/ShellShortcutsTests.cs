using System.Windows.Input;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Shell;

namespace Deskair.Tests.Ui;

public class ShellShortcutsTests
{
    [Theory]
    [InlineData(Key.D1, ShellSection.Live)]
    [InlineData(Key.D2, ShellSection.History)]
    [InlineData(Key.D3, ShellSection.Settings)]
    [InlineData(Key.NumPad3, ShellSection.Settings)]
    public void Ctrl_and_a_digit_opens_that_section(Key key, ShellSection expected)
    {
        Assert.Equal(expected, ShellShortcuts.SectionFor(key, ModifierKeys.Control));
    }

    [Theory]
    [InlineData(Key.D1, ModifierKeys.None)]
    [InlineData(Key.D1, ModifierKeys.Alt)]
    [InlineData(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)]
    [InlineData(Key.D4, ModifierKeys.Control)]
    [InlineData(Key.A, ModifierKeys.Control)]
    public void Other_keys_open_nothing(Key key, ModifierKeys modifiers)
    {
        Assert.Null(ShellShortcuts.SectionFor(key, modifiers));
    }
}
