# Handoff: after the first-run, Settings, History and import/export slices

*Last reviewed: 2026-10*

For the next agent. Repo: `c:\Personal\Dev\Projects\Deskair` (use absolute paths there). Ignore the other project directory (`AranetHomeDesktop`) unless the owner points you to it, and do not read `../deskair-legacy`.

## Rules you must follow

- Read `AGENTS.md`, this file, `docs/plans/m1-skeleton-brief.md` (sections 4 and 6) and `docs/plans/todo-index.md` first.
- **Do not commit.** The owner commits by hand (about 60 files are changed and uncommitted). Make no changes under `.claude/`.
- **Plan first:** for any batch of cards, list the cards and wait for approval before editing. The owner often says "go with your recommendations" or "continue"; that counts as approval for what you listed.
- **Ask before adding a NuGet package or project reference.** Approved so far: ExcelDataReader (added) and `H.NotifyIcon.Wpf` for the tray (approved, not added yet). The toast library waits for spike S5.
- **No subagents.** The owner works with one agent only.
- For each card: write or unskip the test, see it fail (or, for a pixel/visual card, break the code once to prove the test can fail), implement only what the card says, run the three checks, remove the card comment and the `NotImplementedException`, remove the row in `todo-index.md` and recompute its totals line (use the awk one-liner in "Checks").
- If the docs are silent, add a question to `docs/plans/questions.md` and move on. Do not guess.
- When you find a gap in the prototype (`design/prototype/index.html`) during implementation, fix it in the prototype and in the docs together (the owner asked for this).
- Do not mention vendor names outside `Deskair.Sensors` and `App/Composition`. Do not use `DateTime.Now`. UI strings and colours come from resources and tokens.
- **The owner is not a native Ukrainian speaker.** Every new string gets an English and a Ukrainian value; mark the Ukrainian `<comment>Draft: needs native review.</comment>` and add product strings to `docs/design/localization.md`.
- The owner wants to be told plainly what is verified and what is not (for example "tested with a fake, not on the real sensor"). Say so.

## Checks (all must pass before a card is done)

```
dotnet build Deskair.sln
dotnet test tests/Deskair.Tests
dotnet format Deskair.sln --verify-no-changes
```

State now: build 0 warnings and 0 errors; **1128 tests, 1097 passed, 31 skipped, 0 failed** (17+5 of the skipped are image-render tests that only run with `DESKAIR_RENDER=1`, the rest are card placeholders); format clean. `todo-index.md`: **10 cards**. Nothing is committed.

Recompute the totals line of `todo-index.md` with:

```
awk -F'|' '/^\| [A-Z]+-[0-9]/ {gsub(/ /,"",$4); gsub(/ /,"",$5); s[$4]++; m[$5]++; t++} END {printf "Totals: %d cards. Sizes: L: %d, M: %d, S: %d. Suggested models: haiku: %d, sonnet: %d.\n", t, s["L"], s["M"], s["S"], m["haiku"], m["sonnet"]}' docs/plans/todo-index.md
```

## How to see what you built (important)

You cannot see the UI, so use these:

| What | How |
|---|---|
| Gallery of every control, light and dark, English and Ukrainian, as PNG | `$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~GalleryRenderTests"` writes `artifacts/visual/gallery-*.png`. The gallery is long (about 6000 px high); crop the part you need with System.Drawing in PowerShell |
| Whole screens with real view models | same switch with `--filter "FullyQualifiedName~MainWindowRenderTests"` (Live), `OnboardingRenderTests` (first run and add a sensor), `SettingsRenderTests`, `HistoryRenderTests`. They write `live-*.png`, `firstrun-*.png`, `settings-*.png`, `history-*.png` |
| The prototype as PNG | `msedge --headless=new --window-size=1280,6000 --screenshot=<png> file:///C:/Personal/Dev/Projects/Deskair/design/prototype/index.html`, then crop |
| The real running app | see "Running the real app" below, and capture its window with the Windows `PrintWindow` call (`PrintWindow(hwnd, dc, 2)` into a `Bitmap`). The first capture of a process works; a later capture of the same process has sometimes returned only the title bar |

Open the PNGs with the Read tool. `artifacts/` is git-ignored. The render tests run only when `DESKAIR_RENDER=1`. In renders, toggle knobs always sit on the "off" side (the animation does not run offscreen); that is not a bug.

## Running the real app (dev switches, all in `AppOptions`)

