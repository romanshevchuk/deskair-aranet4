using System.Windows;

namespace Deskair.App.Ui.Motion;

/// <summary>Where the selection indicator of a segmented control or the nav underline goes: the left edge and the width, relative to the row of items.</summary>
public static class SlideMath
{
    /// <summary>The indicator covers the selected item (a segment), or, with a fixed width, is centred under it (the nav underline).</summary>
    public static (double X, double Width) Target(Rect item, double? fixedWidth = null)
        => fixedWidth is { } width ? (item.X + ((item.Width - width) / 2), width) : (item.X, item.Width);
}
