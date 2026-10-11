# Add a setting

*Last reviewed: 2026-10*

1. Add the property with a default to `AppSettings` (`Services/Settings`).
2. Persist it in the versioned settings JSON (`Storage/Settings`); old files without it must load with the default.
3. Expose it in the matching Settings section view model (`App/ViewModels/Settings`).
4. Add the row to the Settings view with `SettingsRow` and the right control. Add its strings (see `add-a-translation.md`).
5. React to `SettingsChanged` where the setting matters.
6. Update the Settings table in `docs/design/screens-and-states.md` and its "Last reviewed" date.
7. Done when: a test covers the default and a round trip, and the setting works after restarting the app.
