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
| A11Y-01 | Accessibility pass | M6 | todo |
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
| POL-19 | Review fix: Not connected turns the header logo ring and the room chip dot grey | todo |
| POL-20 | Review fix: accessible names on the Live chart, ribbons, sparklines and History lines; the 1h / 6h / 24h selector gets its own name; announce zone changes (live region) | todo |
| POL-21 | Review: High Contrast support (system colours; heat steps as stepped opacities; accessibility.md) | todo |
| POL-22 | Review: `heat.comfort` (and `heat.none.line`) too close to the surface in both themes, about 1.2:1; needs a colour decision, then tokens and the contrast exception in design-tokens.md | todo |
| POL-23 | Review fix: Year summary header names both years ("1 Nov 2025 – 12 Oct 2026") | todo |
| POL-24 | Review fix: Live ribbon labels as "00:00 / 06:00 / 12:00 / 18:00"; Live chart title "CO₂ · last hour" | todo |
| POL-25 | Review: render tests: stop the culture race between test classes (English renders came out in Ukrainian), add fixtures outside the comfort range, Live Good and High, dark states, flyout Bluetooth-off, Settings below the fold; delete the three stale Night-filter renders; stale fixture says "0 min", flyout says "25 min" next to a live reading | todo |
| POL-26 | Review: style the scrollbar (light and dark; Settings shows the native one) and add its tokens | todo |
| POL-27 | Review, owner decision: Pressure tip. Docs say one line ("Pressure follows the weather, not the room"); the concept page says no tip; the code adds "A fast fall often means rain on the way." | todo |
| POL-28 | Review, owner decision: advice "Open a window for about 10 minutes" against D-0015 "no time estimates" | todo |
| POL-29 | Review, owner decision: the CO₂ Day line is smoothed, the Temperature, Humidity and Pressure Day lines are stair-stepped | todo |
| POL-30 | Review, small: Day summary title "Sleep" vs "Last night"; first-run hint says "title bar"; Ukrainian lacks `_Other` plurals for 3 keys; flyout "ppm" baseline; type scale in design-tokens.md (15, 58, 32 px); Settings and first-run cards sit 2 px from the bottom edge | todo |
| POL-31 | Review: docs housekeeping in screens-and-states.md (stale POL-11 paragraph, Year row outside its table) | todo |