| Command (from the repo root) | What it does |
|---|---|
| `dotnet run --project src/Deskair.App -- --simulated` | simulated sensor, own temp data folder `%TEMP%\Deskair-simulated`, two seeded rooms |
| `... -- --sensor C3DEC8FCC7C6` | real Bluetooth, that one sensor, own temp data folder `%TEMP%\Deskair-sensor-test` (persists between runs; delete the folder to test a fresh install) |
| `... -- --first-run` | real Bluetooth, empty temp data folder `%TEMP%\Deskair-first-run`, wiped at every start; shows the welcome screen |
| `... -- --gallery` | the control gallery |
| `... -- --spike-s1 [--compare file.xlsx]` | the throwaway S1 spike; writes `artifacts/spike-s1/` |

The owner's sensor is an Aranet4 `0A0B3`, address `C3DEC8FCC7C6`, Bluetooth is on, and the sensor is within reach of this PC, so **you can run the real app yourself** (start it with `Start-Process`, wait, capture the window, `Stop-Process`). A full first sync of a week takes about 25 s. Real-app logs: `%LOCALAPPDATA%\Deskair\logs\deskair-YYYYMMDD.log` (read-only for you: removal of that folder is blocked). The log shows connect, sync result, each live reading, sensor discovery and warnings; **read it before guessing** why something does not work.

## Done (all green, nothing committed)

Earlier sessions: domain, storage, services, Live screen, view models, charts (Live), controls (see the previous handoff in git history if the owner committed it).

This session:

