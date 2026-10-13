using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Deskair.App.ViewModels.Tray;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;

namespace Deskair.App.Platform.Tray;

/// <summary>
/// Owns the notification-area icon. The icon and tooltip follow <see cref="TrayViewModel"/>: the colour always updates, even when toasts are off.
/// Left click opens the flyout (or closes it), right click opens the menu (<see cref="TrayMenuViewModel"/>). With the setting on, the icon shows the value instead of the ring.
/// The icon is drawn at the size Windows uses for small icons, so it is sharp (docs/plans/spike-s4-tray-icons.md).
/// </summary>
public sealed class TrayIconHost : IDisposable
{
    private readonly TrayViewModel viewModel;
    private readonly TrayMenuViewModel menuModel;
    private readonly FlyoutController flyout;
    private readonly ILogger<TrayIconHost> logger;
    private TaskbarIcon? icon;
    private Icon? currentIcon;

    public TrayIconHost(TrayViewModel viewModel, TrayMenuViewModel menuModel, FlyoutController flyout, ILogger<TrayIconHost> logger)
    {
        this.viewModel = viewModel;
        this.menuModel = menuModel;
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
        var open = Item(menuModel.OpenCommand, nameof(TrayMenuViewModel.OpenText));
        open.FontWeight = FontWeights.SemiBold;

        var status = new MenuItem { IsEnabled = false };
        status.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(TrayViewModel.StatusText)) { Source = viewModel });

        var resume = Item(menuModel.ResumeCommand, nameof(TrayMenuViewModel.ResumeText));
        resume.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(TrayMenuViewModel.IsSnoozed)) { Source = menuModel, Converter = new BooleanToVisibilityConverter() });

        // The tick shows the setting; choosing the item changes the setting, and the tick follows it.
        var showValue = Item(menuModel.ToggleShowValueCommand, nameof(TrayMenuViewModel.ShowValueText));
        showValue.SetBinding(MenuItem.IsCheckedProperty, new Binding(nameof(TrayMenuViewModel.ShowValue)) { Source = menuModel, Mode = BindingMode.OneWay });

        var menu = new ContextMenu();
        menu.Items.Add(open);
        menu.Items.Add(status);
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(menuModel.SnoozeHourCommand, nameof(TrayMenuViewModel.SnoozeHourText)));
        menu.Items.Add(Item(menuModel.SnoozeTomorrowCommand, nameof(TrayMenuViewModel.SnoozeTomorrowText)));
        menu.Items.Add(resume);
        menu.Items.Add(new Separator());
        menu.Items.Add(showValue);
        menu.Items.Add(Item(menuModel.OpenSettingsCommand, nameof(TrayMenuViewModel.SettingsText)));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(menuModel.QuitCommand, nameof(TrayMenuViewModel.QuitText)));

        // A snooze ends by itself, without an event, so the menu looks again each time it opens.
        menu.Opened += (_, _) => menuModel.Refresh();

        return menu;
    }

    private MenuItem Item(ICommand command, string textProperty)
    {
        var item = new MenuItem { Command = command };
        item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(textProperty) { Source = menuModel });

        return item;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TrayViewModel.Zone) or nameof(TrayViewModel.Co2) or nameof(TrayViewModel.ShowValue))
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
        var next = ToIcon(TrayIconRenderer.Render(viewModel.Zone, size, viewModel.ShowValue ? viewModel.Co2 : null));
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
