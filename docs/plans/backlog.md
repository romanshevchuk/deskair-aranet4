# Backlog

*Last reviewed: 2026-10-23*

**Start here.** Everything v1 needs is built. This file lists only what is left before release. Finished items are in [`backlog-done.md`](backlog-done.md) (archive, do not read unless you need history). Update this file in the same change as the work: move a row to the archive when it is done.

Status: `todo`, `doing`. Who: **agent** (an AI agent can do it and test it), **owner** (needs the owner's hands, eyes, hardware or decision; an agent must not mark it done), **both** (agent prepares, owner confirms).

Order of work: 1. Owner checks (R1) and decisions (R2), which can start now. 2. Agent code work (R3) in the order shown. 3. Release (R4) after R1 and R2 pass.

## R1. Owner checks before release (hardware and eyes)

| ID | What to check | Who | Status |
|---|---|---|---|
| OWN-05 | Still open: "Try again" and the 30 s read timeout with the sensor out of range. (PC sleep and wake with the real sensor: checked by the owner 2026-10-18, the app left open over sleep, the sensor was connected after wake) | owner | doing |
| OWN-08 | Windows 10 (never tried): solid backdrop, title bar, toasts, Do Not Disturb | owner | todo |
| A11Y-01 | Manual check in `accessibility.md`. Checked by the owner 2026-10-18: keyboard only, scaling 150 % and 200 %, High Contrast, Narrator on Live and Settings. Narrator on the flyout checked 2026-10-18 (fine; the flyout now opens above the tray icon also when opened from the keyboard). Still open: Narrator on the History period buttons | owner | doing |
| I18N-02 | Native Ukrainian review of the strings still marked "Draft: needs native review" (9 left on 2026-10-19: the Smart Home steps, About card, rising advice, "< 1 min", welcome text; the rest were reviewed by the owner and the agent) and of the listing text in `store-listing.md` | owner | doing |
| OWN-09 | BLE-10 with the real sensor (code done 2026-10-20; checked by the owner on 2026-10-20 with the unpackaged app: phone app connects right after the sync, 10 minutes of live updates with Hero and chart matching, Sync history, sleep and wake; still open: the packaged app, and the battery, see D-0028): (1) start Deskair, wait for the history sync to finish, open the phone app at once: it must connect and sync; (2) leave Deskair running 10 minutes with the phone app closed: Live must keep updating every 2 minutes and History must show no new gaps; (3) press Settings → Data and storage → Sync history: the footer shows the sync and the phone app is out of reach only for that time; (4) sleep and wake the PC: a sync runs and live values follow; (5) battery in the footer matches the phone app; (6) the Aranet4 battery lasts as before or better (watch over weeks) | owner | todo |

## R2. Owner decisions (questions an agent cannot settle)

Full text in [`questions.md`](questions.md) "Open". Until answered, the agent keeps the stated assumption.

| ID | Decision | Who | Status |
|---|---|---|---|
| Q27 | Done 2026-10-18 (banner, option A). Owner check left: start the app once with the month file locked and see the banner (see questions.md Q27) | owner | todo |
| Q7 | Phone export date order and the repeated autumn hour | owner | todo |
| Q17 | Placeholder strings: owner wording | owner | todo |
| Q30, Q32, Q33 | Footer signal wording; signal strength not reported by a connected Aranet4; older firmware service id (not supported) | owner | todo |
| P1 | Store listing: app name, privacy policy page, screenshots | owner | todo |
| P2, P3 | Interval to recommend on first run; trend rule values after more real data | owner | todo |

## R3. Code work for agents (do in this order)

| ID | Item | Who | Status |
|---|---|---|---|
| REL-02 | Known gaps still open (the rest of the old list was checked 2026-10-17: the grey header ring and the dark Settings scrollbar were already fixed; History now refreshes on the UI thread; the main window has a minimum size, 1100 × 700; Devices follows new rooms again after leaving and re-entering Settings): (1) fixed 2026-10-18: a room switch keeps the Devices rows; (2) `WindowsBleConnection` opens a new `GattDeviceService` for each characteristic lookup in an already-opened service (freed on dispose) and attaches its disconnect handler after the pairing checks (needs a real sensor to test) | both | todo |
| BLE-10 | Release the sensor between syncs (D-0028): connect only to sync (start, wake, "Sync history", retry), live values from advertisements (`ISensor.ListenAsync`, passive scan), battery from advertisements. Done 2026-10-20 in the working tree with tests; waits for OWN-09. Open: a re-sync when a gap is noticed; signal strength from advertisements (Q33) | agent | doing |
| SIZE-02 | Trim only the Windows SDK projection on publish (`.msix` 65.8 → 60 MB). Built and agent-checked 2026-10-23 (D-0029); waits for the owner checks in [`size-and-startup.md`](size-and-startup.md) sections 2 and 3 | both | owner check |
| PERF-02 | Replace the generic host with a plain `ServiceCollection` (section 5). **Needs the owner's yes first** (package change) | both | todo |
| PERF-06 | Measure ReadyToRun; the owner decides from the numbers (section 9). Agent part done 2026-10-23 (+4.1 MB, about 56 ms warm); waits for the owner's cold-start runs | both | owner check |

## R4. Release

| ID | Item | Who | Status |
|---|---|---|---|
| REL-01 | Store packaging; winget and GitHub later. Plan and status: [`store-packaging.md`](store-packaging.md) (phases 0 to 2 done 2026-10-18; texts, privacy page and screenshots prepared 2026-10-19, section 10 of the plan; phase 3 passed 2026-10-20; **published in the Microsoft Store on 2026-10-20, package 0.1.3.0**; see `handoff.md`) | both | done, move to backlog-done.md |
| REL-05 | Final pass: build, tests, format clean, renders in light and dark (EN), soak test, then tag | both | todo |

## Later (not v1)

| ID | Item | Status |
|---|---|---|
| POL-08 | Spike: acrylic flyout (optional) | later |
| POL-55 | Month view as a calendar grid (7 columns, about 5 rows, one colour per day, click opens the Day view) | later |
| POL-56 | Temperature grid on a diverging scale around 22 °C instead of grey for "comfortable" | later |
| POL-57 | "Typical" line on the Day chart as a quiet band (median and 25th to 75th percentile) with a value in the tooltip | later |
| POL-58 | Icons in the status badges (✓ / ! / ▲) so zones do not rely on colour alone | later |
| POL-59 | Temperature and Humidity chart lines coloured by comfort zone, as the CO₂ chart is | later |
| POL-60 | Floating tooltip beside the grid square (hover and keyboard), with the row and hour labels highlighted; today the readout is a fixed line under the legend. Needs edge handling and keyboard support | later |
| POL-36 | Tests: merge the four near-identical render-test `BuildShell` helpers; look for other duplicates (tidiness only) | later |
| AIR-01 | Airing feedback | later |
| SLEEP-01 | Morning toast (the sleep verdict card is done) | later |
| HIST-07 | Click a bar to drill in | later |
| REPORT-01 | Shareable report | later |
| ROOMS-01 | All-rooms view | later |
| THRESH-01 | Editable CO₂ thresholds | later |
| WORK-01 | Work-hours filter (dropped from v1) | later |
