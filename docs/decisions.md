# Decisions

*Last reviewed: 2026-10-19*

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

## D-0013 Readings history is append-only

**Status:** accepted · Oct 2026

The earlier rule was "sensor values win over imports", which means rewriting an imported row when the sensor later delivers the same slot. Rewrites are slower, riskier (interrupted writes) and the values come from the same sensor.

**Decision:** the first valid reading stored for an interval slot stays. Later readings for that slot are ignored, whether they come from a sensor sync or an import. A reading with a missing or non-finite value does not fill a slot.

**Consequences:** monthly CSV files are only ever appended to, so the store needs no rewrite or temp-file logic; merging is a set difference by slot; sync and import are idempotent. If an import contains wrong values for a slot, they stay (a repair tool would be a separate feature).

## D-0014 Tests use xunit v3 on Microsoft.Testing.Platform

**Status:** accepted · Oct 2026

The .NET 10 SDK no longer runs VSTest-based test projects through `dotnet test` unless MTP is chosen, and xunit v3 is the current line.

**Decision:** `xunit.v3` in `tests/Deskair.Tests` (an executable project) and `"test": { "runner": "Microsoft.Testing.Platform" }` in `global.json`. No `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`.

**Consequences:** `dotnet test tests/Deskair.Tests` is unchanged for contributors. Test Explorer in Visual Studio and VS Code needs a version that supports MTP.

## D-0015 No recovery-time estimate; qualitative advice and mini-card subtitles

**Status:** accepted · Oct 2026; humidity labels amended by D-0021

The prototype said "Should drop below 1,000 ppm in about 9 min". The fall rate depends on room size, doors and windows, people and outdoor air, and sensor readings come every 1 or 2 minutes, so any estimate would sound more certain than it is.

**Decision:** the advice text depends only on the zone and the CO₂ trend (`Advice_<Zone>_Text_<Falling|Steady|Rising>`). The observed change ("254 ppm over 32 min", `Trend_Detail`) stays, because it is a measurement. Mini cards show the trend (the same `Trend_*` words as the hero, with no "this hour", because CO₂ is measured over 30 minutes and the others over 1 hour); temperature and humidity show a label with the ideal range (`Mini_Temperature_*`, `Mini_Humidity_*`) only when the value is outside 20 to 24 °C or 30 to 50 %. Pressure has no health range, so it shows no comfort label.

**Consequences:** no forecasting code. A later feature could show an observed drop ("dropped 180 ppm in 30 min").

**Clarification (POL-28, Oct 2026):** the rule forbids estimating how long the air will take to improve. A suggested action may carry a duration ("Open a window for about 10 minutes"): it tells the user what to do, not what will happen, and the toast and the hero use the same words.

## D-0016 Writing to a sensor through `IBleConnection`; history is returned only when complete

**Status:** accepted · Oct 2026

Spike S1 showed that the Aranet4 history is requested by writing a command to one characteristic and then reading another repeatedly. `IBleConnection` could only read and subscribe, and its rule is to add a method only when a driver needs it.

**Decision:** add `WriteAsync(service, characteristic, value, ct)` to `IBleConnection` (card BLE-02d). The Aranet4 driver downloads CO₂, temperature, humidity and pressure completely and only then returns readings, oldest first. A download that fails, stalls (20 s without progress) or is cancelled throws and returns nothing, so the sync service reports it as incomplete and the saved cursor does not move.

**Consequences:** no streaming of partial history; the four metrics are joined by their position, and a reading taken by the sensor during the download is detected from the age counter and corrected for (`Aranet4Parser.ParseHistory`).

## D-0017 Ink is the app's accent; zones are the only colour

**Status:** accepted · Oct 2026 (POL-00)

The UI review (`docs/plans/ui-polish-plan.md`) found that one zone appeared up to ten times at once (hero fill, border and shadow, pill, advice fill and border, gauge, chart bands, line, ribbon), toggles were green (the Good colour), the primary button black and the logo amber (the Elevated colour): the app had no colour of its own and the zone colours stopped carrying meaning.

**Decision:** the accent is ink (`accent`: text.primary in each theme) for the nav underline, toggles, the primary button and selected states; the focus ring stays blue. The hero card is white with a zone glow in one corner; zone fill, zone border and zone-tinted shadow go; the advice panel keeps its fill and loses its border. The flyout gets the same glow and no inner card.

