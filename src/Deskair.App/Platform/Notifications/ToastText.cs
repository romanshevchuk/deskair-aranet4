namespace Deskair.App.Platform.Notifications;

/// <summary>The words of a toast, already in the user's language.</summary>
public sealed record ToastText(string Title, string Body, string SnoozeButton, string DismissButton);
