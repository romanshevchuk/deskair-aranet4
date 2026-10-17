# Backlog

*Last reviewed: 2026-10-17*

**Start here.** Everything v1 needs is built. This file lists only what is left before release. Finished items are in [`backlog-done.md`](backlog-done.md) (archive, do not read unless you need history). Update this file in the same change as the work: move a row to the archive when it is done.

Status: `todo`, `doing`. Who: **agent** (an AI agent can do it and test it), **owner** (needs the owner's hands, eyes, hardware or decision; an agent must not mark it done), **both** (agent prepares, owner confirms).

Order of work: 1. Owner checks (R1) and decisions (R2), which can start now. 2. Agent code work (R3) in the order shown. 3. Release (R4) after R1 and R2 pass.

## R1. Owner checks before release (hardware and eyes)

| ID | What to check | Who | Status |
|---|---|---|---|
| OWN-01 | Quiet-hours and night-hours times typed by hand in Settings (Settings was reworked 2026-10-17: one "Active between" row, captions under fields) | owner | todo |
| OWN-02 | A real press on an alert toast (Snooze, Open), also with the app not running; Do Not Disturb and the "paused by Windows" row | owner | todo |
| OWN-03 | Tray: menu items, value icon, flyout closes on click-away and Esc, the Snooze menu inside the flyout stays open | owner | todo |
| OWN-04 | Flyout and tray on other taskbar edges, other DPI scales, a second monitor (spike S2) | owner | todo |
| OWN-05 | "Try again" and the 30 s read timeout with the sensor out of range; PC sleep and wake with the real sensor | owner | todo |
| OWN-06 | Sleep card, History Night view and Stats card on real data | owner | todo |
| OWN-07 | Real open and save dialogs, Open folder, importing the phone export through the UI. (Pair, Add, Cancel and Bluetooth off then on were checked by the owner 2026-10-17: fine.) | owner | todo |
| OWN-08 | Windows 10 (never tried): solid backdrop, title bar, toasts, Do Not Disturb | owner | todo |
| A11Y-01 | Manual accessibility check in `accessibility.md` (Narrator, keyboard, scaling); the automated part is done | owner | doing |
| I18N-02 | Native Ukrainian review of every string marked "Draft: needs native review" (`ukrainian.md`) | owner | todo |

## R2. Owner decisions (questions an agent cannot settle)

Full text in [`questions.md`](questions.md) "Open". Until answered, the agent keeps the stated assumption.

| ID | Decision | Who | Status |
|---|---|---|---|
| Q27 | What the user sees when local history cannot be read (locked or corrupt file, missing folder). Likely needed before release | owner | todo |
| Q7 | Phone export date order and the repeated autumn hour | owner | todo |
| Q17 | Placeholder strings: owner wording | owner | todo |
| Q30, Q32, Q33 | Footer signal wording; signal strength not reported by a connected Aranet4; older firmware service id (not supported) | owner | todo |
| P1 | Store listing: app name, privacy policy page, screenshots | owner | todo |
| P2, P3 | Interval to recommend on first run; trend rule values after more real data | owner | todo |

## R3. Code work for agents (do in this order)

| ID | Item | Who | Status |
|---|---|---|---|
| REL-02 | Known gaps still open (the rest of the old list was checked 2026-10-17: the grey header ring and the dark Settings scrollbar were already fixed; History now refreshes on the UI thread; the main window has a minimum size, 1100 × 700; Devices follows new rooms again after leaving and re-entering Settings): (1) a room switch rebuilds the Devices rows while a name is being edited and drops the typed text (small); (2) `WindowsBleConnection` opens a new `GattDeviceService` for each characteristic lookup in an already-opened service (freed on dispose) and attaches its disconnect handler after the pairing checks (needs a real sensor to test) | both | todo |

## R4. Release

| ID | Item | Who | Status |
|---|---|---|---|
| REL-01 | Store packaging; winget and GitHub later. Needs P1 from the owner for the listing; the packaging itself is agent work | both | todo |
| REL-05 | Final pass: build, tests, format clean, renders in light and dark (EN), soak test, then tag | both | todo |

## Later (not v1)

| ID | Item | Status |
|---|---|---|
| POL-08 | Spike: acrylic flyout (optional) | later |
| POL-36 | Tests: merge the four near-identical render-test `BuildShell` helpers; look for other duplicates (tidiness only) | later |
| AIR-01 | Airing feedback | later |
| SLEEP-01 | Morning toast (the sleep verdict card is done) | later |
| HIST-07 | Click a bar to drill in | later |
| REPORT-01 | Shareable report | later |
| ROOMS-01 | All-rooms view | later |
| THRESH-01 | Editable CO₂ thresholds | later |
| WORK-01 | Work-hours filter (dropped from v1) | later |
