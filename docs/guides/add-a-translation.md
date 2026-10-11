# Add or change a UI string

*Last reviewed: 2026-10*

1. Add the key to `Strings.resx` (English) and `Strings.uk.resx` (Ukrainian). Key format `Area_Element_Purpose`.
2. No sentence joining. Use whole strings with named placeholders (`{count}`, `{room}`, `{time}`).
3. For counts, add plural keys (English: one and other; Ukrainian: one, few, many). Rules: `docs/design/localization.md`.
4. Do not translate "CO₂", unit symbols, "Aranet4" or "Deskair".
5. Mark Ukrainian text you are unsure about with a comment so a native speaker reviews it.
6. Use the string through `ILocalizer` or `{loc:T Key}`. Never hard-code text.
7. Done when: the key parity test passes (every English key has a Ukrainian key) and the screen fits with the longer Ukrainian text.
