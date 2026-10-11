---
paths:
  - "**/*.resx"
  - "src/Deskair.App/Localization/**"
---
<!-- Last reviewed: 2026-10 -->

# Localization rules

- Every English key needs a Ukrainian key, and the other way round. Key format `Area_Element_Purpose`.
- No sentence joining. Whole strings with named placeholders (`{count}`, `{room}`, `{time}`).
- Counts need plural keys: English one and other; Ukrainian one, few, many.
- Do not translate "CO₂", unit symbols, "Aranet4" or "Deskair".
- Mark uncertain Ukrainian text with a comment for native review.
- Details: `docs/design/localization.md`, `docs/guides/add-a-translation.md`.
