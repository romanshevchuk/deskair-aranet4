---
paths:
  - "src/Deskair.Services/**"
---
<!-- Last reviewed: 2026-10 -->

# Services rules

Workflows and the interfaces they need from outside. `net10.0`. May depend on Domain.

- Never: WPF, Windows APIs, file access, a concrete sensor, user-visible text.
- Contracts live here: `ISensorProvider`, `ISensor`, `IBleConnection`, `IReadingStore`, `INotifier`, `ISettingsStore`, platform interfaces.
- Async methods end in `Async` and take a `CancellationToken`. Shutting down a sensor cancels its sync and live loop.
- Inject `TimeProvider`; never `DateTime.Now`.
- Output structured data (a zone changed, N days missing), never display strings.
- Tests use fakes for store, clock, notifier and sensor.
- Specs: `docs/engineering/architecture.md` sections 5 and 6, `docs/product/behaviour-rules.md`.
