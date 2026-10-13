namespace Deskair.App.Platform.Notifications;

/// <summary>The arguments a toast carries back to the app when the user presses its body or a button.</summary>
public static class ToastArguments
{
    public const string Open = "action=open";
    public const string Snooze = "action=snooze";

    /// <summary>The action for the arguments of a click, or null when they are not ours.</summary>
    public static ToastAction? Parse(string arguments)
    {
        return arguments switch
        {
            Open => ToastAction.Open,
            Snooze => ToastAction.Snooze,
            _ => null
        };
    }
}
