using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;
using Deskair.App.Platform.Tray;
using Deskair.Domain.Zones;
using Deskair.Services.Platform;

namespace Deskair.App.Platform.Notifications;

/// <summary>
/// The zone-coloured ring shown next to a toast's text. A toast can only show an image file, so each ring is drawn once per run (with <see cref="TrayIconRenderer"/>,
/// on the UI thread, because it reads the colour tokens) and saved under the user's local app data.
/// </summary>
public sealed class ToastLogos
{
    private const int PixelSize = 96;

    private readonly IUiDispatcher dispatcher;
    private readonly string folder;
    private readonly ConcurrentDictionary<Zone, Uri> written = new();

    public ToastLogos(IUiDispatcher dispatcher)
        : this(dispatcher, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deskair", "toast"))
    {
    }

    /// <summary>For tests: another folder.</summary>
    public ToastLogos(IUiDispatcher dispatcher, string folder)
    {
        this.dispatcher = dispatcher;
        this.folder = folder;
    }

    /// <summary>The address of the ring file for a zone.</summary>
    public async Task<Uri> UriForAsync(Zone zone)
    {
        if (written.TryGetValue(zone, out var known) && File.Exists(known.LocalPath))
        {
            return known;
        }

        byte[] png = [];
        await dispatcher.InvokeAsync(() => png = Encode(TrayIconRenderer.Render(zone, PixelSize, null)));

        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"ring-{zone.ToString().ToLowerInvariant()}.png");
        await File.WriteAllBytesAsync(path, png);

        var uri = new Uri(path);
        written[zone] = uri;

        return uri;
    }

    private static byte[] Encode(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);

        return stream.ToArray();
    }
}
