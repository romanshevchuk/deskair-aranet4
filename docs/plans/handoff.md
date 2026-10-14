# Handoff (updated 2026-10-14)

*Last reviewed: 2026-10-14*

For the next agent. Repo: `c:\Personal\Dev\Projects\Deskair` (use absolute paths there). Ignore the other project directory (`AranetHomeDesktop`) unless the owner points you to it, and do not read `../deskair-legacy`.

## Rules you must follow

- Read `AGENTS.md` and this file first.
- **Commits:** the owner asks for commits and pushes to `main` in small steps. Commit under the owner's git identity and add NO `Co-Authored-By` or Claude footer (the owner objected to it). Make no changes under `.claude/`.
- **Plan first:** for any batch of work, list it and wait for approval before editing. "Go with your recommendations" or "continue" counts as approval for what you listed.
- **Ask before adding a NuGet package or project reference.** Approved and in use: ExcelDataReader, `H.NotifyIcon.Wpf`. Toasts use the inbox Windows API; the Windows App SDK is the fallback and needs a new question first (Q8).
- **Subagents:** only when the owner asks for them (an independent code review through the `code-review` skill found real bugs on 2026-10-13).
- For each change: write or unskip the test, see it fail (for a pixel or visual change, break the code once to prove the test can fail), implement, run the three checks, update the docs that describe the behaviour.
- If the docs are silent, add a question to `docs/plans/questions.md` and move on. Do not guess.
- When you find a gap in the prototype (`design/prototype/index.html`), fix it in the prototype and in the docs together (the owner asked for this).
- Do not mention vendor names outside `Deskair.Sensors` and `App/Composition`. Do not use `DateTime.Now`. UI strings and colours come from resources and tokens.
- **The owner is not a native Ukrainian speaker.** Every new string gets an English and a Ukrainian value; mark the Ukrainian `<comment>Draft: needs native review.</comment>` and add product strings to `docs/design/localization.md`.
- The owner wants to be told plainly what is verified and what is not (for example "tested with a fake, not on the real sensor"). Say so.

## Checks (all must pass before work is done)

```
dotnet build Deskair.sln
dotnet test tests/Deskair.Tests
dotnet format Deskair.sln --verify-no-changes
```

State now (2026-10-14): build 0 warnings and 0 errors; **1436 tests, 0 failed** (40 skipped: the opt-in image-render tests, `DESKAIR_RENDER=1`); format clean. No TODO cards are left in the code. Everything on the v1 list in `backlog.md` is built except the accessibility pass (A11Y-01), the native Ukrainian review (I18N-02) and Store packaging (REL-01). The latest work is two passes aligning the look with the prototype (Q47).

## How to see what you built (important)

You cannot see the UI, so use these:

| What | How |
|---|---|
| Gallery of every control, light and dark, English and Ukrainian, as PNG | `$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~GalleryRenderTests"` writes `artifacts/visual/gallery-*.png`. The gallery is long (about 6000 px high); crop the part you need with System.Drawing in PowerShell |
| Whole screens with real view models | same switch with `--filter "FullyQualifiedName~RenderTests"` (or one of `MainWindowRenderTests` (Live), `OnboardingRenderTests`, `SettingsRenderTests`, `HistoryRenderTests`, the flyout and tray icon tests). They write `live-*.png`, `firstrun-*.png`, `settings-*.png`, `history-*.png`, `flyout-*.png`, `tray-icons.png` |
| The prototype as PNG | `design/prototype-shot.ps1` (see its header) screenshots the prototype window in a given state to `artifacts/proto/` at the same scale as the renders |
| The real running app | see "Running the real app", and capture its window with the Windows `PrintWindow` call (`PrintWindow(hwnd, dc, 2)` into a `Bitmap`). The first capture of a process works; a later capture of the same process has sometimes returned only the title bar |

Open the PNGs with the Read tool. `artifacts/` is git-ignored. In renders, toggle knobs always sit on the "off" side (the animation does not run offscreen); that is not a bug.

## Running the real app (dev switches, all in `AppOptions`)

| Command (from the repo root) | What it does |
|---|---|
| `dotnet run --project src/Deskair.App -- --simulated` | simulated sensor, own temp data folder `%TEMP%\Deskair-simulated`, two seeded rooms |
| `... -- --sensor C3DEC8FCC7C6` | real Bluetooth, that one sensor, own temp data folder `%TEMP%\Deskair-sensor-test` (persists between runs; delete the folder to test a fresh install) |
| `... -- --first-run` | real Bluetooth, empty temp data folder `%TEMP%\Deskair-first-run`, wiped at every start; shows the welcome screen |
| `... -- --gallery` | the control gallery |

