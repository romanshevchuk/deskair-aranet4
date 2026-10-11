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
- Never delete an existing reading. Merge by interval slot; sensor values win over imports.
- Append to the current month's file; when rewriting, write a temp file then rename.
- Tests use temporary folders and golden CSV files, and cover interrupted writes.
