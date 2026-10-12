# Questions raised while building the skeleton

*Last reviewed: 2026-10*

Questions the docs did not answer. A card that depends on an open question names it (for example `questions.md Q6`). Older product questions are in [open-questions.md](open-questions.md). Decisions that were made are recorded in the doc they change and in [../decisions.md](../decisions.md); they are only listed here so the numbers in older cards still make sense.

## Open

| # | Question | Where it came up | Assumption used meanwhile |
|---|---|---|---|
| Q4 | Is the selected device remembered between launches, and where (settings or device.json)? | `DeviceRegistry.Select` (DEV-01b); `AppSettings` has no field for it | Not remembered; the first known device is selected. |
| Q6 | What is the rest of the settings list and its stored shape? Notify at Elevated / High and quiet hours (on, start, end) are now in `AppSettings`; the settings file, the Settings view models and views for them are still to do (SET-02, SET-01e, SET-01k). | screens-and-states.md "Settings"; `AlertsSettingsViewModel` (SET-01e) | Language, theme, alerts on or off, Notify at Elevated and High, quiet hours exist. Temperature unit, keep in tray, show CO2 on tray, data folder are added when their cards need them (docs/guides/add-a-setting.md). |
| Q7 | Phone export: should the repeated autumn hour be flagged to the user in the import preview, and should the app ask the user to confirm day-first or month-first when no day in the file is above 12? The import never guesses the order: it reads day-first only. | sync-and-import.md rules 2 and 3; `PhoneExportRow` (IMPORT-02a) | Readings in the repeated hour map to its first (summer time) occurrence; a time skipped by the spring change uses the offset from before it. Nothing is flagged yet, and the day-first prompt is not built. |
| Q13 | If Windows Do Not Disturb cannot be read, should alerts be treated as allowed or paused? | behaviour-rules.md "Alerts"; `DoNotDisturbMonitor` (ALERT-03a) | Treated as not active (alerts allowed). |
| Q14 | Title bar: confirm in spike S3 that `WindowChrome` (caption height 52, native caption buttons kept) gives the prototype's header with snap layouts on Windows 11 and an acceptable result on Windows 10. | wpf-mapping.md "Riskiest pieces"; `BackdropService` (WIN-01a), `HeaderBar` (SHELL-01a) | The plain native title bar until S3 is done. |
| Q24 | How is `{min}` in `Advice_Elevated_Text` and `Advice_High_Text` ("should drop below 1,000 ppm in about {min} min") calculated? No doc gives a formula, and the hero card must not invent one. | localization.md "Advice"; `HeroViewModel` (LIVE-01c) | LIVE-01c is not started until the owner decides. |
| Q25 | Mini cards: `MiniCardsViewModel.Update(recent)` has no selected metric, so it cannot pick "the three non-selected metrics". What are the subtitle texts and their keys? The prototype shows "Steady this hour" and "Comfortable · ideal 30–50%"; neither is in `Strings.resx` or `localization.md`. | components.md "Metric mini card"; `MiniCardsViewModel` (LIVE-02b) | LIVE-02b is not started until the owner decides. |
| Q26 | History windows: Day is the last 24 h. For Week, Month and Year the docs say 7, 30 and 365 days but not how they align. Day range ignores the Hours filter (a line of raw readings), and Week always groups by day. | screens-and-states.md "History controls"; `HistoryViewModel` (HIST-00a) | Week, Month and Year start at local midnight 6, 29 and 364 days before today and end now, so today is the last, clipped bucket. Day line ignores the Hours filter. |
| Q17 | Strings added by the skeleton need owner wording and a native Ukrainian review: Sensor_*_Step*, Footer_NoSensor, Nav_Main, Language_*, Gallery_*, Import_PhoneExport_Name. Cards ask for further keys (Coverage_Chip). | localization.md; Strings.resx | Short placeholder wording in both languages. |

## Decided

