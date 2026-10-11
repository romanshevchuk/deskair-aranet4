# Data model

*Last reviewed: 2026-10*

Raw readings are the source of truth. Every average, minimum and maximum in History is calculated from them, so nothing is lost when a rule changes. Hourly and daily files are not stored.

| File | Content | Retention |
|---|---|---|
| `devices/<id>/readings-YYYY-MM.csv` | One row per reading, one file per month | Forever |
| `devices/<id>/device.json` | Room name and type, sensor interval history, last sync time | Forever |
| `devices/<id>/events.csv` | Airing events (later feature) | Forever |

Readings columns: `ts_utc, co2_ppm, temp_c, rh_pct, pres_hpa, src`. `src` is `dev` (sensor sync) or `imp` (import). Example: `2026-10-09T09:41:40Z,987,22.0,51,980.7,imp`.

- **Size:** a year at a 2-minute interval is about 263,000 rows (about 12 MB). Load everything into memory at start and aggregate on demand.
- **Location:** default `Documents\Deskair\`, not private app storage. A Store (MSIX) app's private data is deleted on uninstall. A visible folder survives reinstalling, can be backed up, and is the same for Store, GitHub and winget installs.
- **Time:** store UTC, show local time. Calendar days follow local time; a day is 23 or 25 hours when daylight saving changes.
- **Merge rule:** snap each reading to the sensor's interval grid (within half an interval) and treat the slot as one reading. History is append-only: the first valid reading stored for a slot stays, and later readings for that slot (sensor or import) are ignored. A reading with a missing or non-finite value does not count, so its slot stays empty. See D-0013.
- **Interval changes:** keep the interval history in `device.json`. Coverage is readings divided by expected slots.
- **Safety:** append to the month's file; readings files are never rewritten. A half-written last line is ignored when loading. `device.json` and settings are rewritten through a temp file and a rename. Allow one app instance.
- **Units:** °C and hPa as received. Files use the invariant culture.

## Export CSV

The same five columns as the phone export, with time as `YYYY-MM-DD HH:mm:ss` in local time. Export the current History range or all data.
