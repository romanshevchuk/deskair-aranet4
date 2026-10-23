# Backlog: finished items

*Last reviewed: 2026-10-23*

Archive. Everything here is done; nothing to pick up. Open work is in [`backlog.md`](backlog.md).

| ID | Item | Status |
|---|---|---|
| ARCH-01 | Solution skeleton, repo files, DI composition root | M1 | done |
| ARCH-02 | Dependency-rules tests | M1 | done |
| ARCH-03 | Control gallery (`--gallery` option, all builds) | M1 | done |
| ARCH-04 | Simulated sensor module | M1 | done |
| DATA-01 | Monthly CSV readings store | M1 | done |
| DATA-02 | Sensor history sync | M0/M1 | done |
| DATA-03 | Merge and dedupe by interval slot | M1 | done |
| DATA-04 | Import from phone app (.xlsx) with preview | M5 | done |
| DATA-05 | Export CSV | M5 | done |
| I18N-01 | Localization infrastructure (resx, runtime switch, plurals) | M1 | done |
| THEME-01 | Light, dark, system themes and tokens | M1 | done |
| LIVE-01 | Hero card with zones | M2 | done |
| LIVE-02 | Mini cards and metric tabs | M2 | done |
| LIVE-03 | Live chart, ranges, hover | M2 | done |
| LIVE-04 | 24 h ribbon | M2 | done |
| TRAY-01 | Tray icon states | M3 | done |
| TRAY-02 | Flyout | M3 | done |
| TRAY-03 | Tray menu | M3 | done |
| ALERT-01 | Toasts and alert rules | M3 | done |
| ALERT-02 | "Alerts paused by Windows" indicator | M3 | done |
| HIST-01 | History charts (line and bars) | M4 | done |
| HIST-02 | Hours filter: All day, Night | M4 | done |
| HIST-03 | Group by | M4 | done |
| HIST-04 | Gap, partial and coverage states | M4 | done |
| HIST-05 | Typical-day chart | M4 | done |
| HIST-06 | Stats card | M4 | done |
| SET-01 | Settings screen | M5 | done |
| FIRST-01 | First-run onboarding with import | M5 | done |
| STATE-01 | Stale, low battery, not connected | M5 | done |
| POL-00 | Design docs updated to the new direction; decisions recorded | done |
| POL-01 | Surfaces and accent (zone glow, no zone borders, ink accent, gauge knob, flyout corners) | done |
| POL-02 | Numbers and units (ppm without separator, proportional hero figures, percent and time formats) | done |
| POL-03 | Shell chrome (text nav, plain room chip, footer) | done |
| POL-04 | Live chart (fitted axis, threshold line, area fill, smoothing) | done |
| POL-05 | Sparklines (minimum range, area fill) | done |
| POL-06 | Metric selection by hero and mini cards; tabs removed | done |
| POL-07 | Flyout rework (Q48 follow-up done: Bluetooth step) | done |
| POL-09 | History period model and ‹ › navigator | done |
| POL-10 | History toolbar: metric and range only | done |
| POL-11 | HourGridChart for Week and Month | done |
| POL-12 | Year as a months × days grid | done |
| POL-13 | Day chart with typical line and aired markers | done |
| POL-14 | History summary card (stats and sleep merged; Q49 follow-up done: Nights section) | done |
| POL-15 | Settings layout | done |
| POL-16 | Motion (sliding selection, hero fade, gauge glide, High dot pulse, flyout fade; all off with Windows animations) | done |
| POL-17 | Final review against the concept (done 2026-10-17; findings are POL-19 to POL-31) | done |
| POL-18 | History per metric: comfort grids for temperature and humidity, a weather line for pressure; no CO₂ typical day or sleep on other tabs | done |
| POL-19 | Review fix: Not connected turns the header logo ring and the room chip dot grey | done |
| POL-20 | Review fix: charts expose a Narrator peer (`ChartAutomationPeer`); names on the Live chart, both ribbons and the History line charts; the 1h / 6h / 24h selector has its own name; the hero zone text is a polite live region. Sparklines stay hidden (the card has the name) | done |
| POL-21 | Review fix: High Contrast: `HighContrastPalette` laid over the tokens by `ThemeService` while Windows High Contrast is on (surfaces, text and outlines in system colours; zones, metrics and heat steps as stepped opacities); render test and palette tests; documented in accessibility.md | done |
| POL-22 | Review decision: `heat.comfort` is a slightly stronger grey (light #D5DBE0, dark #3A444E, about 1.4:1 and 1.8:1) and joins the contrast exception in design-tokens.md | done |
| POL-23 | Review fix: Year summary header names both years ("1 Nov 2025 – 12 Oct 2026") | done |
| POL-24 | Review fix: Live ribbon labels as "00:00 / 06:00 / 12:00 / 18:00"; Live chart title "CO₂ · last hour" | done |
| POL-25 | Review: render coverage added (Live Good and High in both themes, dark Not connected, Stale and Low battery, flyout Bluetooth-off, Good, High and dark offline, Temperature and Humidity with values across every comfort step, full-height Settings); tests run one at a time; stale renders deleted; the flyout fixture says "updated 2 min ago" when connected. Found by it: the Settings sleep hint still named the Night filter (fixed) | done |
| POL-26 | Review fix: thin scrollbar style in both themes (`BaseStyles.xaml`; documented in design-tokens.md) | done |
| POL-27 | Review decision: the Pressure tip is the documented line only; the extra sentence is gone | done |
| POL-28 | Review decision: "Open a window for about 10 minutes" stays; D-0015 and the behaviour rule now say they forbid recovery-time estimates, not a suggested duration | done |
| POL-29 | Review decision: every metric draws its History Day line as a monotone curve (done; the stairs on Humidity and Pressure remain, see POL-32) | done |
| POL-30 | Review, small: Day summary title is now "Last night · 22:00 – 07:00"; first-run hint says "header"; flyout "ppm" nudged to the baseline (`Space.Flyout.Unit`). No change needed: Ukrainian never uses the `_Other` plural form; the 2 px card bottom padding follows the token. The type scale moved to POL-33 | done |
| POL-31 | Review: docs housekeeping in screens-and-states.md (stale POL-11 paragraph, Year row outside its table) | done |
| POL-32 | Review decision: keep the raw readings on the Day lines of Temperature, Humidity and Pressure (steps from coarse sensor values are the data); noted in screens-and-states.md | done |
| POL-33 | Review fix: the type scale is applied (hero 104, History average 48, stat 20, hero title 14, mini label 13, welcome 20, period title 16); a test keeps every `Font.Size.*` on the scale; the hero stats share their width by need (`StatTriplePanel.Allocate`); the summary card blocks sit 9 px apart so the tallest card still fits | done |
| POL-39 | Removed after the polish: the range-bar chart (HistoryChart bar mode, six bar layers, `ChartBar`, weekend and average lines), the Typical-day card with its chart, view model, strings and tokens, the unused bar-era header strings, six unused spacing tokens; HistoryChart is now a line-only chart | done |
| POL-40 | Settings code review findings: `ApplyLater`/`ApplyQuietlyAsync` is copied into five view models (a small base class would do); Appearance and Alerts save all their fields on any change (a change from the tray menu in the same moment can be overwritten; save only the field that changed); `Data` view has four near-identical hidden-row styles (a `Visibility` binding on the row does the same); `BoolToVisibility` is declared in every view | done |
| POL-41 | Settings UI polish ideas: segmented controls have three different widths, so their left edges are ragged (equal widths); Quiet hours "From" and "Until" as one row "22:00 to 07:00"; status lines under Data rows are full-height rows with a divider (make them small captions); the "invalid time" caption sits at the card edge, not under its field | done |
| POL-37 | Critical pass over Onboarding and the Bluetooth platform code: scan logic moved to `SensorSearch` (a replaced search cannot change the state; UI updates go through `IUiDispatcher`; dead `StepIndex` removed); the platform files were left as they are. Remaining minor points, see REL-02 | done |
| POL-34 | Critical pass over `ViewModels/History`: `LoadAsync` split into read (`HistoryLoad`) and show steps; `ChartHeaderViewModel` has three explicit methods (Day, grid, line), the bar-era branches, the zone legend and three strings are gone; the stats insight text moved to `StatsInsight` (`StatsViewModel` 483 to 402 lines, the rest is its card properties) | done |
| POL-35 | Critical pass over `ReadingQueryService` and `Domain/Analytics`: measured 2026-10-17 instead of rewritten. The store keeps an in-memory index, so the repeated range calls in one refresh are slices, not file reads; a Year refresh over one year of 2-minute readings (about 260,000, in-memory fake store) plus building the data took about 1 second for the whole test (two refreshes). No change made. Revisit only if a real Year refresh feels slow (then merge the per-range scans) | done |
| POL-38 | `HighContrastPalette`: a test now fails when a colour token is neither mapped nor listed as kept on purpose (Tray, Caption.CloseHover). The Gallery stays reachable with `--gallery` in every build by design (ARCH-03; it only shows the controls with sample data). `StatTriplePanel` left as it is | done |
| REL-03 | Stats card: highest and lowest reading with their time, and the partial note (already there when checked) | done |
| REL-04 | The redundant `BoolToVisibility` copies are gone; `IBleConnection.SubscribeAsync` and the notification code behind it were removed (unused, never verified on hardware) | done |
| OWN-01 | Quiet hours and night hours typed by hand, checked by the owner 2026-10-17 (quiet hours hold the toast; night band follows) | done |
| OWN-02 | Toast buttons (Open, Snooze, a press with the app not running) and Do Not Disturb with the "paused by Windows" row, checked by the owner 2026-10-17 (`--test-toast` added for it) | done |
| OWN-04 | Flyout and tray at 125 % and 150 % checked by the owner 2026-10-17. Second monitor and other taskbar edges closed without a manual test: `FlyoutPositionerTests` cover all four taskbar edges, corners, a work area not at zero and the overflow popup | done |
| OWN-03 | Tray: left click, click-away, Esc, the Snooze menu, the value on the tray icon, and the flyout no longer blinks (a new flyout window for each opening), checked by the owner 2026-10-17 | done |
| OWN-07 | Open folder, Export, Choose file and Import checked by the owner 2026-10-17; a real phone export of about 44,000 readings imported fast | done |
| OWN-06 | History on real data (Day, Week, Month, Night) looked at again after the Day rework, closed by the owner 2026-10-18 | done |
| POL-50 | Review feedback batch 1 (D-0022): neutral trend, "Good now, but rising" advice, "< 1 min", brand logo, neutral CO₂ dot, quiet ideal band. 2026-10-19 |
| POL-51 to POL-54 | Heatmaps (D-0023): six CO₂ steps kept; best and worst only on complete rows; caption over the row-end bars; no "best" for Temperature and Humidity. 2026-10-19 |
| ALERT-01 | Toast repeats (D-0025): an episode ends after 10 minutes of Good; pressing a toast acknowledges it. 2026-10-19 |
| TIDY-01 | Dead code removed (`HourAxisLayer`, `HourBar`, `TimeSlot`, `ZoneChanged`, `HasReadingsAsync`); only English and Ukrainian framework resources in the package (`.msix` 69.2 → 65.8 MB). See `size-and-startup.md` section 0. 2026-10-22 |
| PERF-01 | The reading files are loaded off the UI thread: `MonthlyCsvStore` reads and indexes them in `Task.Run` (nothing is cached when the load is cancelled) and logs `Loaded N readings for ADDRESS in M ms.` once per device (789,120 readings: 760 ms, measured 2026-10-23). Owner check: the window stays smooth while the readings load (`size-and-startup.md` section 4). 2026-10-23 |
| PERF-04 | No second sort of readings the store already returns oldest first: `IReadingStore.ReadRangeAsync` documents the order, `ReadingQueryService.GetRangeAsync`/`GetRecentAsync` and `ExportService` no longer sort again; `FakeReadingStore` returns sorted readings; new test `Reading_ranges_come_back_oldest_first`. Open question Q53 (the fake includes a reading at `end`). Plan: `size-and-startup.md` section 7. 2026-10-23 |
