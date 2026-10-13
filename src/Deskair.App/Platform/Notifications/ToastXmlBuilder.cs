using System.Xml.Linq;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// Builds the XML of an alert toast. Windows draws the card itself, so the look is the text, the zone ring as the app logo and two buttons: Snooze comes back to the app,
/// Dismiss is handled by Windows without waking the app.
/// </summary>
public static class ToastXmlBuilder
{
    /// <param name="text">The title, body and button words.</param>
    /// <param name="logo">The zone-coloured ring shown next to the text (a local file).</param>
    /// <param name="tag">Not used in the XML; the caller puts it on the notification so a newer toast for the same room replaces the older one.</param>
    public static string Build(ToastText text, Uri logo, string tag)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(logo);

        var toast = new XElement(
            "toast",
            new XAttribute("launch", ToastArguments.Open),
            new XAttribute("activationType", "foreground"),
            new XElement(
                "visual",
                new XElement(
                    "binding",
                    new XAttribute("template", "ToastGeneric"),
                    new XElement("image", new XAttribute("placement", "appLogoOverride"), new XAttribute("src", logo.AbsoluteUri)),
                    new XElement("text", text.Title),
                    new XElement("text", text.Body))),
            new XElement(
                "actions",
                new XElement("action", new XAttribute("content", text.SnoozeButton), new XAttribute("arguments", ToastArguments.Snooze), new XAttribute("activationType", "foreground")),
                new XElement("action", new XAttribute("content", text.DismissButton), new XAttribute("arguments", "dismiss"), new XAttribute("activationType", "system"))));

        return toast.ToString(SaveOptions.DisableFormatting);
    }
}
