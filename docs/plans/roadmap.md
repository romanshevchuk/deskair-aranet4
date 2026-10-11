# Roadmap

*Last reviewed: 2026-10*

Each milestone ends with a working, testable result.

| Milestone | Content | Exit criteria |
|---|---|---|
| M0 Spikes | BLE history sync; tray flyout positioning; title bar with Mica and snap layouts; icon rendering; toast setup (`spikes.md`) | Each spike answered with a short write-up |
| M1 Foundation | Solution structure (five projects) and dependency-rules tests (per `architecture.md`), control gallery, simulated sensor, theme dictionaries, localization infrastructure (EN, UK), data layer: monthly CSV, merge, in-memory aggregation | Unit tests pass; language switch works at runtime |
| M2 Live | Hero, mini cards, Live chart with ranges and hover, ribbon, footer | Matches the prototype in light and dark, EN and UK |
| M3 Tray and alerts | Icon states, flyout, menu, toasts, Do Not Disturb indicator | Works on Windows 10 and 11, multiple monitors |
| M4 History | Charts, filters, group by, gap states, stats card, typical day | All bucket states render; Night filter works |
| M5 Settings and states | Settings, first run with import, stale, low battery, not connected, export | Every state in `screens-and-states.md` reachable |
| M6 Quality and release | Accessibility, DPI, Windows 10 and 11, soak test, Ukrainian review, Store packaging | Store submission passes |

Later: airing feedback, sleep verdicts, drill-down, shareable report, all-rooms view, editable thresholds.
