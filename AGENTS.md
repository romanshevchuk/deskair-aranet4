# Deskair: instructions for AI coding agents

Deskair is a Windows desktop app (WPF, .NET 10) that shows live and historical CO₂, temperature, humidity and pressure from an Aranet4 sensor over Bluetooth LE. It lives in the tray, alerts the user when CO₂ is high, and stores every reading locally. Languages: English (default) and Ukrainian.

*Last reviewed: 2026-10*

## Data flow

`ISensor` (Sensors) → `SyncService` / `LiveFeed` (Services) → `IReadingStore` (Storage) → `ReadingQueryService` + Analytics (Domain) → ViewModels → Views (App/Ui)

## Projects (details: `docs/engineering/architecture.md`)

| Project | Role | May depend on |
|---|---|---|
| `Deskair.Domain` | Types and pure calculations (zones, aggregation, trend, coverage, calendar) | nothing |
| `Deskair.Services` | Workflows (sync, live feed, alerts, queries) and the interfaces they need | Domain |
| `Deskair.Sensors` | One folder per sensor (`Aranet4/`, `Simulated/`) | Services, Domain |
| `Deskair.Storage` | Monthly CSV readings, settings files | Services, Domain |
| `Deskair.App` | WPF UI, view models, localization, Windows glue, startup wiring | all |
| `tests/Deskair.Tests` | Tests; folders mirror the projects | all |

Area rules live in `.claude/rules/` (one file per project or folder). Claude Code loads them automatically when you read, edit or create a file in that area. In other tools, read the matching rule file before editing there.

## Commands (Windows only)

| Task | Command |
|---|---|
| Build | `dotnet build Deskair.sln` |
| Test | `dotnet test tests/Deskair.Tests` |
| Format check | `dotnet format Deskair.sln --verify-no-changes` |
| Run app | `dotnet run --project src/Deskair.App` |
| Run control gallery | `dotnet run --project src/Deskair.App -- --gallery` |
| Run the app on a simulated sensor (temp data folder, no hardware) | `dotnet run --project src/Deskair.App -- --simulated` |
| Run the app on one real sensor (own temp data folder) | `dotnet run --project src/Deskair.App -- --sensor C3DEC8FCC7C6` |
| Try the first-run screen with real Bluetooth (empty temp data folder, wiped at every start) | `dotnet run --project src/Deskair.App -- --first-run` |
| Render gallery images (light, dark, English, Ukrainian) to `artifacts/visual/` | `$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~GalleryRenderTests"` |

WPF cannot be built or run on Linux or macOS. If a command here is wrong, fix it in this file.

## Sources of truth

| Question | File |
|---|---|
| What is in v1 | `docs/product/vision-and-scope.md` |
| Behaviour, rules, thresholds, alerts | `docs/product/behaviour-rules.md` |
| Colours, type, spacing | `docs/design/design-tokens.md` |
| UI parts, screens, states | `docs/design/components.md`, `docs/design/screens-and-states.md` |
| Languages, plurals, formats | `docs/design/localization.md` |
| Accessibility | `docs/design/accessibility.md` |
| Data storage, sync, import | `docs/engineering/data-model.md`, `docs/engineering/sync-and-import.md` |
| Solution structure and rules | `docs/engineering/architecture.md` |
| WPF choices and risks | `docs/engineering/wpf-mapping.md` |
| How to do common tasks | `docs/guides/` |
| What to build and when | `docs/plans/roadmap.md`, `docs/plans/backlog.md` (only what is left before release; finished items are in `backlog-done.md`), `docs/plans/handoff.md` |
| Why something was decided | `docs/decisions.md` |
| What is undecided | `docs/plans/questions.md` |

The visual reference is `design/prototype/index.html`. Its data is simulated; known shortcuts are in `docs/design/README.md`. Where the prototype and the docs disagree, the docs win. `docs/engineering/csharp-instructions.md` holds C# conventions (owner-maintained; if empty, use standard .NET conventions).

## Working rules

1. **Plan first** for anything larger than a small fix: files to touch, tests to add. Wait for approval.
2. **One backlog item per branch.** Use the IDs from `docs/plans/backlog.md`.
3. **Tests with every change** to data, parsing, aggregation or rules (`docs/engineering/testing-strategy.md`).
4. **Keep docs in step.** Change behaviour, change the doc and its "Last reviewed" date in the same change. New decision: add an entry to `docs/decisions.md`.
5. **Files use invariant culture.** The UI uses the selected language.
6. **Gaps stay gaps.** Never interpolate missing data; averages use only readings that exist.
7. **When you correct the same mistake twice,** propose a rule or a decision entry so it is written down.

## Do not

- Do not weaken, delete or skip a test to make it pass. If a test fails, fix the code, not the assertion, unless the requirements changed (then change the doc first and say so).
- Do not add NuGet packages or projects, or change project references, without asking.
- Do not hard-code colours, sizes or user-visible strings. Use tokens and resources, in English and Ukrainian.
- Do not use `DateTime.Now` or static singletons.
- Do not mention a vendor outside `Deskair.Sensors` and `App/Composition`.
- Do not invent behaviour. If the docs are silent, add a question to `docs/plans/questions.md` and continue.
- Do not create summary, notes or changelog markdown files unless asked.
- Do not add comments or docstrings to code you did not change.
- Do not leave long methods filled with comments; split the work into small methods, or stub one with `throw new NotImplementedException("TODO(ID)")` and a short comment naming its spec.
- Do not edit `design/prototype/` unless the task is a design update.
- Do not read or copy the legacy prototype (`../deskair-legacy`) unless the task says so. When told to, adapt its ideas to our interfaces and tests; never copy it wholesale.

## Git

Branches `feature/<ID>-name`, `fix/name`, `docs/name`. Commits are imperative and start with the ID (`LIVE-03: add hover readout`). Every pull request follows `.github/pull_request_template.md`. See `CONTRIBUTING.md`.
