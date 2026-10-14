# Backlog

*Last reviewed: 2026-10-14*

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
| POL-03 | Shell chrome (text nav, plain room chip, footer) | todo |
| POL-04 | Live chart (fitted axis, threshold line, area fill, smoothing) | done |
| POL-05 | Sparklines (minimum range, area fill) | todo |
| POL-06 | Metric selection by hero and mini cards; tabs removed | done |
| POL-07 | Flyout rework | todo |
| POL-08 | Spike: acrylic flyout (optional) | todo |
| POL-09 | History period model and ‹ › navigator | done |
| POL-10 | History toolbar: metric and range only | todo |
| POL-11 | HourGridChart for Week and Month | done |
| POL-12 | Year as a months × days grid | todo |
| POL-13 | Day chart with typical line and aired markers | todo |
| POL-14 | History summary card (stats and sleep merged) | todo |
| POL-15 | Settings layout | todo |
| POL-16 | Motion | todo |
| POL-17 | Final review against the concept | todo |
