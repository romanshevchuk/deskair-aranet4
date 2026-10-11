# Questions raised while building the skeleton

*Last reviewed: 2026-10*

Questions the docs did not answer. A card that depends on an open question names it (for example `questions.md Q6`). Older product questions are in [open-questions.md](open-questions.md). Decisions that were made are recorded in the doc they change and in [../decisions.md](../decisions.md); they are only listed here so the numbers in older cards still make sense.

## Open

| # | Question | Where it came up | Assumption used meanwhile |
|---|---|---|---|
| Q4 | Is the selected device remembered between launches, and where (settings or device.json)? | `DeviceRegistry.Select` (DEV-01b); `AppSettings` has no field for it | Not remembered; the first known device is selected. |
| Q5 | Quiet hours with equal start and end: off, or all day? | screens-and-states.md "Settings"; `AlertEngine.IsQuietHour` (ALERT-01a) | Off. |
| Q6 | What is the full settings list and its stored shape? `AppSettings` holds only language, theme and alerts on or off. | screens-and-states.md "Settings"; `AlertsSettingsViewModel` (SET-01e) | Only the three fields exist. Temperature unit, notify at Elevated and High, quiet hours times, keep in tray, show CO2 on tray, data folder are added when their cards need them (docs/guides/add-a-setting.md). |
| Q7 | Phone export: how to treat the repeated hour at the autumn daylight-saving change, and when to ask the user to confirm day-first versus month-first dates? | sync-and-import.md rules 2 and 3; `PhoneExportImporter` (IMPORT-02a) | Not implemented yet. The card says to flag the repeated hour. |
| Q9 | Folder name of a device under `devices/`: how is it built from `DeviceId` (provider and address), and which characters are safe? | data-model.md (`devices/<id>/`); `MonthlyCsvStore` (STORE-01a) | `<providerId>-<address>` with characters that are not valid in file names replaced. Not implemented. |
| Q13 | If Windows Do Not Disturb cannot be read, should alerts be treated as allowed or paused? | behaviour-rules.md "Alerts"; `DoNotDisturbMonitor` (ALERT-03a) | Treated as not active (alerts allowed). |
| Q14 | Title bar: confirm in spike S3 that `WindowChrome` (caption height 52, native caption buttons kept) gives the prototype's header with snap layouts on Windows 11 and an acceptable result on Windows 10. | wpf-mapping.md "Riskiest pieces"; `BackdropService` (WIN-01a), `HeaderBar` (SHELL-01a) | The plain native title bar until S3 is done. |
| Q17 | Strings added by the skeleton need owner wording and a native Ukrainian review: Sensor_*_Step*, Footer_NoSensor, Nav_Main, Language_*, Gallery_*, Import_PhoneExport_Name. Cards ask for further keys (Coverage_Chip). | localization.md; Strings.resx | Short placeholder wording in both languages. |

## Decided

| # | Decision | Recorded in |
|---|---|---|
| Q1 | Trend compares the last 6 minutes with the 6 minute window that ends 30 minutes before now; with no readings in either window the trend is Steady. | behaviour-rules.md "Live values" |
| Q2 | History is append-only: the first valid reading for a slot stays; later readings for it (sensor or import) are ignored. | D-0013; data-model.md; behaviour-rules.md "Sync and data" |
| Q3 | Order when several connection states apply: First run, Not connected, Stale, Low battery. Stale starts after 3 missed intervals. | behaviour-rules.md "Connection states" |
| Q8 | Excel files are read with ExcelDataReader (MIT, forward-only). Tray and toast libraries are still to be chosen when their cards start (ask first). | card IMPORT-02b; open-questions.md 4 |
| Q10 | Focus ring: 2 px, #2563C9 in light and #6E9BE0 in dark (the prototype uses #2563C9). | Colors.*.xaml (`Focus.Ring`) |
| Q11 | Toggle knob 18 px with 2 px inset, status dot 8 px, pill fully rounded with 9 / 4 / 11 / 4 padding, segmented control 3 px track padding and 14 / 5 px item padding, taken from the prototype. | Spacing.xaml, Radii.xaml |
| Q12 | Hero padding top 22, sides 24, bottom 20; advice panel radius 12; banner radius 10; insight box 10 (prototype does not show it). | Spacing.xaml, Radii.xaml |
| Q15 | Banner uses the Elevated advice colours; battery fill uses the zone segment colours; primary button is filled Text.Primary with white text, secondary is outlined; selected metric card is outlined in Text.Primary; caption buttons 46 px, hover `Caption.Hover`, close hover `Caption.CloseHover` (#C42B1C). All from the prototype. | cards CTRL-05, 09, 16, 17, 04; Colors.*.xaml |
| Q19 | xunit v3 on Microsoft.Testing.Platform. | D-0014 |
| Q16 | Segmented control stays a restyled single-selection `ListBox`: arrow keys move the selection, it needs no custom selection code, and screen readers announce a selectable list. A RadioButton group would need extra code for the same result. | components.md |
| Q18 | Section labels use normal case and normal letter spacing. The uppercase and .06 em spacing came from the HTML prototype and WPF has neither. | design-tokens.md "Typography" |