| # | Decision | Recorded in |
|---|---|---|
| Q1 | Trend compares the last 6 minutes with the 6 minute window that ends 30 minutes before now; with no readings in either window the trend is Steady. | behaviour-rules.md "Live values" |
| Q2 | History is append-only: the first valid reading for a slot stays; later readings for it (sensor or import) are ignored. | D-0013; data-model.md; behaviour-rules.md "Sync and data" |
| Q3 | Order when several connection states apply: First run, Not connected, Stale, Low battery. Stale starts after 3 missed intervals. | behaviour-rules.md "Connection states" |
| Q8 | Excel files are read with ExcelDataReader 3.9.0 (MIT, forward-only, published 2026-06-16). Tray and toast libraries are still to be chosen when their cards start (ask first). | card IMPORT-02b; open-questions.md 4 |
| Q10 | Focus ring: 2 px, #2563C9 in light and #6E9BE0 in dark (the prototype uses #2563C9). | Colors.*.xaml (`Focus.Ring`) |
| Q11 | Toggle knob 18 px with 2 px inset, status dot 8 px, pill fully rounded with 9 / 4 / 11 / 4 padding, segmented control 3 px track padding and 14 / 5 px item padding, taken from the prototype. | Spacing.xaml, Radii.xaml |
| Q12 | Hero padding top 22, sides 24, bottom 20; advice panel radius 12; banner radius 10; insight box 10 (prototype does not show it). | Spacing.xaml, Radii.xaml |
| Q15 | Banner uses the Elevated advice colours; battery fill uses the zone segment colours; primary button is filled Text.Primary with white text, secondary is outlined; selected metric card is outlined in Text.Primary; caption buttons 46 px, hover `Caption.Hover`, close hover `Caption.CloseHover` (#C42B1C). All from the prototype. | cards CTRL-05, 09, 16, 17, 04; Colors.*.xaml |
| Q19 | xunit v3 on Microsoft.Testing.Platform. | D-0014 |
| Q16 | Segmented control stays a restyled single-selection `ListBox`: arrow keys move the selection, it needs no custom selection code, and screen readers announce a selectable list. A RadioButton group would need extra code for the same result. | components.md |
| Q18 | Section labels use normal case and normal letter spacing. The uppercase and .06 em spacing came from the HTML prototype and WPF has neither. | design-tokens.md "Typography" |
| Q20 | Temperature, humidity and pressure trend: the change is newest minus oldest reading in the last hour, and the trend is Steady unless those readings cover at least 30 minutes (so start-up does not show an hourly trend from a few minutes of data). | behaviour-rules.md "Live values" |
| Q21 | Grouping: days and months follow local midnights; a week is 7 local days counted back from the end of the range (as in the prototype); the first and last bucket are clipped to the range, so today or the current month is judged only on the readings expected so far; `Year` is not a grouping and throws. The test `Group_by_day_gives_one_bucket_per_local_day_with_no_holes` had an end time one hour past the daylight-saving shifted midnight; its end is now 21:00 UTC and a test covers the clipped day. | behaviour-rules.md "Aggregation" rule 2; `BucketGroupingTests` |
| Q22 | Not connected starts after the larger of 10 minutes and 5 intervals; Stale after 3 intervals. At the usual 2 minute interval this is the documented 10 minutes. | behaviour-rules.md "Connection states" |
| Q5 | Quiet hours with equal start and end mean no quiet hours (nothing to mute). | behaviour-rules.md "Alerts"; `AlertEngine.IsQuietHour` |
| Q9 | A device folder is `<providerId>-<address>` under `devices/`, with characters that are not valid in file names replaced by `_`; the device id is read from `device.json`. | data-model.md "Device folder"; `DeviceFolders` |
| Q23 | How does the import tell a .xlsx file from text? | sync-and-import.md "Import from the phone app"; `PhoneExportImporter.ReadAsync` (IMPORT-02b) | By content: an .xlsx file is a zip archive, so the importer checks the first two bytes and reads the rest as text otherwise. The file name is not used. |
