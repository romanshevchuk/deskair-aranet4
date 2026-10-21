---
paths:
  - "src/Deskair.App/Ui/**"
---
<!-- Last reviewed: 2026-10 -->

# UI rules

- One control per folder: `Controls/<Name>/<Name>.cs` and `<Name>.xaml`, template registered in `Themes/Generic.xaml`. Guide: `docs/guides/add-a-control.md`.
- Use `DynamicResource` tokens only; no literal colours. No business logic; code-behind only for view-only concerns (focus, hit tests).
- XAML about 120 lines at most; split into child controls first.
- Charts are `ChartBase` plus small layer classes. They receive plain data from `ViewModels/ChartModels` and never calculate analytics.
- Every control appears in the gallery in light, dark, English and Ukrainian, and works with keyboard and Narrator (`docs/design/accessibility.md`).
- Reference: `docs/design/components.md`.
