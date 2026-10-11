# M1 skeleton brief

*Last reviewed: 2026-10*

For the model that builds the skeleton (Claude Sonnet), and the protocol for the models that fill it in (Claude Haiku or others). Read `CLAUDE.md` and `docs/00-index.md` first.

## 1. Goal

Build a **compiling, runnable skeleton** of the solution described in `docs/engineering/architecture.md` (sections 2 to 5, 13), so that small, well-specified pieces can be completed one at a time by a smaller model. The skeleton fixes structure, names, contracts and styles. It does not implement complex logic.

Work on Windows with the .NET 10 SDK. WPF cannot be run elsewhere. You cannot see the UI, so do not claim visual correctness; the gallery is for the project owner to review.

## 2. Deliverables

1. `Deskair.sln`, five projects and `tests/Deskair.Tests` as in the architecture doc; `Directory.Build.props`, `Directory.Packages.props` (central package versions), `global.json`, `.editorconfig` (C# rules from `csharp-instructions.md`, or the defaults below).
2. **Domain:** all records, enums and interfaces named in the docs (`Reading`, `DeviceId`, `Zone`, `ZoneThresholds`, `Metric`, `MetricDescriptor`, `Period`, `TimeSlot`...). Implement the small pure ones fully with tests: `ZoneClassifier`, `ZoneThresholds`.
3. **Services:** every interface from architecture section 4 (`ISensorProvider`, `ISensor`, `IBleConnection`, `IBleScanner`, `IReadingImporter`, `IReadingStore`, `INotifier`, `ISettingsStore`, platform interfaces) and service classes with constructors, dependencies and **stub bodies**.
4. **Sensors:** `Simulated/` fully working (it feeds the gallery and tests); `Aranet4/` as stubs only (spikes S1 comes first).
5. **Storage:** class and file layout as stubs; `CsvReadingFormat` (read and write one row, invariant culture) fully implemented with tests as the reference pattern.
6. **App:** theme dictionaries with **every token** from `docs/design/design-tokens.md` (light and dark, zones, metrics, typography, spacing, radii); base styles; `ThemeService`; localization (`Localizer`, `LanguageService`, `PluralRules` with tests, `Strings.resx` and `Strings.uk.resx` with the seed strings from `docs/design/localization.md`, the `{loc:T}` markup extension); `MainWindow` shell with header, footer and navigation; DI composition (`ServiceRegistration`); `--gallery` window.
7. **Reference controls, fully implemented:** `Card`, `SegmentedControl`, `ToggleSwitch`, `Pill`, `StatusDot`, each in its own folder, shown in the gallery in light, dark, English and Ukrainian. They set the pattern for all other controls.
8. **All other controls, charts, views and view models:** files exist with the right names, correct base class, dependency properties or properties declared, and stub bodies with TODO cards (section 4).
9. `tests/Deskair.Tests`: folders per project; the dependency-rules test (`Architecture/DependencyRulesTests`, rules from architecture section 3, using NetArchTest.Rules or reflection); one skipped test per TODO card where a test is possible.
10. Do not change `.claude/` (rules, skills, commands). Verify the commands in `AGENTS.md` and correct them if they differ. Do not read the legacy prototype.
11. `docs/plans/todo-index.md`: a table of every TODO card (ID, file, size S/M/L, suggested model, status).

## 3. Defaults and constraints

- C# defaults if `csharp-instructions.md` is empty: file-scoped namespaces, nullable enabled, `record` for data, `async` methods end in `Async` and take a `CancellationToken`, no regions, one public type per file, small methods.
- Approved packages: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting` (and its DI and logging), `xunit` with its runner, `NetArchTest.Rules`. Use current stable versions. **Ask before adding anything else.** Excel reading, tray and Bluetooth libraries are decided later.
- Only `App/Composition` may reference `Deskair.Sensors`. No WPF types in `Domain`, `Services`, `Sensors`, `Storage` or `ViewModels`.
- No hard-coded colours or strings: tokens and resources only, English and Ukrainian.
- Do not invent behaviour. If the docs do not say, put the question in `docs/plans/questions.md` and continue.

## 4. TODO card format

Use one card per method or small class. A card is a comment directly above a stub that throws `NotImplementedException("TODO(ID)")`.

```csharp
// TODO(HIST-04a) [S, haiku]
// Spec: docs/product/behaviour-rules.md "Aggregation" rules 4 and 5.
// Input : readings in one bucket, expected slot count.
// Output: BucketState (Complete, Partial, NoData).
// Rules : under 80 % of expected = Partial; none = NoData; otherwise Complete.
// Edge  : expected = 0 returns NoData.
// Done  : unskip tests in Domain/Analytics/BucketStateTests.cs and make them pass.
public static BucketState Classify(int present, int expected)
    => throw new NotImplementedException("TODO(HIST-04a)");
```

Rules for cards:
- Maximum about 30 lines of implementation per card. Split bigger work into several cards. Do **not** leave long methods filled with comments.
- Every card names its spec file, inputs, outputs, edge cases and a concrete done condition (a test or a visible gallery state).
- Give each card a size (S, M, L) and a suggested model: `haiku` for S and clear M, `sonnet` for L or anything involving timing, threading, DST, Windows APIs or custom drawing.
- Cards that need a decision, not just implementation, are **not** cards: list them in `docs/plans/questions.md`.
- Write the test first (skipped, with reason `TODO(ID)`) when the logic is testable.

## 5. Work split guidance

| Suggested for Haiku (clear spec, local, testable) | Keep for Sonnet or the owner |
|---|---|
| Remaining controls from tokens and the reference controls | Chart base class, layers and hit testing |
| Simple view models with generated properties | Slot merging, DST and calendar logic, sync |
| Aggregation functions, coverage, trend, typical day | Windows Bluetooth, tray, flyout positioning, toasts |
| CSV read and write, settings JSON, export | Aranet4 client and protocol (needs spike results) |
| Resource strings and Ukrainian plural keys | Title bar and backdrop |
| Phone-export parser against a sample file | Anything where the card says "decide" |

## 6. Protocol for the model that completes a card

1. Take **one** card. Read the spec it links to and the code around it.
2. Unskip or write the test first. Run it and see it fail.
3. Implement only what the card says. Do not change other files except to add tests or strings.
4. Run `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`. All must pass. Never claim a card is done without running them.
5. Update the status in `docs/plans/todo-index.md`; remove the stub exception and the card comment.
6. If the card is unclear, conflicts with a doc, or needs a decision, stop and add a question to `docs/plans/questions.md`. Do not guess.
7. Commit message: `TODO-ID: short summary`.

## 7. Review checklist (owner or Sonnet, per batch of cards)

- Builds and tests green; no new packages; dependency-rules test passes.
- No hard-coded colours or strings; Ukrainian string present for every English one.
- Behaviour matches `docs/product/behaviour-rules.md`.
- Controls shown in the gallery in light, dark, English and Ukrainian; owner checks the look against `design/prototype/index.html`.
- Cards are removed when done; the todo index is accurate.
