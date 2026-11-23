# Handoff (updated 2026-11-14)

*Last reviewed: 2026-11-14*

For the next agent. Repo: `c:\Personal\Dev\Projects\Deskair` (use absolute paths there). Ignore the other project directory (`AranetHomeDesktop`) unless the owner points you to it, and do not read `../deskair-legacy`.

## Session of 2026-11-23 (read this first)

One pull request from branch `feature/ux-settings-header-changelog` holds all of this session's work, one commit per item. It is **not merged** yet; the owner reviews it. Build has 0 warnings, the format check is clean, **1860 tests pass** (85 skipped: the opt-in render tests).

| Item | Result | Where |
|---|---|---|
| UX-30 | "Notify at Elevated (1000 ppm)" / "Notify at High (1400 ppm)"; the two rows are indented under Alerts; they and quiet hours are disabled while Alerts is off; a disabled `SettingsRow` dims its labels | `AlertsSettingsView`, `SettingsRow`, tokens `Space.SettingsRow.Indent`, `Opacity.SettingsRow.Disabled`; test `AlertsSettingsViewTests` |
| UX-34 | Settings order: Alerts, Devices, Appearance and units, Sleep, Data and storage, Start-up, About | `SettingsView`, screens-and-states.md "Screens" |
| UX-22 | Temperature and Humidity rows are tagged "least comfortable" (CO₂ keeps "worst") | `HourGridBuilder.WorstTag`, D-0023 amended |
| UX-39 | The header room button has no dot. It was the CO₂ zone, but the owner read it as "connected" (the footer dot's meaning). The room menu keeps a dot beside each value | D-0034; `RoomChip.Zone` and `ShellViewModel.HeaderZone` removed |
| Releases | `CHANGELOG.md` at the root: how to publish a Store update, a table of releases, and "Unreleased" with the "What's new" text to paste (under 1500 characters) and the backlog IDs. `AGENTS.md` rule 4 and the PR template ask for a line there with every change a user can notice | `CHANGELOG.md`, `release-and-distribution.md` |

**Verified:** unit and view tests; renders of Settings (light, English), History Humidity Month (the new tag fits) and Live (header without the dot). **Not verified:** the running app; the disabled Alerts rows in a render (only the test); Ukrainian renders. "найменш комфортний" is a draft for native review.

**Released so far:** only 0.1.3 (package `0.1.3.0`, commit `1e64e94`, 2026-10-20). Everything after it, including FIX-01 to FIX-05 and the window place fixes, is unreleased and listed in `CHANGELOG.md` as planned 0.1.4. To publish, follow "How to publish an update" there; the older "For an update" note below is replaced by it.

**What to do next**
1. After the owner merges the PR: release 0.1.4 when the owner asks (CHANGELOG steps), then record it there.
2. Wave 1 leftovers: validate UX-05 and UX-07.
3. Owner decisions Q55 to Q62; Q59 (keep the 1000 ppm line on the Live 1h axis, UX-09) is the best value for its cost.
4. Other `ready` items that are cheap: UX-19 (lighter night band), UX-24 (lighter empty Year cells), UX-25 (small labels), UX-27 ("45 % all hour").

**How the owner worked this session:** asked for low-effort, high-value items; questioned UX-39's proposal and wanted a reasoned alternative (the answer: the footer owns the connection, remove the header dot); asked for one PR for all the changes and the handoff kept current.

## Session of 2026-11-14 (older)

The owner did a UI/UX review of the released app (0.1.3 plus the 0.1.4 patch work): [`ux-review-2026-11.md`](ux-review-2026-11.md), items UX-01 to UX-45 in six waves, with backlog rows UX-W1 to UX-W6 in [`backlog.md`](backlog.md). That file says how to work: validate, then decide, then build, then close, and it holds a status per item and a "Validation notes" table. Keep both up to date.

State: everything is merged and **pushed to `origin/main`** (head `66bcf15`); the work branches are deleted. Build has 0 warnings, the format check is clean, **1773 tests pass** (85 skipped: the opt-in render tests). No release was built.

| Item | Result | Where |
|---|---|---|
| UX-01 | Day tips start with "Usually" and name "previous 7 days" (`Insight_*_Day`, 12 keys per language); Q54 answered B | `StatsInsight.ForRange`, D-0033, behaviour-rules.md rule 9 |
| UX-02 | Dropped: the CO₂ axis labels are even (every 400 ppm above a 2000 top); 1000 and 1400 are limit lines drawn apart on purpose | screens-and-states.md "Two cards" wording corrected |
| UX-03 | Typical-day windows may cross midnight for every metric (rule 11 changed); the grid outlines both parts | `HourGridModel.WorstSpans`, `HourGridLayer.DrawTypical` |
| UX-04 | The comfort pill judges the average as shown (50.4 % shows "50 %" and is comfortable), in the unit shown | `MetricFormatter.Shown`, `TemperatureUnit.ToCelsius`, rule 10 |
| UX-06 | Advice strings say "1000 ppm" and "1400 ppm" (D-0020 kept, rechecked); a test fails on any grouped ppm value | `LocalizerTests` |
| UX-08 | "Deep sleep avg" is now "Middle of the night avg (02:00–05:00)" | `Sleep_Deep` |

**Not verified:** none of this was seen in the running app or in fresh renders; it is covered by unit tests only. A render pass (History Day tip, Week grid with a window across midnight, Humidity Month pill) is worth doing before the next release. The new Ukrainian strings are drafts with a `<comment>`.

**Known, left for now:** the comfort tip (`TypicalDayCalculator.FurthestOutsideWindow`) still judges the raw window mean, so a window shown exactly on a limit could still be named (rare). In °F the pill can differ from °C for the same air (24.04 °C: "Comfortable" in °C, "75.3 °F" is "A bit warm"); this follows "the pill matches the number shown".

**What to do next**
1. Wave 1 leftovers: validate UX-05 (tip colours) and UX-07 (durations: "7.9 h of 9 h" follows `Sleep_HoursOf` as specified, so it is a spec change; the Ukrainian "2 h 05" was not found in the strings).
2. Owner decisions Q55 to Q62 in `questions.md` (each has a recommendation). Then items marked `ready` in waves 2 to 4 can be built.
3. When Wave 1 is fully done, move its backlog row to `backlog-done.md`.

**How the owner worked this session**
- When a finding says "the code breaks rule N", first ask whether the rule itself is still right for users, and say so; some rules are workarounds (rule 11 was one). The owner wants the best overall decision, not compliance with the doc.
- Explain technical findings in plain words; the owner asked for UX-02 again in simpler terms.
- "Split and commit", "merge if safe", "push" were each explicit. One branch per item, stacked when they touch the same review file; fast-forward merge after build, format and tests; no Co-Authored-By trailer.
- Owner screenshots of real data are in `artifacts/owner-screenshots/` (not in git); use them to validate.

## Session of 2026-10-20 (older)

**Deskair 0.1.3.0 is published in the Microsoft Store** (product ID `9N027HRZXSMH`, https://apps.microsoft.com/detail/9N027HRZXSMH; submitted and certified the same day). Local `main` is ahead of `origin/main` until the owner pushes; CI is on GitHub and passed.

| Done | Where |
|---|---|
| Start with Windows starts in the tray (D-0027): the Run key adds `--background`; the packaged startup task is detected through `AppInstance.GetActivatedEventArgs`; owner checked it on a packaged test build after a restart | `AppOptions`, `StartupSequence`, `PackagedStartupActivation`, `Program` |
| Stat dividers on the hero card were drawn from stale column widths; `StatTriplePanel` now repaints when columns move | `StatTriplePanel`, `StatTriplePanelTests` |
| History grid end column is 160 px and grows only when a best or worst tag is wider (Ukrainian "найкращий") | `HourGridLayout.EndWidth`, `HourGridChart.TagWidth`, `design-tokens.md` |
| Store listing (English only), Partner Center pages filled, tester notes written | `store-listing.md`; the Ukrainian listing was removed on purpose until its text is reviewed by a native speaker |
| Store screenshots: Live good, high and dark, flyout (a real capture), History week and day, Temperature; made from `RealisticRoom` (days that differ, window dips) by the `...ForTheStore` render tests | `artifacts/store/en` (not in git); six copies in `docs/screenshots/` for the README |
| README rewritten with screenshots, requirements, build, troubleshooting and acknowledgements; CI workflow (`.github/workflows/ci.yml`: build, format check, tests) | `README.md`, `.github/` |

**Facts to know**
- The Store build and the test package share one identity, so a PC that has the **signed test package** installed shows "Installed version 0.1.2.0" in the Store and does not update; uninstall the test package, then install from the Store.
- Package settings: category Utilities and tools, price 0, age rating IARC 3+, only the English listing, `runFullTrust` justified in Submission options, Additional Testing Information says the sensor is separate hardware.
- `build-msix.ps1` wipes `artifacts\msix` (the old test package and `DeskairTest.cer` go too); the test certificate is still trusted on the owner's PC.
- The legacy repo `romanshevchuk/aranet4-monitor` (local copy `C:\Personal\Dev\Projects\AranetHomeDesktop`) is superseded; the owner plans to delete or archive it. Its `THIRD-PARTY-NOTICES.md` credits Aranet4-Python; our README now has an Acknowledgements section.

**What to do next**
1. Watch for Store reviews and GitHub issues; fix what real users hit first.
2. Ideas from the older list still stand: a message that names Smart Home integration when the read fails with `ProtocolError`; Windows 10 check (OWN-08); native Ukrainian review, then a Ukrainian Store listing; a "Try demo data" button if certification ever complains that the app cannot be tested without a sensor.
3. For an update: bump the version, `packaging\build-msix.ps1 -Version <higher, last number 0>`, upload in Partner Center with "Start update".

## Session of 2026-10-19 (older)

State: `main` is pushed and clean (head `d1ce3a6`). Release build and format are clean; **1732 tests, 0 failed** (69 skipped: the opt-in render tests). Work happened on a branch that was merged into `main` and deleted (local and remote); no Claude or Co-Authored-By trailer (the owner asked again; see the memory note).

**What changed (decisions in `docs/decisions.md`):**

| ID | What |
|---|---|
| D-0022 | One colour, one meaning: the trend text is neutral; "Good now, but rising" advice at 800 ppm or more; "< 1 min" instead of "0 min"; the header logo is brand ink (only the tray icon follows the zone); the CO₂ dot is neutral slate; the Temperature and Humidity ideal band is the Good green at 9 % with dashed limits |
| D-0023 | History grid: the six CO₂ steps stay (no three-zone grid, no "detailed zones" setting); best and worst only on complete rows (not today, not the month in progress); a caption over the row-end bars ("Time in zones", "Time in range"); no "best" for Temperature and Humidity |
| D-0024 | Comfortable is mid green (light `#B4E0C9`, dark `#244F3F`), not grey |
| D-0025 | An alert episode ends only after 10 minutes of Good; pressing a toast acknowledges the episode (only the escalation to High still sends) |
| D-0026 | High alerts wait 2 minutes (Elevated 5); the sensor needs "Smart Home integration" on in the Aranet Home app (with it off the current-values read fails with `ProtocolError`); the first-run and offline steps and the listing say so |

Also: the typical-window label wrapped past midnight ("23:00–26:00", now "23:00–02:00"); Settings has an About card ("unofficial"); the first-run text names the Aranet4; render tests use an `aranet4` device so the footer shows "Aranet4"; the Ukrainian strings were reviewed (6 still carry the "Draft" flag, check `Strings.uk.resx`).

**Store (REL-01), read [`store-packaging.md`](store-packaging.md) sections 9 and 10, and [`store-listing.md`](store-listing.md):**

- Done: phases 0 to 2; `PRIVACY.md` is on `main` and its URL works; the GitHub description and topics are set; listing texts (English; Ukrainian reviewed by the owner); screenshots in `artifacts/store/en` and `artifacts/store/uk` (not in git; re-render with `$env:DESKAIR_RENDER=1`); a test package `artifacts\msix\Deskair_0.1.1.0_x64.msix` signed with the test certificate (already trusted on the owner's PC).
- Owner checks on the installed package: 1, 2, 3 (Snooze and Open), 4, 5, 6, 8, 10 passed (2026-10-19). **Pending: 7 (start with Windows after sign-in), 9 (sleep and wake), 11 (uninstall keeps `Documents\Deskair`).**
- Decisions taken: category Utilities and tools; `Aranet4` is not a search term (policy 10.1.3; it is in the description); no courtesy email.
- Not done: a full-screen real flyout screenshot (the owner will take one with the desktop icons hidden), the Partner Center clicks (listing, age rating, upload, submit), and the clean upload package.
- Note: the test package `0.1.1.0` was built before D-0026; rebuild for any further test.

**What to do next, in order:**

1. When the owner reports checks 7, 9 and 11: record them in `store-packaging.md` (section 10) and `backlog.md`. Fix anything that failed.
2. REL-05 final pass: build, tests, format, renders in light and dark (English); then build the **upload package** with `packaging\build-msix.ps1 -Version <higher than 0.1.1.0, last number 0>` from the final commit. Do not sign it with the test certificate for upload (unverified whether Store accepts it; Microsoft signs Store packages). Tell the owner the path.
3. If the owner sends the flyout screenshot, check it is at least 1366 x 768 and crop only if asked.
4. Ideas not done, in rough order of value (none blocks release): an in-app message that names the cause when the sensor read fails with `ProtocolError` (Smart Home integration off); the amber line outside the ideal range on Temperature and Humidity charts and a Y axis that always includes the ideal range (D-0022 left out); the floating grid tooltip (POL-60); the calendar Month view (POL-55; plan it first, mock "one colour per day" against "four daypart stripes"); alert state is in memory, so a restart sends a new toast (D-0025 note); an unexplained moment where the tray showed "not connected" while the footer said "Connected" (same state drives both; not reproduced).

How the owner worked this session: asks for an opinion and wants a plain recommendation; "do it locally, no commit" and "commit" are explicit, so do not commit or push without being asked; wants simple step-by-step instructions for manual checks; wants honest limits ("not verified").

Everything below this section is older; where it differs (test counts, "latest decision D-0016"), this section wins.

## Rules you must follow

- Read `AGENTS.md` and this file first.
- **Commits:** the owner asks for commits and pushes to `main` in small steps. Commit under the owner's git identity and add NO `Co-Authored-By` or Claude footer (the owner objected to it). Make no changes under `.claude/`.
- **Plan first:** for any batch of work, list it and wait for approval before editing. "Go with your recommendations" or "continue" counts as approval for what you listed.
- **Ask before adding a NuGet package or project reference.** Approved and in use: ExcelDataReader, `H.NotifyIcon.Wpf`. Toasts use the inbox Windows API; the Windows App SDK is the fallback and needs a new question first (Q8).
- **Subagents:** only when the owner asks for them (an independent code review through the `code-review` skill found real bugs on 2026-10-13).
- For each change: write or unskip the test, see it fail (for a pixel or visual change, break the code once to prove the test can fail), implement, run the three checks, update the docs that describe the behaviour.
- If the docs are silent, add a question to `docs/plans/questions.md` and move on. Do not guess.
- Do not mention vendor names outside `Deskair.Sensors` and `App/Composition`. Do not use `DateTime.Now`. UI strings and colours come from resources and tokens.
- **The owner is not a native Ukrainian speaker.** Every new string gets an English and a Ukrainian value; mark the Ukrainian `<comment>Draft: needs native review.</comment>` and add product strings to `docs/design/localization.md`.
- The owner wants to be told plainly what is verified and what is not (for example "tested with a fake, not on the real sensor"). Say so.

## Checks (all must pass before work is done)

```
dotnet build Deskair.sln
dotnet test tests/Deskair.Tests
dotnet format Deskair.sln --verify-no-changes
```

State now: see "Session of 2026-10-17" below (build clean, 1676 tests passing). The paragraph that stood here described 2026-10-14 and is out of date.

## How to see what you built (important)

You cannot see the UI, so use these:

| What | How |
|---|---|
| Gallery of every control, light and dark, English and Ukrainian, as PNG | `$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~GalleryRenderTests"` writes `artifacts/visual/gallery-*.png`. The gallery is long (about 6000 px high); crop the part you need with System.Drawing in PowerShell |
| Whole screens with real view models | same switch with `--filter "FullyQualifiedName~RenderTests"` (or one of `MainWindowRenderTests` (Live), `OnboardingRenderTests`, `SettingsRenderTests`, `HistoryRenderTests`, the flyout and tray icon tests). They write `live-*.png`, `firstrun-*.png`, `settings-*.png`, `history-*.png`, `flyout-*.png`, `tray-icons.png` |
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

The per-feature rules and what was and was not verified are in the decided Q entries of `docs/plans/questions.md` (tray Q37, flyout Q38, single instance Q39, resume Q40, toasts Q41, snooze Q42, Do Not Disturb Q43, reconnect Q44, try again Q45, sleep Q46, prototype alignment Q47, title bar Q14, offline Q31, stats Q36). The spike notes were removed after release (they are in git history). The Aranet4 history download returns 5040 readings every 120 s (a real capture is `tests/Deskair.Tests/TestData/aranet4-history-capture.txt`).

### Session of 2026-10-17 (older)

**Packaging (REL-01):** read [`store-packaging.md`](store-packaging.md) first, section 9 has the status; the research is done, do not repeat it.

**Start with [`backlog.md`](backlog.md).** It was reorganised: only open work is there, in four lists (R1 owner checks, R2 owner decisions, R3 agent code work, R4 release). Finished items are in [`backlog-done.md`](backlog-done.md) (an archive; read it only for history).

State: everything is committed on `main` (the owner asks for commits; no Claude or Co-Authored-By trailer). Release build and format are clean; **1676 tests, 0 failed** (66 skipped: the opt-in render tests). The owner has since looked at Settings, the tray, toasts, History Day and Week and Month, the import and the export in the real app, all fine.

What changed that you should know about:

- **Settings:** `SettingsSectionViewModel` is the base for the sections that save at once (a change saves only its own field). Equal-width segmented controls, quiet hours on one row, status lines as captions. `BoolToVisibility` is declared once in `App.xaml`; the test host (`UiHost.cs`) registers it too, because it does not load `App.xaml`.
- **Onboarding:** the scan logic is `SensorSearch` (a replaced search cannot change the state); the view model only shows its state on the UI thread through `IUiDispatcher`.
- **History:** `HistoryViewModel` refreshes through the dispatcher and `LoadAsync` is split into `ReadAsync` (into a `HistoryLoad`), `ShowSummary` and `ShowChart`. `ChartHeaderViewModel` has three methods (`UpdateForDay`, `UpdateForGrid`, `UpdateForLine`). `StatsInsight` holds the insight text. The right column is two cards (the period; the night) and the worst-hours tip sits under the chart. The CO₂ line colours blend over 30 ppm at each limit (`ChartLines.ZoneBlend`); the typical line is thin and light; "air improved" is a ring plus a legend entry.
- **Window:** minimum size 1100 × 700 (tokens `Size.Window.MinWidth` and `MinHeight`), chosen by rendering Live and History at smaller sizes.
- **Flyout:** a new `FlyoutWindow` for each opening (reusing one made a transparent window blink on re-show).
- **Removed:** `IBleConnection.SubscribeAsync` and the notification code (unused, never verified), the bar-era chart header branches and strings.
- **New dev switch:** `--test-toast` (use with `--simulated`) shows one High CO₂ toast at start, to try Snooze (the button) and Open (pressing the toast body).
- **Sleep verdict pill colours:** Good green, Fair neutral, Poor amber (red is kept for the air being High now).
- **Measured, not changed:** a Year refresh over a year of 2-minute readings is fast (POL-35), so the query layer was left alone.

What is left for you, in order (details in `backlog.md`):

1. **Agent work:** REL-01 (Store packaging: plan first; the listing needs the owner's P1 answers), REL-02 (two small items; one needs a real sensor), REL-05 (final pass before a tag).
2. **Owner work, not yours:** OWN-05 (only the sensor out of range is left; sleep and wake passed 2026-10-18), OWN-08 (Windows 10), A11Y-01 (only Narrator on the History period buttons is left; the flyout passed too; keyboard, scaling, High Contrast, Narrator on Live and Settings passed 2026-10-18), the native Ukrainian review, Q27 and P1 to P3. OWN-06 (History) is closed. Do not mark these done.

How the owner likes to work (from this session): be efficient, make real progress toward the release, do not over-engineer; plan first when a change is large and wait for a yes, otherwise just do it; give a plain, honest opinion when asked about design; say what is verified and what is not; the owner is watching token use, so do not review or translate Ukrainian beyond what a change needs, and do not start subagents unless asked.

Tooling notes from this session:

- `dotnet test` can report zero tests; run the exe directly: `tests\Deskair.Tests\bin\Release\net10.0-windows10.0.19041.0\Deskair.Tests.exe` (add `-class "Deskair.Tests.Ui.SettingsRenderTests"` for one class). Build in Release (the running app can lock Debug output). Renders: set `DESKAIR_RENDER=1` and run the exe; images go to `artifacts\visual`.
- Python is available (it was used on 2026-11-14; an older note said it was not). In the shell tool, a heredoc or `sed` text with an apostrophe can break parsing; write files with the Write tool and keep `sed` edits short. Check every splice afterwards.
- `AGENTS.md` mentions `.github/instructions/csharp.instructions.md`; it does not exist. The C# style guide is `docs/engineering/csharp-instructions.md`.


### What is left

Everything left before release (owner checks, owner decisions, agent code work, release steps) is in [`backlog.md`](backlog.md). The old "Open, in the owner's hands" and "Known, not fixed" lists moved there (OWN-01 to OWN-08, REL-02 to REL-04). Do not keep a second list here.

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
| BLE protocol as seen on the sensor | `Aranet4Protocol.cs`, `tests/Deskair.Tests/TestData/aranet4-history-capture.txt` |
| Colours and sizes | `docs/design/design-tokens.md` |
| Adding a control, string, setting, sensor | `docs/guides/` and the skills in `.claude/skills/` |
| Why something was decided | `docs/decisions.md` (latest: D-0033) and the decided entries in `docs/plans/questions.md` |
| Dependency rules | `docs/engineering/architecture.md` section 3, `tests/Deskair.Tests/Architecture` |
| Startup and wiring | `src/Deskair.App/Composition/` (`AppServicesStarter`, `StartupSequence`, `ServiceRegistration`, `SettingsApplier`, `AppOptions`) |
| Test helpers | `tests/Deskair.Tests/Fakes/` (`FakeClock`, `FakeAranet4Connection`, `FakeBleScanner`, `Aranet4TestData`, `ThreadSafeLog`, `ListProgress`, `Wait`) |
