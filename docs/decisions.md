# Decisions

*Last reviewed: 2026-10*

A short, append-only log. Each entry: context, decision, consequences. Do not delete entries; mark them superseded. Review the statuses once a quarter so nothing stays "proposed" after it shipped. If this file grows past about 15 entries or an entry needs long reasoning, move that entry to its own file in `docs/decisions.md`.

## D-0001 Raw readings are the source of truth

**Status:** accepted · Oct 2026

The first plan stored only hourly and daily aggregates. The phone export and the sensor both give raw readings every 1 to 2 minutes, and rules (Night window, thresholds) may change.

**Decision:** store every reading in monthly CSV files and calculate aggregates from them in memory. No hourly or daily files.

**Consequences:** nothing is lost when rules change; dedupe needs the interval-slot merge rule; text files stay readable and easy to back up; about 12 MB per year at 2-minute intervals.

## D-0002 WPF on .NET 10; Windows 10 and 11

**Status:** accepted · Oct 2026 (amended from .NET 8)

The owner knows WPF; the design needs custom charts and tray behaviour. .NET 8 reaches end of support on 10 Nov 2026, and .NET 10 is the current LTS (supported to Nov 2028).

**Decision:** WPF on .NET 10 (`net10.0-windows10.0.19041.0` for the app). Support Windows 10 (2004 and later) and Windows 11. Mica only on Windows 11; solid colour on Windows 10. Confirm the .NET 10 minimum Windows 10 version against Microsoft's supported-OS table.

**Consequences:** square corners and no snap-layout popup on Windows 10. The tray flyout, title bar and BLE sync are prototyped first (`plans/spikes.md`).

## D-0003 Microsoft Store first; data in Documents

**Status:** accepted · Oct 2026

Store (MSIX) apps lose private data on uninstall.

**Decision:** ship in the Microsoft Store first, then GitHub and winget. Default data folder is `Documents\Deskair`.

**Consequences:** same folder for every install type; survives reinstall; the user can back it up. Toast setup differs between packaged and unpackaged builds.

## D-0004 English and Ukrainian

**Status:** accepted · Oct 2026

**Decision:** support English (default) and Ukrainian from v1 with resx files and a runtime language switch. Files always use the invariant culture.

**Consequences:** every string needs two versions and Ukrainian plural forms; layouts must tolerate 30 % longer text; Ukrainian copy needs native review.

## D-0005 No work hours in v1

**Status:** accepted · Oct 2026

Work hours mean different things to different people (for example working from home).

**Decision:** History offers All day and Night only. The Night window is fixed at 22:00 to 07:00.

**Consequences:** simpler UI and data. Can return later as an option.

## D-0006 Alerts follow Windows Do Not Disturb

**Status:** accepted · Oct 2026

**Decision:** use normal Windows toasts. Show "Alerts paused by Windows" in the tray tooltip and Settings. The tray icon colour always updates.

**Consequences:** alerts can be silent in meetings; the tray icon still shows the state.

## D-0007 Five projects and one test project

**Status:** accepted · Oct 2026

Earlier drafts had 17 projects, then one. Neither fit: too many hid the structure, one could not stop logic from depending on UI.

**Decision:** `Deskair.Domain`, `Deskair.Services`, `Deskair.Sensors`, `Deskair.Storage`, `Deskair.App`, and `tests/Deskair.Tests`. Project references enforce the direction of dependencies; one namespace test enforces the rest (see `engineering/architecture.md`). Names avoid `Core`, `Infrastructure` and `Application` (the last clashes with `System.Windows.Application`).

**Consequences:** compiler-enforced pure logic, easy fakes for tests, and a sensor module that cannot reach the UI. Split further only for a concrete reason.

## D-0008 Sensor-agnostic design with in-project sensor folders

**Status:** accepted · Oct 2026

The app is open source and forks may want another sensor.

**Decision:** everything outside `Deskair.Sensors` and `App/Composition` knows nothing about a vendor. A sensor is a folder in `Deskair.Sensors` implementing `ISensorProvider` and `ISensor`, registered by one line. Bluetooth transport (`IBleConnection`) is separate from protocol parsing. No runtime plug-in loading in v1.

**Consequences:** a new sensor is mostly a parser plus a provider class. The core requires exactly four measurements (CO₂, temperature, humidity, pressure) and treats history, battery and signal as optional capabilities. A fork must rebuild the app.

## D-0009 MVVM with CommunityToolkit.Mvvm

**Status:** accepted · Oct 2026

The UI has many state combinations (zone, condition, metric, range, filter, theme) and the Live screen and flyout show the same state.

**Decision:** MVVM using only `CommunityToolkit.Mvvm` (source generators for properties and commands, `IMessenger`). View models contain presentation logic only; calculations stay in Domain and Services. Chart drawing and hover stay inside the chart controls.

**Consequences:** screens follow state automatically; view models are unit-testable; a small amount of indirection to learn.

## D-0010 Name: Deskair

**Status:** accepted · Oct 2026

"Air Pulse" has an unrelated and unfortunate meaning in search results, which hurts discoverability and the Store listing.

**Decision:** the app, solution, namespaces, data folder (`Documents\Deskair`) and listing are named **Deskair**. The name does not mention Aranet, so the app can add sensors later. Aranet4 appears in the description, keywords, README and GitHub topics as a statement of compatibility ("Unofficial desktop monitor for the Aranet4 CO₂ sensor").

**Consequences:** before publishing, check the Store name, GitHub, domain and trademark databases, and reserve the name in Partner Center. Do not use "Aranet" in the product name without the maker's permission. Rename the repository to match when ready (GitHub keeps redirects).

## D-0011 Agent instructions: shared file plus path-scoped rules

**Status:** accepted · Oct 2026

An earlier draft put a `CLAUDE.md` in every project folder. Claude Code loads nested `CLAUDE.md` files only when it reads a file in that folder, and the official guidance is to put area-specific rules in path-scoped rules and procedures in skills.

**Decision:** `AGENTS.md` holds the shared rules and `CLAUDE.md` imports it (Claude Code uses `CLAUDE.md` when both exist). Area rules live in `.claude/rules/*.md` with `paths` frontmatter. Procedures are skills backed by `docs/guides/`. Hard limits go in permission rules or hooks, set up by the owner.

**Consequences:** rules load on read, edit and create; one folder to audit; no nested `CLAUDE.md` files. Maintenance guide: `docs/guides/maintaining-agent-instructions.md`.


## D-0012 The control gallery is part of every build

**Status:** accepted · Oct 2026

The first plan opened the gallery only in Debug builds. That needs conditional compilation or a build check in the startup code.

**Decision:** `--gallery` works in every build. There are no `#if DEBUG` blocks and no extra branches; the option is read in one place (`Composition/AppOptions`).

**Consequences:** simpler code. The gallery uses the simulated sensor only, never touches storage or Bluetooth, and a normal launch never opens it. If a Store build should not expose it, remove the option in the packaging step instead of in the code.