**Consequences:** token changes in both themes (design-tokens.md); green means Good air only; the "use the Windows accent colour" idea (polish-ideas 13) is dropped for now. Supersedes the Q47 notes on the zone-tinted hero shadow and the zone card border.

## D-0018 History draws a grid of hours instead of range bars and a typical-day card

**Status:** accepted · Oct 2026 (POL-00)

Range bars (minimum to maximum per day) are set by one spike, so the largest shape shows the least useful number, and they cannot show when in the day the air was bad. The typical day was a second card with its own axis over the same data. Missing days filled two thirds of the Month chart with hatching.

**Decision:** Week and Month show a days × hours grid in a six-step CO₂ heat scale; Year shows months × days. The typical day is the grid's last row with the worst three hours outlined; Day draws it as a grey line under the readings. Two or more missing days collapse into one row. The two lightest heat steps are below the 3:1 chart contrast on purpose; the values stay available as text (row averages, tooltips, focus and Narrator labels).

**Consequences:** a new `HourGridChart` (POL-11, POL-12); `TypicalDayChart`, the range-bar and weekend-shading layers and gap hatching leave History; new `heat.*` tokens; the "best day / worst day" numbers move from the stats card to tags on the rows. Supersedes the Q47 notes on History bars, the typical-day card and the coverage box.

## D-0019 History has a period navigator; the Night filter and Group by go

**Status:** accepted · Oct 2026 (POL-00)

Every range ended now, so an earlier day or week could not be opened. The toolbar had four control groups; the Night filter and Group by changed the chart in ways that were hard to predict, and the grid shows days and night hours by its shape.

**Decision:** ‹ › step back by the range's length (Day: the last 24 hours, then calendar days; Week 7 days; Month 30 days; Year 12 calendar months, one year back at most); the title names the period. The toolbar keeps metric, period, range and an export icon. The night hours from Settings → Sleep show as a band and in the summary's Nights section; the sleep card becomes the summary's "Last night" section in Day view, with the same verdict rules.

**Consequences:** a period model and navigator (POL-09); HIST-02 (Hours filter) and HIST-03 (Group by) are removed from the UI (POL-10), `NightWindow` stays; screens-and-states.md and behaviour-rules.md updated; a drill-down later becomes a change of range and offset.

## D-0020 Metric values have no group separator; display values use proportional figures

**Status:** accepted · Oct 2026 (POL-00)

"1,151" at hero size read as a break inside the number, and the comma took more room than a digit; the Aranet app shows "1151". Tabular figures gave each "1" a full digit width at 104 px, leaving gaps in "1151".

**Decision:** ppm and other metric values are written without a group separator in both languages, everywhere in the UI ("1151 ppm", "1013.2 hPa"); the decimal mark follows the language. Percent is "59%" in English and "59 %" in Ukrainian (narrow no-break space). Standalone display and stat values use proportional figures; numbers in columns and axis labels use tabular figures.

**Consequences:** one formatter for ppm and one for percent (POL-02) replaces `N0` and `N1` in view models and chart layers; localization.md rule 4 and behaviour-rules.md "Number and time format" updated; CSV files are unchanged.

## D-0021 Humidity labels: "a bit" before "too"

**Status:** accepted · Oct 2026 (POL-00); amends D-0015

At 51 % the mini card said "Too humid", which read as an alarm over one point.

**Decision:** the ideal range stays 30 to 50 %. 25 to 29 % shows "A bit dry", 51 to 55 % "A bit humid"; below 25 % "Too dry", above 55 % "Too humid"; each with the ideal range. Temperature labels are unchanged.

**Consequences:** two new strings per language (`Mini_Humidity_*`); behaviour-rules.md "Live values" updated.

## D-0022 One colour, one meaning: neutral trend, brand logo, neutral CO₂ dot, quiet ideal band

**Status:** accepted · Oct 2026 (POL-50); a review of the Live and History screenshots found places where the app contradicts itself

A red "Rising" sat on a Good card with the advice "Air is fresh, nothing to do" while CO₂ climbed 184 ppm in 30 minutes. Green, amber and red mean zones, yet the header logo ring and the CO₂ tab dot also took those colours, so amber meant both "Elevated" and "the CO₂ series". "0 min Good so far" read as a fault in the first minute. The ideal band on Temperature and Humidity was the heaviest thing on the chart.