- **S1 (history over BLE) is closed.** Note and results: `docs/plans/spike-s1-ble-history.md` (packet layout, 5040 readings every 120 s, values match the phone export row by row, phone app connected is fine). A real capture is `tests/Deskair.Tests/TestData/aranet4-history-capture.txt` (154 packets).
- **Aranet4 driver** in `Deskair.Sensors/Aranet4`: `Aranet4Protocol` (UUIDs, offsets), `Aranet4Parser` (current readings, one history page, history join with the "a reading arrived mid-download" correction), `Aranet4Sensor` (connect, current, history with progress; **all four metrics are downloaded before any reading is returned**, a failed or stalled download throws and returns nothing), `Aranet4Provider` (scan filter by name or manufacturer id; an address first seen without a name is reported again when its name arrives). `IBleConnection` gained `WriteAsync` (D-0016).
- **Windows Bluetooth** in `App/Platform/Bluetooth`: `WindowsBleScanner`, `WindowsBleConnection`, `BleAddress` (12 upper-case hex digits). Verified on the real sensor: scan, connect, read, write (the history download), discovery in 2 to 8 s. **Not verified:** `SubscribeAsync` (the Aranet4 does not use notifications).
- **Logging:** `RollingFileLoggerProvider` (one file per day, newest 7, never throws). Information lines: app start, sensor found, connected, sync result, each live reading, scan restarts.
- **First run:** `DeviceEnrollment` (Services: enroll, rename), `OnboardingViewModel` (checklist, nearby sensors with signal wording via `SignalClassifier`, room name default "My room", Pair, Retry, no-sensor and Bluetooth-off hints, **scan restarts every 8 s while nothing is found and when the radio turns on** because the radio monitor reports "off" for the first moments after start), `OnboardingView`, `WelcomeCard`. The shell shows it instead of Live while there is no sensor, and also on top of Live when the user presses Add in Settings (`OnboardingViewModel.IsOpen`, Cancel button). The simulated provider is registered only for `--simulated` and the gallery.
- **Live reading first:** after connecting, `SensorSession` shows the current reading at once (`LiveFeed.PollOnceAsync`, announced but **not stored**, because the history sync asks for everything after the newest stored reading), then syncs history, then polls at the interval. The footer shows "Syncing history… N%" (`SyncStarted`/`SyncProgressed`/`SyncCompleted` messages, `ISensor.ReadHistoryAsync(since, progress, ct)` default interface method, `HistoryProgress`).
- **Settings screen:** Appearance (language, theme, temperature unit), Alerts (switches; quiet-hours times not editable yet), Devices (rename, add), Start-up (start with Windows, real per-user Run key in `StartupRegistration`). View models save a change at once (`loading` guard in `Load()`); `SettingsApplier` applies language and theme on `SettingsChanged`; Live and History reload on `SettingsChanged`. New controls: `SettingsCard`, `SettingsRow`, `TextInput` (Enter moves focus on).
- **History screen:** `HistoryToolbar`, `HistoryChart` (bars with `RangeBarLayer`, `AverageLineLayer`, `WeekendShadingLayer`, `GapHatchLayer`; hourly line for Day), `TypicalDayChart` (`HourBarLayer`, `HourAxisLayer`), `TypicalDayCard`, partial `StatsCard`, `HistoryView`. New controls: `CoverageChip`, `ZoneShareBar`, `InsightBox`. `HistoryViewModel` loads when the section opens and when the toolbar, a sync, the room or a setting changes; values are converted to the display unit there.
- **Import and export (SET-01g2, SET-01m, CTRL-06, STATE-01c, History Export):** `IFileDialogs` (open and save, returns null on cancel; `FileTypeFilter` gives the description and extensions, no WPF filter string) and `IFolderOpener` in `Services/Platform`, implemented by `WindowsFileDialogs` and `ExplorerFolderOpener` in `App/Platform/Files`. `ReadingImporters` (Services) finds the importer of the selected device. `IDataFolderChoice` (Services) is the folder picked in Settings: `DataFolderChoice` keeps it in `%LOCALAPPDATA%Deskairdata-folder.txt` and `KnownFolders` uses it from the next start (Q34); `IFileDialogs.PickFolderAsync` picks it. `ImportViewModel` (choose file, preview, confirm or cancel, result line) and `ExportViewModel` (all stored readings of the selected device to CSV) live in `ViewModels/Data` and are shared: `DataSettingsViewModel` holds both plus Open folder, `HistoryViewModel.Export` feeds the toolbar Export button (`ExportCommand` and `ExportStatus` dependency properties). New messages: `ReadingsImported` (History reloads) and `ImportRequested` (shell goes to Settings). `HistoryViewModel.IsEmpty` (via `ReadingQueryService.HasReadingsAsync`) swaps the charts for `EmptyHistoryView`. New control `EmptyStateCard` with a gallery section. Decisions in Q34 and Q35. **Verified:** view models with fakes (real files in a temp folder), renders of Settings, History (data and empty) and the gallery. **Not verified:** the real Windows open and save dialogs and Open folder (you cannot click), and an import of a real phone export through the UI (the importer itself is covered by its own golden tests).
- **Stats card (HIST-06c):** `StatsInput` carries everything the card needs; `StatsViewModel.Update(StatsInput)` sets the change against the period before (`MetricFormatter.Difference` keeps a temperature difference free of the °F offset), best and worst period (`BucketStatistics.Lowest/Highest`, complete buckets only), time in each zone (`ZoneShareCalculator`, `ReadingQueryService.GetZoneShareAsync`, CO₂ only) and the insight (`TypicalDayCalculator.WorstWindow`). `HistoryViewModel` loads the period before and the zone share. Rules and what is not built: Q36 (no "comfort range" for temperature and humidity, no insights for other metrics or the night view). **Verified:** view models and domain code with tests; renders of Week, Month, Year (English and Ukrainian). **Not verified:** the real app with real data.
- **Offline view (STATE-01b):** `OfflineViewModel` (ViewModels/Connection) follows readings, battery, the Bluetooth radio, language and the connection state by itself (no start-up call); `OfflineView` is an `EmptyStateCard` with the message, buttons and steps; `MainWindow` shows it in place of Live while the state is NotConnected (adding a sensor and first run win). `ISystemSettings` (`WindowsSystemSettings`) opens the Bluetooth page. Decision Q31. **Verified:** view model with fakes, renders (light English, dark Ukrainian with Bluetooth off). **Not verified:** the real "Turn on Bluetooth" button and the real not-connected path. The header ring is not grey in this state (HeaderBar, not done).
- **Tray icon renderer (TRAY-01b, spike S4 closed):** `TrayIconRenderer.Render(zone, pixelSize, co2Ppm)` draws the ring, the not-connected icon (dashed ring and slash) or the value icon ("1.1k") onto a frozen 96-dpi bitmap; `FormatValue` shortens the value. Colours are the zone dot tokens plus new `Tray.*` tokens (same in both themes). Note and picture: `docs/plans/spike-s4-tray-icons.md`, `artifacts/visual/tray-icons.png`. The quiet-hours moon badge is not drawn yet (not in the card). `TrayIconHost` (TRAY-01a) must ask Windows for the small icon size (`SM_CXSMICON`). **Not verified:** on a real taskbar.
- **Tray icon (TRAY-01a, first slice):** `H.NotifyIcon.Wpf` 2.4.1 added (approved). `TrayViewModel` (ViewModels/Tray) follows readings, connection state, room and language and gives the zone, value, tooltip and menu texts; `TrayIconHost` (Platform/Tray) puts the icon in the notification area (drawn by `TrayIconRenderer` at the `SM_CXSMICON` size), builds the right-click menu and opens the window on left click; `MainWindowController` keeps the window in the tray when closed and implements `IMainWindowController` (declared in `ViewModels/Shell`, because view models may not use `Platform`). `StartupSequence` starts it after the window. What is left of the tray is in Q37. **Verified:** view model and controller with tests; the simulated app starts with no tray error in the log. **Verified by the owner on a real taskbar:** the icon appears in the zone colour, the tooltip reads "Office · 1,512 ppm · High", and closing the window keeps the icon. **Not verified:** the menu and the left click.
- **Flyout positioning (TRAY-02a):** `FlyoutPositioner.Position(icon, size, workArea)` (pure, DIPs): the side of the icon outside the work area tells where the taskbar is; the flyout goes `Gap` (8) away from it, centred on the icon along the taskbar, clamped inside the work area; an icon inside the work area (overflow pop-up) gets it above, else below. 14 tests. `Shell_NotifyIconGetRect` and the real window come with TRAY-02b. **Not verified:** on real taskbars, other DPIs and two monitors (spike S2).
- **Docs and tokens:** D-0016 in `docs/decisions.md`; Q32 and Q33 in `docs/plans/questions.md`; new strings in `docs/design/localization.md`; new tokens in `Spacing.xaml`/`Typography.xaml`; the prototype's first-run screen lost its room-type row and its settings rows got 4 px padding.

