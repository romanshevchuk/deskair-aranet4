# Testing strategy

*Last reviewed: 2026-10*

One test project, `tests/Deskair.Tests`, with folders per source project (see `architecture.md`, section 12), plus dependency-rule tests.

| Level | What | Notes |
|---|---|---|
| Unit | Aggregation, coverage, merge and dedupe, trend rule, plural helper, import parser, CSV round trip | Pure logic, no UI. Cover daylight-saving days and empty periods. One test project, `Deskair.Tests`, with folders per source project (see `architecture.md`, section 12) |
| Golden files | Real phone export (thousands of rows) and a sensor sync capture | Keep small samples in the repo |
| Integration | Store reads and writes monthly files; app start with an existing folder | Use temp folders |
| UI | Smoke tests for main flows; visual comparison with the prototype | Optional automation (for example FlaUI) |
| Manual matrix | Windows 10 and 11; DPI 100, 125, 150, 200; light, dark, High Contrast; English and Ukrainian; one and two monitors; taskbar on each edge | Run before each release |
| Soak | Run for a week with a real sensor; check memory, sync, gaps | Before Store submission |
| Accessibility | Keyboard-only pass; Narrator pass | See `docs/design/accessibility.md` |
