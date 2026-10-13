# Spike S3: title bar, Mica and snap layouts

*Last reviewed: 2026-10*

Question (from [spikes.md](spikes.md)): which title bar approach keeps snap layouts and Mica? Exit: the chosen approach works on Windows 11; the Windows 10 fallback looks right.

> **The spike code was removed on 2026-10-13** (last present in commit `07d4f12`, folder `src/Deskair.App/Composition/Spikes/`). The commands in this note no longer exist; the note stays as the record of what was tried and found. The result is in the real window (`MainWindow`, `HeaderBar`, `BackdropService`).

## Options (researched 2026-10-13)

| Option | Verdict | Why |
|---|---|---|
| **`WindowChrome` (built into WPF), caption height 52, the system's caption buttons, Mica through the DWM call** | **Chosen** | No package. Verified on Windows 11 (below): integrated 52 px header, native buttons, snap layouts. |
| Plain native title bar (header below it) | Works, not chosen | Works the same for snap layouts and Mica, but the system title bar takes its own 32 px and the prototype's header (logo, tabs, room chip in one 52 px bar) cannot be integrated. |
| `WindowChrome` with our own caption buttons | Rejected | On Windows 11 the system buttons are drawn anyway (our buttons would be a second set on top), so there is nothing to gain; replacing them means `WindowStyle=None`, hit-test code for snap layouts and our own pressed and hover states. |
| .NET 10 built-in Fluent theme | Not usable for this | Experimental; Mica and Acrylic were still a future item in the [dotnet/wpf plan](https://github.com/dotnet/wpf/discussions/10387) and the backdrop property is internal ([issue #9496](https://github.com/dotnet/wpf/issues/9496)). |
| WPF-UI (lepoco), `FluentWindow` and `TitleBar` | Not needed | Maintained (4.0.2, targets .NET 10), but a new dependency with its own theme system next to our design tokens, for something the built-in call does. Fallback only; ask before adding. |

Snap layouts appear when the window has a system maximize button and the hit test over it answers `HTMAXBUTTON` ([Microsoft](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-snap-layout-menu)).

## The spike

`dotnet run --project src/Deskair.App -- --spike-s3 --variant native|chrome|custom` (throwaway, `Composition/Spikes/SpikeS3.cs`) opens a window with a 52 px header like the prototype's (logo, three tabs, room chip), asks Windows for Mica (`DWMWA_SYSTEMBACKDROP_TYPE`, only from build 22621; otherwise a solid colour) and, a second after it opens, asks itself what Windows would do at a few points. The answers go to `artifacts/spike-s3/report.txt`.

## Results (agent, Windows 11 build 26200, 100 % scale, 2026-10-13)

| | native | chrome | custom |
|---|---|---|---|
| Mica requested | ok (0x00000000) | ok | ok |
| Header drag (hit test on the empty header) | system title bar | `HTCAPTION` | `HTCAPTION` |
| Tabs and room chip react | n/a | `HTCLIENT` | `HTCLIENT` |
| Resize edges and corners | `HTLEFT`, `HTTOPLEFT` | same | same |
| System caption buttons | yes, in a separate 32 px bar | yes, in the 52 px header (32 px high, at the top) | yes, and ours on top of them (double) |
| Maximize button answers `HTMAXBUTTON` | yes | **yes** | yes (ours) |
| **Snap layouts box appears on hover** (photographed) | **yes** | **yes** | not tried |

Pictures: `artifacts/spike-s3/{native,chrome,custom}.png` and `snap-{chrome,native}.png` (the snap layouts box next to the maximize button).

Notes for WIN-01a:
- The system buttons are 32 px high at the top of the 52 px header, so tabs and chip (centred at 26 px) sit a little lower than the buttons (centred at 16 px). Acceptable; a taller button is not available in WPF.
- Leave room at the right for three buttons, 46 px each at 100 % scale (`DWMWA_CAPTION_BUTTON_BOUNDS` gives the exact width at any scale).
- Anything clickable in the header needs `WindowChrome.IsHitTestVisibleInChrome="True"`; the empty part drags and double-click maximizes by itself.
- `DWMWA_USE_IMMERSIVE_DARK_MODE` must follow the theme (the spike only used light).
- The minimum window width should stay at 500 px or less for snap zones.

## Not verified

- **Windows 10:** this machine has Windows 11 only. The code path is guarded by the build number (Mica only from 22621); `WindowChrome` itself is supported on Windows 10, with square corners and no snap layouts box (decision D-0001). Please check on a Windows 10 machine or VM if you have one.
- **Maximized, two monitors, other scales:** the real main window was checked maximized on this machine (WIN-01a, header and footer fit, nothing clipped). Not tried: two monitors with different scales, other scales than 100 %.
- **Mica look in dark theme and with accent colours.**

## Run it (owner), optional

```
dotnet run --project src/Deskair.App -- --spike-s3 --variant chrome
```

Look at: Mica (the background is tinted by the wallpaper), the three system buttons at the top right, the snap layouts box on hovering the maximize button, dragging the empty header, double-click to maximize, the tabs and chip reacting to clicks. Then maximize and restore.

## Exit criteria (from spikes.md)

- [x] The chosen approach works on Windows 11 (`WindowChrome` with the system caption buttons and Mica through the DWM call).
- [ ] The Windows 10 fallback looks right (not tested; see above).
