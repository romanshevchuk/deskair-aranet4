using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Deskair.App.ViewModels.Tray;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Owns the notification-area icon. The icon and tooltip follow <see cref="TrayViewModel"/>: the colour always updates, even when toasts are off.
/// Left click opens the flyout (or closes it), right click opens the menu: Open Deskair, the status line, Settings, Quit.
/// The icon is drawn at the size Windows uses for small icons, so it is sharp (docs/plans/spike-s4-tray-icons.md).
/// </summary>
public sealed class TrayIconHost : IDisposable
{
    private readonly TrayViewModel viewModel;
    private readonly FlyoutController flyout;
    private readonly ILogger<TrayIconHost> logger;
    private TaskbarIcon? icon;
    private Icon? currentIcon;

    public TrayIconHost(TrayViewModel viewModel, FlyoutController flyout, ILogger<TrayIconHost> logger)
    {
        this.viewModel = viewModel;
        this.flyout = flyout;
        this.logger = logger;
    }

    /// <summary>Puts the icon in the notification area. Returns false when that fails (the failure is logged); the app then works without a tray.</summary>
    public bool Start()
    {
        try
        {
            icon = new TaskbarIcon
            {
                MenuActivation = PopupActivationMode.RightClick,
                NoLeftClickDelay = true,
                ContextMenu = BuildMenu()
            };
            icon.TrayLeftMouseUp += (_, _) => _ = flyout.ToggleAsync();
            icon.SetBinding(TaskbarIcon.ToolTipTextProperty, new Binding(nameof(TrayViewModel.Tooltip)) { Source = viewModel });
            icon.ForceCreate(enablesEfficiencyMode: false);

            ShowCurrentIcon();
            viewModel.PropertyChanged += OnViewModelChanged;

            return true;
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(exception, "Creating the tray icon failed.");
            }

            Dispose();

            return false;
        }
    }

    public void Dispose()
    {
        viewModel.PropertyChanged -= OnViewModelChanged;
        icon?.Dispose();
        icon = null;
        Release(ref currentIcon);
    }

    private ContextMenu BuildMenu()
    {
        var open = new MenuItem { FontWeight = FontWeights.SemiBold, Command = viewModel.OpenCommand };
        open.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(TrayViewModel.OpenText)) { Source = viewModel });

        var status = new MenuItem { IsEnabled = false };
        status.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(TrayViewModel.StatusText)) { Source = viewModel });

        var settings = new MenuItem { Command = viewModel.OpenSettingsCommand };
        settings.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(TrayViewModel.SettingsText)) { Source = viewModel });

        var quit = new MenuItem { Command = viewModel.QuitCommand };
        quit.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(TrayViewModel.QuitText)) { Source = viewModel });

        var menu = new ContextMenu();
        menu.Items.Add(open);
        menu.Items.Add(status);
        menu.Items.Add(new Separator());
        menu.Items.Add(settings);
        menu.Items.Add(new Separator());
        menu.Items.Add(quit);

        return menu;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TrayViewModel.Zone) or nameof(TrayViewModel.Co2))
        {
            ShowCurrentIcon();
        }
    }

    private void ShowCurrentIcon()
    {
        if (icon is null)
        {
            return;
        }

        var width = NativeMethods.GetSystemMetrics(NativeMethods.SmallIconWidth);
        var size = width > 0 ? width : 16;
        var next = ToIcon(TrayIconRenderer.Render(viewModel.Zone, size, null));
        var previous = currentIcon;

        icon.Icon = next;
        currentIcon = next;
        Release(ref previous);
    }

    private static Icon ToIcon(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        using var picture = new Bitmap(stream);

        return Icon.FromHandle(picture.GetHicon());
    }

    private static void Release(ref Icon? value)
    {
        if (value is null)
        {
            return;
        }

        NativeMethods.DestroyIcon(value.Handle);
        value.Dispose();
        value = null;
    }
}