**Decision:** (1) the trend text is neutral in every zone; zone colour stays on the pill, gauge, chart and ribbon. (2) In Good, a rising trend at 800 ppm or more shows the advice "Good now, but rising"; there is still no recovery-time or time-to-High estimate (D-0015 stands). (3) Time in zone under one minute reads "< 1 min". (4) The header logo ring is the brand ink; only the tray icon takes the zone colour. (5) The CO₂ metric dot is a neutral slate, so amber means only Elevated. (6) The ideal band is the Good colour at 9 % on the card surface, with 1 px dashed limits at 35 % and a small label top-left.

**Consequences:** `Trend.*` and `Metric.*.Fill/Ideal` tokens are removed; `LogoRing` loses its zone; two advice strings and `Duration_LessThanMinute` in both languages; design-tokens.md, components.md, screens-and-states.md, behaviour-rules.md and localization.md updated. Second batch (heatmaps): [`review-feedback-polish.md`](plans/review-feedback-polish.md).

## D-0023 History grid: keep the six CO₂ steps; tag only complete rows; "worst" only for comfort metrics

**Status:** accepted · Oct 2026 (POL-51 to POL-53); keeps D-0018, amends POL-18

An outside review called the six-step CO₂ legend a second scale next to the three zones. Looking at it again, hue already gives the zone (green, amber, red) and lightness gives how far into it, and the legend names Good, Elevated and High over its swatches. The six steps stay, and no "three or six zones" setting is added. Live keeps three zones because it answers "do I need to act?", and there are three actions.

The review also found real flaws. Today could win "best" with 17 hours of data, before the evening peak, so an unfinished day was always favoured. The row-end bars had no caption. On Temperature and Humidity most days are comfortable, so a "best" tag ranked near-identical days.

**Decision:** (1) best and worst tags go only to complete rows: not today, not the month in progress. (2) A caption over the row-end bars names them: "Time in zones" (CO₂), "Time in range" (Temperature, Humidity). (3) Temperature and Humidity have no "best" tag; "worst" appears only on the row with the least time inside the range, and only if that row has time outside it.

**Consequences:** `HourGridDay.IsComplete`, `HourGridMonth.IsComplete`, `HourGridCalculator.ComfortWorst` replaces `ComfortExtremes`; `HourGridModel.BarCaption` and two strings per language; behaviour-rules.md rules 5 and 10 and screens-and-states.md updated. The floating tooltip and the calendar Month view stay in [`review-feedback-polish.md`](plans/review-feedback-polish.md) batch 3.

## D-0024 Comfortable is pale green in the Temperature and Humidity grids

**Status:** accepted · Oct 2026; amends POL-18 and POL-22

Comfortable squares were neutral grey. Everywhere else green means fine: the CO₂ grid's Good, the row-end bar and the "Comfortable" pill. Grey reads as inactive or missing, so a good month looked empty and the grid said "nothing here" about the hours that were fine.

**Decision:** `heat.comfort` is a mid green (light #B4E0C9, dark #244F3F). A first try with the palest green (#DDF2E7) nearly vanished on the light card and was fainter than the mild cool and humid steps, which inverted the hierarchy. Cool stays blue, warm orange; the outer steps stay the strongest marks. "No data" stays an empty outline.

**Consequences:** two token values and design-tokens.md; no code change.

## D-0025 An alert episode survives short dips below 1000 ppm; pressing a toast acknowledges it

**Status:** accepted · Oct 2026; amends the Alerts rules

The logs showed Elevated toasts 9 to 21 minutes apart, mostly at 1003 to 1038 ppm. One reading below 1000 ended the episode, so CO₂ hovering at the limit started a new episode, and after five minutes a new toast. A user who had pressed the toast and opened the app got another one anyway.

**Decision:** (1) An episode ends only after the air has stayed Good for 10 minutes. (2) Pressing a toast or its Open button acknowledges the current episodes that have sent a toast: they send no more toasts, except the escalation to High (still after its 5 minute dwell). A newly sent toast is unacknowledged. Snooze is unchanged.

**Consequences:** `AlertPolicy.RecoveryDwell`, `AlertEngine.AcknowledgeAll`, and `ToastActionHandler` calls it; behaviour-rules.md "Alerts" updated. Not done: skipping toasts while the window is open and in front.
