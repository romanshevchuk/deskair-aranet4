# Handoff: after the Live vertical slice

*Last reviewed: 2026-10*

For the next agent. Repo: `c:\Personal\Dev\Projects\Deskair` (use absolute paths there). Ignore the other project directory and do not read `../deskair-legacy`.

## Rules you must follow

- Read `AGENTS.md`, this file, `docs/plans/m1-skeleton-brief.md` (sections 4 and 6) and `docs/plans/todo-index.md` first.
- **Do not commit.** The owner commits by hand. Make no changes under `.claude/`.
- **Plan first:** for any batch of cards, list the cards and wait for approval before editing.
- **Ask before adding a NuGet package or project reference.** Approved so far: ExcelDataReader (added) and `H.NotifyIcon.Wpf` for the tray (approved, not added yet). The toast library waits for spike S5.
- **No subagents.** The owner decided to work with one agent only.
- For each card: write or unskip the test, see it fail, implement only what the card says, run the three checks, remove the card comment and the `NotImplementedException`, remove the row in `todo-index.md` and fix its totals line.
- If the docs are silent, add a question to `docs/plans/questions.md` and move on. Do not guess.
- When you find a gap in the prototype (`design/prototype/index.html`) during implementation, fix it in the prototype and in the docs together. The owner asked for this.
- Do not mention vendor names outside `Deskair.Sensors` and `App/Composition`. Do not use `DateTime.Now`. UI strings and colours come from resources and tokens.

## Checks (all must pass before a card is done)

```
dotnet build Deskair.sln
dotnet test tests/Deskair.Tests
dotnet format Deskair.sln --verify-no-changes
```

State now: build 0 warnings and 0 errors; **785 tests, 768 passed, 17 skipped, 0 failed** (stable over repeated runs); format clean. `todo-index.md`: **51 cards** (L 12, M 24, S 15; haiku 25, sonnet 26). Nothing is committed.

## How to see what you built (important)

You cannot see the UI, so use these:

| What | How |
|---|---|
| Gallery of every control, light and dark, English and Ukrainian, as PNG | `$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~GalleryRenderTests"` writes `artifacts/visual/gallery-*.png` |
| The whole Live screen with real view models and simulated data (normal, stale, low battery, dark, Ukrainian, Fahrenheit) | same switch, `--filter "FullyQualifiedName~MainWindowRenderTests"` writes `artifacts/visual/live-*.png` |
| The prototype as PNG | `msedge --headless=new --window-size=1280,6000 --screenshot=<png> file:///C:/Personal/Dev/Projects/Deskair/design/prototype/index.html`, then crop slices with System.Drawing in PowerShell |
| The real running app | `dotnet run --project src/Deskair.App -- --simulated` (own temp data folder, no sensor). To capture only its window use the Windows `PrintWindow` call, not a screen capture |

Open the PNGs with the Read tool. `artifacts/` is git-ignored. The render tests only run when `DESKAIR_RENDER=1`.

## Done in this session (all green, nothing committed)

