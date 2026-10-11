# WPF mapping and risks

*Last reviewed: 2026-10*

- **Platform:** .NET 10 LTS, target `net10.0-windows10.0.19041.0` for the app (other projects `net10.0`), PerMonitorV2 DPI manifest, MVVM (for example CommunityToolkit.Mvvm).
- **Theming:** the tokens become two ResourceDictionaries (light, dark). Use `DynamicResource`. Follow the system theme when set to System. Dark title bar through DWM.
- **Backdrop:** Windows 11 Mica through the DWM system-backdrop attribute (or a library such as WPF-UI). Windows 10: solid `window.bg`; window corners are square and there is no snap-layout popup.
- **Bluetooth:** Windows Bluetooth LE APIs behind `IBleConnection` (`App/Platform/Bluetooth`). Check radio state to show the "Bluetooth off" message.
- **Tray:** NotifyIcon through a library such as H.NotifyIcon. Render icons with RenderTargetBitmap at 16, 20, 24 and 32 px. Position the flyout with `Shell_NotifyIconGetRect`; close it on deactivation or Esc. Test taskbar at every edge, multiple monitors and DPI.
- **Toasts:** Windows notification toolkit. Packaged (Store) apps have an identity; unpackaged builds (GitHub, winget) need extra setup. Windows applies Do Not Disturb itself.
- **Charts:** one base custom control (axes, bands, hit testing, hover) with Live, History, Typical-day and Ribbon variants, drawn with DrawingContext. Gap hatching: tiled rotated-line brush. Zone-coloured line: LinearGradientBrush mapped to y values.
- **Custom title bar:** if used, handle the maximize-button hit test (or use a library); otherwise snap-layout popups are lost on Windows 11. The alternative is a native title bar.
- **MVVM:** CommunityToolkit.Mvvm (see `docs/decisions.md` D-0009).
- **Localization:** resx plus a runtime-switchable markup extension (see `docs/design/localization.md`).
- **Accessibility:** see `docs/design/accessibility.md`.

## Riskiest pieces (prototype first)

1. Tray flyout positioning.
2. Title bar with Mica and snap layouts.
3. Sensor history sync.