The owner's sensor is an Aranet4 `0A0B3`, address `C3DEC8FCC7C6`, Bluetooth is on, and the sensor is within reach of this PC, so **you can run the real app yourself** (start it with `Start-Process`, wait, capture the window, `Stop-Process`). A full first sync of a week takes about 25 s. Real-app logs: `%LOCALAPPDATA%\Deskair\logs\deskair-YYYYMMDD.log` (read-only for you: removal of that folder is blocked). The log shows connect, sync result, each live reading, sensor discovery and warnings; **read it before guessing** why something does not work.

## Where things stand

The per-feature rules and what was and was not verified are in the decided Q entries of `docs/plans/questions.md` (tray Q37, flyout Q38, single instance Q39, resume Q40, toasts Q41, snooze Q42, Do Not Disturb Q43, reconnect Q44, try again Q45, sleep Q46, prototype alignment Q47, title bar Q14, offline Q31, stats Q36). The spike notes (`spike-s1` to `spike-s5`, `research-do-not-disturb.md`) record how the Windows parts were found out. The Aranet4 history download is documented in `spike-s1-ble-history.md` (5040 readings every 120 s; a real capture is `tests/Deskair.Tests/TestData/aranet4-history-capture.txt`).

**Next: a UI/UX improvement pass.** Brief for it: `docs/plans/ui-review-brief.md`.

### Open, in the owner's hands (nobody else can do them)

Not verified on real hardware or by eye:

- Quiet-hours and night-hours times typed by hand in Settings.
- A real press on an alert toast (Snooze, Open), including with the app not running; Do Not Disturb and "paused by Windows" rows by eye.
- The Snooze menu in the flyout stays open (a context menu inside the flyout window might close it on deactivation); the tray menu items and the value icon in the real tray; closing the flyout on click-away and Esc.
- Flyout and tray on other taskbar edges, DPI scales and a second monitor (S2).
- "Try again" and the 30 s read timeout with the sensor out of range; sleep and wake of the PC with the real sensor.
- Sleep card and History Night view on real data; Stats card on real data.
- The real open and save dialogs, Open folder, and importing the phone export through the UI.
- Pressing Pair and Add in the real UI.
- `IBleConnection.SubscribeAsync` is not used by the Aranet4 and not verified on hardware: remove it or verify it when a sensor that needs notifications is added.
- Windows 10 (nothing was ever tried there): the solid backdrop path, the title bar, toasts, Do Not Disturb.

### Known, not fixed

- `HistoryViewModel` re-reads stored readings several times per refresh and refreshes while hidden (a Year range is heavy); its refresh runs on the sender's thread and its `CancellationTokenSource` field is unguarded.
- `DevicesSettingsViewModel` keeps its `listening` flag after cancel (only matters at shutdown) and a room switch rebuilds the rows while a name is being edited.
- The Settings scrollbar is the light WPF default in the dark theme; the Settings page was not compared with the prototype's Settings view; Live in the dark theme was not looked at after the last styling pass.
- Stats card: time of the highest and lowest reading needs timestamps in `Bucket`; "· N partial" is missing in the coverage box.
- The header ring is not grey in the offline state; the main window has no minimum width (snap zones want 500 px or less).
- Per-feature gaps are listed at the end of the Q entries above ("Not built").

## Open questions for the owner (`docs/plans/questions.md` "Open")

Q7 (read the date order from the export header; flag the repeated autumn hour), Q17 (placeholder strings and the native Ukrainian review), Q27 (what the user sees when history cannot be read), Q30 (footer signal limits are an assumption), Q32 (a connected Aranet4 reports no signal strength: pass on the advertisement signal?), Q33 (older firmware uses another service id; not supported), and the product questions at the end of that file (Store listing, recommended interval, trend values).

## Gotchas found in earlier sessions

