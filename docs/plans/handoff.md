# Handoff: after the M1 skeleton

*Last reviewed: 2026-10*

For the next agent. Read `AGENTS.md`, `docs/plans/m1-skeleton-brief.md` (card format and protocol in sections 4 and 6) and `docs/plans/todo-index.md` first. Nothing is committed yet; the project owner reviews and commits.

## State

- The solution builds with 0 warnings. `dotnet test tests/Deskair.Tests` passes (154 active tests, 107 skipped, one skipped test per card where a test is possible). `dotnet format Deskair.sln --verify-no-changes` passes. All three must stay green.
- The app starts in both modes (`--gallery` and normal). The windows are built and smoke-tested, but nobody has looked at them: visual checks are the owner's job.
- Working: Domain zones and metric descriptors, simulated sensor, `CsvReadingFormat`, localization (resx, plurals, `{loc:T}`), theme tokens and `ThemeService`, five reference controls (`Card`, `Pill`, `StatusDot`, `SegmentedControl`, `ToggleSwitch`), main window shell, gallery, dependency-rules tests.
- Everything else is a stub with a TODO card (147 cards). Stub controls and views have an empty template and no exception; methods throw `NotImplementedException("TODO(ID)")`.
- Decisions already made are in `docs/decisions.md` (D-0012 gallery in every build, D-0013 append-only history, D-0014 xunit v3) and in `docs/plans/questions.md` ("Decided").

## Suggested next phase: finish M1, then M2

M1's exit criteria (roadmap) include the data layer, which is still stubs. Work in this order, one card per commit, `TODO-ID: summary`:

1. **Pure logic, haiku-sized:** the Domain cards (HIST-*, LIVE-01a/b, TIME-01c; TIME-01a/b are sonnet). Their tests already exist, skipped: unskip, see them fail, implement.
2. **Services logic:** SYNC-01a/01b, CONN-01a, ALERT-01a, QUERY-01a to 01c, DEV-01a/b, SET-01a/b, EXPORT-01a.
3. **Storage:** STORE-02a to 02c (`ReadingIndex`), STORE-01a to 01d (`MonthlyCsvStore`, append-only), STORE-03*, SET-02*, EXPORT-02a. These need temp-folder tests.
4. **Then M2 (Live):** view model cards (LIVE-*), controls CTRL-*, `ChartBase` (CHART-01a to 01c, sonnet), layers CHART-10 to 19, charts CHART-20, 23, 24, views LIVE-00b to 03b, footer SHELL-01b. Add each control to the gallery as it is finished.

Sonnet is needed for L cards and for anything with timing, DST, threading, Windows APIs or custom drawing. Aranet4 cards (ARANET-*) wait for spike S1 (`docs/plans/spikes.md`); tray, toast and title bar cards wait for spikes S2 to S5.

## Open items for the owner

`docs/plans/questions.md` "Open": Q4 (remember selected device), Q5 (quiet hours with equal times), Q6 (full settings list), Q7 (import: repeated autumn hour, day-first dates), Q9 (device folder name), Q13 (Do Not Disturb unreadable), Q14 (title bar, needs spike S3), Q17 (placeholder strings, Ukrainian review). A card that depends on one names it. Ask before adding a tray or toast package.

## Things that cost time this time

- **Controls and tokens in XAML:** use `DynamicResource` for tokens inside control dictionaries. `StaticResource` there failed to find tokens at run time (build was green). Storyboard values must be `StaticResource`, so `ToggleSwitch.xaml` merges `Spacing.xaml` itself. Window styles do not apply to derived window classes; windows set `Style="{StaticResource Window.Base}"`.
- **`RowDefinition.Height` needs a `GridLength`:** a double token crashes at start-up (`Grid.Header.Height`).
- **UI tests:** WPF allows one `Application` per process, so all UI tests run through `UiHost` on one STA thread. Code-created controls get their style only after `BeginInit`, `EndInit`, `ApplyTemplate`: use `UiHost.Prepare`. Do not touch static members of `UiHost` from the thread it starts in its static constructor (deadlock).
- **Compile-only success is not enough for XAML.** Run the smoke tests (`WindowSmokeTests`, `ControlTemplateTests`) or start the app.
- WPF projects do not get `System.IO` as an implicit using. The test project uses `NetArchTest.Rules.TestResult` explicitly because xunit v3 also has a `TestResult`.
- The style rules are enforced by the build (no `_` field prefix, braces, no `DateTime.Now`). Files must be UTF-8 without a byte order mark (PowerShell 5 `Set-Content -Encoding utf8` adds one; `dotnet format` then fails with CHARSET).
- Long multi-file shell heredocs failed to parse in this environment; create files with the editor tools instead.
- Strings: every English key needs a Ukrainian key (a test checks it). Numbers in the UI use the selected culture (`1,068` and `1 068`).
- **Tokens:** the theme dictionaries were generated from `design-tokens.md` and the prototype by a script that is not in the repo. Edit the XAML files directly from now on, and keep light and dark keys identical (a test checks it).
- **The TODO index** was also generated from the cards. Until a script is added, update the status and remove rows by hand when a card is done, and add rows for new cards.

## Where to look

| Need | File |
|---|---|
| What a card must do | the card above the stub; its `Spec:` line names the doc |
| Behaviour, thresholds, merge, trend | `docs/product/behaviour-rules.md`, `docs/engineering/data-model.md` |
| Colours and sizes | `docs/design/design-tokens.md`, then `design/prototype/index.html` |
| Adding a control, string, setting | `docs/guides/` |
| Dependency rules | `docs/engineering/architecture.md` section 3, `tests/Deskair.Tests/Architecture` |