- **Decisions:** D-0015 (no recovery-time estimate; advice text follows the CO₂ trend; mini cards show the plain trend, and a temperature or humidity label only when outside the ideal range; pressure never has a comfort label). Q4 (selected device remembered), Q6 (temperature unit °C/°F, stored data stays °C), Q24 to Q26, Q28, Q29 recorded in `questions.md` "Decided".
- **Settings and devices:** `AppSettings.TemperatureUnit` and `SelectedDevice` (persisted), `MetricFormatter` (culture and unit for every metric value), `DeviceRegistry.LoadAsync(preferred)`, `Known`, `Info`.
- **View models (all with tests):** Hero (with stats, zone/value/unit texts), Gauge, MetricTabs (+`SelectMetricCommand`), MiniCards, ChartCard (display-unit conversion, window, end time, ideal range, summary, 24 h ribbon data), Live (public children; reloads on `DeviceSelected` and `SyncCompleted`), Flyout (shares `AirSummaryBuilder` with the hero), Connection (flags, retry, dismiss low battery), Footer, RoomChip, Settings (appearance, alerts, devices, startup, data folder), Onboarding scan, History (toolbar, stats, typical day, `HistoryViewModel`).
- **Domain:** `LiveStatsCalculator` (good share, peak, time in zone), `RibbonCalculator` (144 slots), `TemperatureUnit`.
- **Services:** `ConnectionTracker` (publishes `ConnectionStateChanged`; follows readings, status, device change and the radio; `StartAsync` also starts its own 15 s check for silence, never on the UI thread, and logs failures), `SensorSession` (connect, sync, live feed; retries every 30 s; **runs on the thread pool on purpose**, see gotchas), messages `DeviceSelected`, `SensorStatusUpdated`, `SyncCompleted`, `RetryRequested`.
- **Controls (11 of the catalogue) plus LogoRing:** Gauge, AdviceCard, StatTriple, KeyValueCell, MetricCard, Banner, RoomChip, RoomMenu, BatteryIcon, PrimaryButton, SecondaryButton, LogoRing.
- **Charts:** `ChartArea` (scales, brush lookup), `ChartBase` (render, hover, hit test), `ChartHitTest`, `ChartTicks`; layers Band, Grid, DashedZoneLines, AxisLabel, ZoneLine, Line, IdealBand, Crosshair, NoData, Ribbon; charts `LiveChart`, `Sparkline`, `ZoneRibbon`.
- **Views:** LiveView, HeroCard, ChartCard, MetricTabs, MiniCardsRow, HeaderBar (logo, navigation, room chip and menu), FooterBar.
- **Wiring:** `--simulated` mode (`AppOptions`, `KnownFolders` uses `%TEMP%\Deskair-simulated`), `AppServicesStarter` (testable startup logic: seeds two simulated rooms on first simulated run), `StartupSequence` (settings, language and theme, window, then services), `App` (startup error box, clean stop on exit), DI registrations, `ServiceRegistrationTests` (resolves the whole graph). `BluetoothRadioMonitor` implemented with the Windows radio API (BLE-03a, status `doing`); `SimulatedRadio` for `--simulated`.
- **Phone export:** a real export is in `tests/Deskair.Tests/TestData/Aranet4_0A0B3_Export_Example.xlsx` and has a golden test (720 rows).
- **Bugs fixed that you should not reintroduce:** see "Gotchas".

## Next work (the owner expects this order; plan it and wait for approval)

1. **Write the spike programs.** The owner runs the spikes on their own machine and pastes the output back. You write each spike as a tiny runnable program (a console project or a `--spike-sN` switch is fine; ask before adding a project) with a clear "paste this output here" step and a note file in `docs/plans/` for the answer. Order: **S1 first** (Aranet4 history over BLE; unblocks about 12 cards: BLE-01a, BLE-02a/b/c, ARANET-01a to 04a). Then S2 (flyout placement), S3 (title bar and snap layouts), S4 (crisp tray icons), S5 (toasts). Spike text is in `docs/plans/spikes.md`.
2. **History and Settings views** (the view models exist): HIST-00b, 01c, 05d, 06c; SET-01i to n; SLEEP-01a; STATE-01b/c; FIRST-01b; controls CTRL-06, 10, 11, 12, 14, 15; charts CHART-14 to 17, 21, 22. Add each control to the gallery and check the PNGs against the prototype, as for the Live controls.
3. **SET-01g2** (import and export commands in Settings): add a small `IFileDialogs` service (`PickFileToOpenAsync`, `PickFileToSaveAsync`) in `Services/Platform`, implemented once in App with the built-in WPF dialogs. The owner has seen the idea but **has not yet said the shape is fine**; confirm before building. Export CSV uses the device selected in the header and the full range.
4. **Windows glue** after the spikes: tray (approved package `H.NotifyIcon.Wpf`), flyout positioning, toasts, startup registration (SET-03a/b), power events (SYNC-03a), single instance (WIN-02a), logging (LOG-01a), title bar and backdrop (WIN-01a, Q14).
5. **Smaller improvements the owner mentioned** (decide later, with them): hero card width and wrapping. The stat captions ("Good in last 24 h", "In the Elevated zone") wrap and clip when the window is narrow; the owner suggested a fixed or minimum hero width and wrapping the other elements. Also the full-screen offline view (STATE-01b) and a "can't read history" state (Q27).

## Open questions for the owner (`docs/plans/questions.md` "Open")