- **Services must not resume on the UI thread.** Background loops start with `Task.Run` (see `SensorSession`). `SensorSession` implements `IDisposable` and `IAsyncDisposable`.
- **A style kept under a key inside a control dictionary cannot be found at run time** from a template (`StaticResource`/`DynamicResource`). Embed styles in the control's own style. In `Generic.xaml` templates, `BasedOn="{StaticResource Text.Secondary}"` also fails (BaseStyles are not visible there); use a plain style with setters, or `DynamicResource` tokens. A `Style` set on an element still gets the control's default template underneath, and `Style=` plus a nested `.Style` on the same element is an error.
- **Detached content loses its inherited `DataContext`.** The render harnesses set `content.DataContext` before removing the content from the window.
- **Chart drawing:** a `Border` does not clip children to its rounded corners (clip by hand, see `ZoneShareBar`); neighbouring chart blocks leave seams unless snapped to device pixels. WPF's `TextBlock` line height is about 1.33 times the font size.
- **The radio monitor is asynchronous:** `IBluetoothRadio.IsOn` is false for a moment after start. Anything that checks it once at start-up must also react to `StateChanged`.
- **The sensor only sends an index, not timestamps.** Times are reconstructed from the age of the newest reading and snapped with `SlotMerger.Snap`. The phone export's own timestamps drift by up to a minute; compare by row, not by nearest time.
- **Timing tests with `FakeClock`:** timers fire only on `Advance`. Before advancing, wait until the loop has created its timer: `await Wait.Until(() => clock.PendingTimers >= 1)`. Lists that a background thread fills while a test reads them must be `ThreadSafeLog<T>`. `ListProgress<T>` records progress synchronously.
- **`StrongReferenceMessenger` allows one handler per message type per recipient**; registering twice throws in the constructor. Messages are sent from whatever thread the sender runs on; view models post to the UI through `IUiDispatcher`.
- **Do not use `async void`**; start a `Task` and log failures. Do not leave a bare `cat` or other stdin-reading command in a shell call (it hangs).
- **Shell heredocs:** long or multi-file heredocs fail to parse in this environment. Create files with the editor tools; keep shell edits short. When you splice text into a file with `sed`/`perl`, check that it applied (a silent no-match happened several times; CRLF files need `\r?\n`). PowerShell 5 `Set-Content -Encoding utf8` writes a BOM and `dotnet format` then fails with CHARSET.
- **Analyzers that fail the build:** CA1001, CA1816, CA1826, CA1834, CA1822, CA1859, CA1873 (guard logging with `IsEnabled`), xUnit1031 (no `.GetAwaiter().GetResult()` in tests), xUnit1051/xUnit1069 (use `TestContext.Current.CancellationToken`), IDE1006 (private static fields in camelCase, constants in PascalCase), sorted usings. Run `dotnet format Deskair.sln` (without `--verify-no-changes`) to fix whitespace and usings, then build.
- **Build locks:** if a build fails with "file locked by .NET Host", run `dotnet build-server shutdown` and rebuild. A stuck app can be stopped with `Stop-Process`.
- **Never run `rm -rf` with a glob after a `cd`;** the safety check blocks it. Deleting `%TEMP%\Deskair-*` folders is fine.
- **Strings:** every English key needs a Ukrainian key (a test checks it); numbers use the selected culture; unit symbols and "CO₂", "Aranet4", "Deskair" are not translated. **Tokens:** edit the theme XAML directly and keep light and dark keys identical (a test checks it).
- **One unreproduced failure** once appeared in `AppServicesStarterTests` cleanup (`session.DisposeAsync`); it never recurred in many later runs. If it comes back, capture the exception text.

## Where to look

| Need | File |
|---|---|
| Behaviour, thresholds, merge, trend | `docs/product/behaviour-rules.md`, `docs/engineering/data-model.md` |
| BLE protocol as seen on the sensor | `docs/plans/spike-s1-ble-history.md`, `Aranet4Protocol.cs`, `tests/Deskair.Tests/TestData/aranet4-history-capture.txt` |
| Colours and sizes | `docs/design/design-tokens.md`, then `design/prototype/index.html` |
| Adding a control, string, setting, sensor | `docs/guides/` and the skills in `.claude/skills/` |
| Why something was decided | `docs/decisions.md` (latest: D-0016) and the decided entries in `docs/plans/questions.md` |
| Dependency rules | `docs/engineering/architecture.md` section 3, `tests/Deskair.Tests/Architecture` |
| Startup and wiring | `src/Deskair.App/Composition/` (`AppServicesStarter`, `StartupSequence`, `ServiceRegistration`, `SettingsApplier`, `AppOptions`) |
| Test helpers | `tests/Deskair.Tests/Fakes/` (`FakeClock`, `FakeAranet4Connection`, `FakeBleScanner`, `Aranet4TestData`, `ThreadSafeLog`, `ListProgress`, `Wait`) |
