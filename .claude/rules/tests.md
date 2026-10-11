---
paths:
  - "tests/**"
---
<!-- Last reviewed: 2026-10 -->

# Test rules

- A failing test is a signal. Fix the code, not the assertion, unless the requirements changed.
- Test the public surface, not internals. One concept per test, with a descriptive name (`Classify_returns_NoData_when_nothing_present`).
- Prefer fast unit tests. Use fakes for clock, store, notifier and sensor.
- Cover the error path and empty input, and daylight-saving days for calendar logic.
- Tests for unfinished work are skipped with reason `TODO(ID)`; unskip when implementing.
- Folders mirror the source projects; `Architecture/DependencyRulesTests` checks the dependency rules.
- Strategy: `docs/engineering/testing-strategy.md`.