## Next work (plan it and wait for approval)

Remaining cards are in `todo-index.md` (10). Suggested order:

1. **Spikes S2, S3 and S5** (flyout placement, title bar, crisp tray icons, toasts): write each as a runnable program for the owner to run, as was done for S1 (`--spike-sN` switch in `Deskair.App/Composition/Spikes`, a note in `docs/plans/`, output saved under `artifacts/`). Text is in `docs/plans/spikes.md`. Ask before adding a project.
2. **Windows glue** after the spikes: tray (`H.NotifyIcon.Wpf`, TRAY-01a/b), flyout (TRAY-02a/b), toasts (ALERT-02a), Do Not Disturb (ALERT-03a, then the "Alerts paused by Windows" line and quiet-hours time editing in Settings), power events (SYNC-03a), single instance (WIN-02a), title bar and backdrop (WIN-01a, Q14).
3. **Possible robustness fix, not requested:** `LiveFeed` only logs a failed live read and keeps polling a dead connection; it never reconnects. If the owner sees "Not connected" again, read the log first. If reads fail, make a transport failure (`IOException`) escape `LiveFeed.RunAsync` so `SensorSession` reconnects (it retries every 30 s); keep invalid-data errors (`FormatException`) as log-and-continue. Ask the owner first: it changes shared service behaviour and `LiveFeedTests`.

## Open questions for the owner (`docs/plans/questions.md` "Open")

Q7 (read the date order from the export header; flag the repeated autumn hour), Q13 (Do Not Disturb unreadable: treat alerts as allowed, owner agreed), Q14 (title bar, needs S3), Q17 (placeholder strings and Ukrainian review), Q27 (history cannot be read), Q30 (footer signal limits are an assumption), Q32 (a connected Aranet4 reports no signal strength, so the footer shows none: ask whether to pass on the advertisement signal), Q33 (older firmware uses another service id; not supported).

## Blocked or waiting on the owner

