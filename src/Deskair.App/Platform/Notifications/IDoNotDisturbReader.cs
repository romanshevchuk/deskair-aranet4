namespace Deskair.App.Platform.Notifications;

/// <summary>Looks at Windows and says whether Do Not Disturb is on. Null means Windows would not tell (the way of asking stopped working).</summary>
public interface IDoNotDisturbReader
{
    bool? Read();
}
