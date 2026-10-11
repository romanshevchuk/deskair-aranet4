# Design reference

*Last reviewed: 2026-10*

`design/prototype/index.html` is an interactive prototype (light and dark, all states). Open it in a browser. Use the switches above the window to preview zones and conditions.

## Prototype shortcuts (do not copy)

- Data is simulated.
- History day buckets are rolling 24 h windows. Implement calendar days in local time.
- The window gradient only imitates Mica.
- The temperature unit applies to Live only. Apply it to History too.
- It still shows the Work filter, the Work hours settings card and "Keep hourly data". These were removed from v1.
- It has no Language setting, no Import, and Export has no defined output. See `localization.md` and `engineering/sync-and-import.md`.
- Strings are English only.