- Running spikes S2 to S5 (they run them on their machine and paste or save the output).
- The toast library choice (after S5).
- Manual checks: the file dialogs and Open folder in the real app (press Choose file, Export, Open folder; import the phone export of the owner's sensor), BLE-03a (turn Bluetooth off and on in Windows and confirm the app follows; the log shows the radio state at start), BLE-02c (needs a sensor that uses notifications), pressing Pair and Add in the real UI (you cannot click; these are covered by tests and renders only).

## Gotchas found in this and earlier sessions

- **Services must not resume on the UI thread.** Background loops start with `Task.Run` (see `SensorSession`). `SensorSession` implements `IDisposable` and `IAsyncDisposable`.
- **A style kept under a key inside a control dictionary cannot be found at run time** from a template (`StaticResource`/`DynamicResource`). Embed styles in the control's own style. In `Generic.xaml` templates, `BasedOn="{StaticResource Text.Secondary}"` also fails (BaseStyles are not visible there); use a plain style with setters, or `DynamicResource` tokens. A `Style` set on an element still gets the control's default template underneath, and `Style=` plus a nested `.Style` on the same element is an error.
- **Detached content loses its inherited `DataContext`.** The render harnesses set `content.DataContext` before removing the content from the window.
- **Chart drawing:** a `Border` does not clip children to its rounded corners (clip by hand, see `ZoneShareBar`); neighbouring chart blocks leave seams unless snapped to device pixels. WPF's `TextBlock` line height is about 1.33 times the font size.
- **The radio monitor is asynchronous:** `IBluetoothRadio.IsOn` is false for a moment after start. Anything that checks it once at start-up must also react to `StateChanged`.
- **The sensor only sends an index, not timestamps.** Times are reconstructed from the age of the newest reading and snapped with `SlotMerger.Snap`. The phone export's own timestamps drift by up to a minute; compare by row, not by nearest time.
- **Timing tests with `FakeClock`:** timers fire only on `Advance`. Before advancing, wait until the loop has created its timer: `await Wait.Until(() => clock.PendingTimers >= 1)` (three flaky tests were fixed this way). Lists that a background thread fills while a test reads them must be `ThreadSafeLog<T>`. `ListProgress<T>` records progress synchronously.
- **`StrongReferenceMessenger` allows one handler per message type per recipient**; registering twice throws in the constructor. Messages are sent from whatever thread the sender runs on; view models post to the UI through `IUiDispatcher`.
- **Do not use `async void`**; start a `Task` and log failures. Do not leave a bare `cat` or other stdin-reading command in a shell call (it hangs).
- **Shell heredocs:** long or multi-file heredocs fail to parse in this environment. Create files with the editor tools; keep shell edits short. When you splice text into a file with `sed`/`perl`, check that it applied (a silent no-match happened several times; CRLF files need `\r?\n`). PowerShell 5 `Set-Content -Encoding utf8` writes a BOM and `dotnet format` then fails with CHARSET.
- **Analyzers that fail the build:** CA1001, CA1816, CA1826, CA1834, CA1822, CA1859, CA1873 (guard logging with `IsEnabled`), xUnit1031 (no `.GetAwaiter().GetResult()` in tests), xUnit1051/xUnit1069 (use `TestContext.Current.CancellationToken`), IDE1006 (private static fields in camelCase, constants in PascalCase), sorted usings. Run `dotnet format Deskair.sln` (without `--verify-no-changes`) to fix whitespace and usings, then build.
- **Build locks:** if a build fails with "file locked by .NET Host", run `dotnet build-server shutdown` and rebuild. A stuck app can be stopped with `Stop-Process`.
- **Never run `rm -rf` with a glob after a `cd`;** the safety check blocks it. Deleting `%TEMP%\Deskair-*` folders is fine.
- **Strings:** every English key needs a Ukrainian key (a test checks it); numbers use the selected culture; unit symbols and "CO₂", "Aranet4", "Deskair" are not translated. **Tokens:** edit the theme XAML directly and keep light and dark keys identical (a test checks it); `Metric.Co2.*` tokens already existed.
- **One unreproduced failure** once appeared in `AppServicesStarterTests` cleanup (`session.DisposeAsync`); it never recurred in many later runs. If it comes back, capture the exception text.

## Where to look

| Need | File |
|---|---|
| What a card must do | the card above the stub; its `Spec:` line names the doc |
| Behaviour, thresholds, merge, trend | `docs/product/behaviour-rules.md`, `docs/engineering/data-model.md` |
| BLE protocol as seen on the sensor | `docs/plans/spike-s1-ble-history.md`, `Aranet4Protocol.cs`, `tests/Deskair.Tests/TestData/aranet4-history-capture.txt` |
| Colours and sizes | `docs/design/design-tokens.md`, then `design/prototype/index.html` |
| Adding a control, string, setting, sensor | `docs/guides/` and the skills in `.claude/skills/` |
| Why something was decided | `docs/decisions.md` (latest: D-0016) |
| Dependency rules | `docs/engineering/architecture.md` section 3, `tests/Deskair.Tests/Architecture` |
| Startup and wiring | `src/Deskair.App/Composition/` (`AppServicesStarter`, `StartupSequence`, `ServiceRegistration`, `SettingsApplier`, `AppOptions`) |
| Test helpers | `tests/Deskair.Tests/Fakes/` (`FakeClock`, `FakeAranet4Connection`, `FakeBleScanner`, `Aranet4TestData`, `ThreadSafeLog`, `ListProgress`, `Wait`) |
