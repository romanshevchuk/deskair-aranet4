---
paths:
  - "src/Deskair.Storage/**"
---
<!-- Last reviewed: 2026-10 -->

# Storage rules

Files on disk. `net10.0`. May depend on Services and Domain.

- Never: UI, Bluetooth.
- Raw readings in monthly CSV files; everything else is derived. Format and merge rule: `docs/engineering/data-model.md`.
- Always the invariant culture and UTC timestamps.
- History is append-only (`docs/decisions.md` D-0013): never delete, change or rewrite a reading. The first valid reading for an interval slot stays; later ones, from sensor or import, are ignored.
- Append whole lines to the month's file; ignore a half-written last line when loading. Only `device.json` and settings are rewritten, through a temp file and a rename.
- Tests use temporary folders and golden CSV files, and cover a half-written last line.
