---
paths:
  - "src/Deskair.App/**"
---
<!-- Last reviewed: 2026-10 -->

# App rules

The WPF app. `net10.0-windows10.0.19041.0`. May depend on all projects; only `Composition/` may use `Deskair.Sensors`.

- Never business rules (they belong in Domain or Services).
- Folders: `ViewModels/`, `Localization/`, `Ui/` (Theme, Controls, Charts, Views, Markup), `Platform/` (Windows glue behind Services interfaces), `Composition/` (startup and DI).
- All user-visible text comes from `Localization/` resources in English and Ukrainian.
- Colours, sizes and fonts come from the theme tokens (`docs/design/design-tokens.md`).
- `Ui/` and `ViewModels/` never use `Storage`, `Platform` or `Sensors`.
