# Backlog

*Last reviewed: 2026-10-17*

Status: `todo`, `doing`, `done`. Update this file in the same change as the work.

## Version 1

| ID | Item | Milestone | Status |
|---|---|---|---|
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
| I18N-02 | Ukrainian translation and native review | M6 | todo |
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
| A11Y-01 | Accessibility pass: tests for names, contrast and High Contrast are done; the manual check in accessibility.md (Narrator, keyboard, scaling) is the owner's part | doing |
| REL-01 | Store packaging; winget and GitHub later | M6 | todo |

## Later

| ID | Item | Status |
|---|---|---|
| AIR-01 | Airing feedback | later |
| SLEEP-01 | Sleep verdict card (done, SLEEP-01a) and morning toast (later) | partly done |
| HIST-07 | Click a bar to drill in | later |
| REPORT-01 | Shareable report | later |
| ROOMS-01 | All-rooms view | later |
| THRESH-01 | Editable CO₂ thresholds | later |
| POLISH-01 | Premium polish pass: now the Polish section below (`plans/polish-implementation.md`) | doing |
| WORK-01 | Work-hours filter (dropped from v1) | later |

## Polish

Plan, specs and model per item: [`polish-implementation.md`](polish-implementation.md). What and why: [`ui-polish-plan.md`](ui-polish-plan.md). Visual target: `design/polish/deskair-polish-concept.html`.

| ID | Item | Status |
|---|---|---|
| POL-00 | Design docs updated to the new direction; decisions recorded | done |
| POL-01 | Surfaces and accent (zone glow, no zone borders, ink accent, gauge knob, flyout corners) | done |
| POL-02 | Numbers and units (ppm without separator, proportional hero figures, percent and time formats) | done |
| POL-03 | Shell chrome (text nav, plain room chip, footer) | done |
| POL-04 | Live chart (fitted axis, threshold line, area fill, smoothing) | done |
| POL-05 | Sparklines (minimum range, area fill) | done |
| POL-06 | Metric selection by hero and mini cards; tabs removed | done |
| POL-07 | Flyout rework (Q48 follow-up done: Bluetooth step) | done |
| POL-08 | Spike: acrylic flyout (optional) | todo |
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
| POL-34 | Critical pass over `ViewModels/History` (`ChartHeaderViewModel.Update` still has non-Day branches that only the bars used; `HistoryViewModel.LoadAsync` is 129 lines; `StatsViewModel` 483 lines; metric branches added one at a time) | todo |
| POL-35 | Critical pass over `ReadingQueryService` and `Domain/Analytics` (HourGrid, Comfort, Pressure, Nights, AirImproved): overlapping queries, several scans per History refresh | todo |
| POL-36 | Tests: merge the four near-identical render-test `BuildShell` helpers; check the other polish-era tests for duplicates | todo |
| POL-37 | Critical pass over Onboarding (397 lines) and the Bluetooth platform code: older, large, most critical for real use | todo |
| POL-38 | Small: `StatTriplePanel` may be heavier than needed (a plain Grid could do); `HighContrastPalette` maps tokens by name pattern, so a new token is silently uncovered; check the Gallery cannot be opened in a release build (`StartupSequence`) | todo |
| POL-39 | Removed after the polish: the range-bar chart (HistoryChart bar mode, six bar layers, `ChartBar`, weekend and average lines), the Typical-day card with its chart, view model, strings and tokens, the unused bar-era header strings, six unused spacing tokens; HistoryChart is now a line-only chart | done |
| POL-40 | Settings code review findings: `ApplyLater`/`ApplyQuietlyAsync` is copied into five view models (a small base class would do); Appearance and Alerts save all their fields on any change (a change from the tray menu in the same moment can be overwritten; save only the field that changed); `Data` view has four near-identical hidden-row styles (a `Visibility` binding on the row does the same); `BoolToVisibility` is declared in every view | todo |
| POL-41 | Settings UI polish ideas: segmented controls have three different widths, so their left edges are ragged (equal widths); Quiet hours "From" and "Until" as one row "22:00 to 07:00"; status lines under Data rows are full-height rows with a divider (make them small captions); the "invalid time" caption sits at the card edge, not under its field | todo |