Q7 (read the date order from the export header; flag the repeated autumn hour), Q13 (Do Not Disturb unreadable: alerts treated as allowed, owner agreed), Q14 (title bar, needs S3), Q17 (placeholder strings and Ukrainian review; owner will do it later), Q27 (history cannot be read), Q30 (footer signal limits are an assumption: good at -60 dBm or stronger, fair to -75, weak below), Q31 (offline view not built).

## Blocked or waiting on the owner

- Spikes S1 to S5 (the owner runs them; Bluetooth is enabled on their machine).
- The toast library choice (after S5).
- BLE-03a needs a manual check: toggle Bluetooth in Windows settings and confirm `IsOn` follows (only the real, non-simulated app uses it).

## Gotchas found in this and earlier sessions

- **A style kept under a key inside a control dictionary cannot be found at run time** (`DynamicResource` or `StaticResource` from the control's template). The segmented control used to fall back to WPF's default list item because of this. Embed such styles inside the control's own style (see `SegmentedControl.xaml`) and use `DynamicResource` for tokens and text styles. A `Style` set on an element still gets the control's default template underneath.
- **Services must not resume on the UI thread.** Loops started from the UI thread capture its synchronization context; at exit the UI thread waits for them and the process hangs. `SensorSession` starts its loop with `Task.Run` (regression test `The_session_never_depends_on_the_thread_that_started_it`). Do the same for any new background loop.
- **Exit:** `SensorSession` implements both `IDisposable` and `IAsyncDisposable` because the host disposes synchronously.
- **Detached content loses its inherited `DataContext`.** The render harness sets `content.DataContext` before it removes the content from the window.
- **The gallery's own switches reset the theme and language while it loads**, so the render test re-applies them afterwards.
- **WPF's `TextBlock` line height** is about 1.33 times the font size; the hero, mini and history values set `LineHeight` equal to the font size (`BlockLineHeight`) like the prototype.
- **Neighbouring chart blocks leave faint seams** unless their edges are snapped to device pixels (see `RibbonLayer`).
- **Do not use `async void`** for fire-and-forget; start a `Task` and log failures (see `LiveViewModel`).
- **Shell heredocs:** long multi-file heredocs fail to parse in this environment. Create files with the editor tools, and keep shell edits short. PowerShell 5 `Set-Content -Encoding utf8` writes a BOM; `dotnet format` then fails with CHARSET.
- **Analyzers that fail the build:** CA1001, CA1816 (seal test classes that are `IDisposable`), CA1834, CA1822, CA1859, CA1873 (guard logging with `IsEnabled`), xUnit1051 (pass `TestContext.Current.CancellationToken`), IDE1006 (private static fields in camelCase), and sorted usings.
- **Tests with timing:** use `FakeClock` (timers fire only on `Advance`), `Wait.Until` for real-time waits, `FakeDeviceStore.Snapshot()` when a session may be saving from another thread, `FakeUiDispatcher` (runs posted work inline). A test that can hang should have `[Fact(Timeout = …)]`.
- **Build locks:** if a build fails with "file locked by .NET Host", run `dotnet build-server shutdown` and rebuild. Kill a stuck `Deskair.Tests.exe` with `taskkill`.
- **Never run `rm -rf` with a glob after a `cd`;** the safety check blocks it. Unzip into a fresh folder in the scratchpad instead.
- **Strings:** every English key needs a Ukrainian key (a test checks it); product strings also go in `docs/design/localization.md`. Numbers use the selected culture. Gallery-only strings are the `Gallery_*` keys.
- **Tokens:** edit the theme XAML directly and keep light and dark keys identical (a test checks it). Sizes and margins are tokens in `Spacing.xaml` and `Radii.xaml`; use a `GridLength` token for row and column sizes.

## Where to look

| Need | File |
|---|---|
| What a card must do | the card above the stub; its `Spec:` line names the doc |
| Behaviour, thresholds, merge, trend | `docs/product/behaviour-rules.md`, `docs/engineering/data-model.md` |
| Colours and sizes | `docs/design/design-tokens.md`, then `design/prototype/index.html` |
| Adding a control, string, setting | `docs/guides/` |
| Why something was decided | `docs/decisions.md` (latest: D-0015) |
| Dependency rules | `docs/engineering/architecture.md` section 3, `tests/Deskair.Tests/Architecture` |
| Startup and wiring | `src/Deskair.App/Composition/` (`AppServicesStarter`, `StartupSequence`, `ServiceRegistration`) |
