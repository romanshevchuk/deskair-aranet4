# Questions raised while building the skeleton

*Last reviewed: 2026-10*

Questions the docs did not answer. A card that depends on an open question names it (for example `questions.md Q6`). Older product questions are in [open-questions.md](open-questions.md). Decisions that were made are recorded in the doc they change and in [../decisions.md](../decisions.md); they are only listed here so the numbers in older cards still make sense.

## Open

| # | Question | Where it came up | Assumption used meanwhile |
|---|---|---|---|
| Q7 | Phone export date order and the repeated autumn hour. A real export (tests/Deskair.Tests/TestData) has the header `Time(DD/MM/YYYY H:mm:ss)`, and the Aranet help page says the file uses the time format of the app settings, so the header names the order. Not yet built: read the order from the header (and ask only when the header is missing or unreadable); flag the repeated autumn hour in the preview. | sync-and-import.md rules 2 and 3; `PhoneExportRow` (IMPORT-02a) | Day-first only; the repeated hour maps to its first occurrence; nothing is flagged. Low priority: the preview shows the first and last date. |
| Q13 | If Windows Do Not Disturb cannot be read, should alerts be treated as allowed or paused? | behaviour-rules.md "Alerts"; `DoNotDisturbMonitor` (ALERT-03a) | Treated as not active (alerts allowed). |
| Q14 | Title bar: confirm in spike S3 that `WindowChrome` (caption height 52, native caption buttons kept) gives the prototype's header with snap layouts on Windows 11 and an acceptable result on Windows 10. | wpf-mapping.md "Riskiest pieces"; `BackdropService` (WIN-01a), `HeaderBar` (SHELL-01a) | The plain native title bar until S3 is done. |
| Q27 | What does the user see when the local history cannot be read (locked or corrupted file, missing data folder)? Today the Live chart keeps its last points and a warning is logged; a failure on first load is thrown to the caller. | architecture.md section 11 "Errors"; `LiveViewModel` (LIVE-00a) | Nothing new is shown. Add a "can't read history" state with the STATE cards. |
| Q30 | Footer signal wording: no doc gives the limits for "Signal good / fair / weak". | `FooterViewModel` (SHELL-01b) | Good at -60 dBm or stronger, fair down to -75 dBm, weak below. Tune with real Aranet4 readings (spike S1). |
| Q31 | Not-connected state on Live: the full-screen offline view (STATE-01b) is not built. Until then Live keeps the last values, the footer says "Not connected" and the stale banner only shows for Stale. | `LiveView` (LIVE-00b); screens-and-states.md "Live conditions" | The footer text and the grey dot carry the state. |
| Q32 | A connected Aranet4 does not report its signal strength (only its advertisements carry one), so the footer shows no signal for it. Should the scanner pass the last advertisement signal on to the session? | `Aranet4Sensor.ConnectAsync` (ARANET-03a); `FooterViewModel` | `SensorStatus.SignalDbm` is null and `SignalStrength` is false for the Aranet4. The footer shows no signal text. |
| Q33 | Older Aranet4 firmware may expose its characteristics under the service `f0cd1400-95da-4f4b-9ac8-aa55d312af0c` instead of `0000fce0-...`. Our sensor (firmware 2.0.7) uses `0000fce0`. Is older firmware supported? | `Aranet4Protocol.LegacyServiceId`; spike S1 | Only `0000fce0` is used. An older sensor fails to connect with a clear error. |
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
| Q24 | The advice text makes no time estimate. It changes with the CO₂ trend (falling, steady, rising); the title stays a fixed suggestion. | D-0015; localization.md "Advice_*" |
| Q25 | Mini cards show the trend ("Steady", "Rising", "Falling"). Temperature and humidity show a "Too cool / warm / dry / humid · ideal a–b" label instead only when outside the ideal range. Pressure shows the trend only. `MiniCardsViewModel.Update` takes the selected metric. | D-0015; localization.md "Mini_*" |
| Q26 | History windows: Day is the last 24 h; Week, Month and Year start at local midnight 6, 29 and 364 days before today and end now (today is the last, clipped bucket); Week groups by day. Day + Night is meant to show the sleep card (SLEEP-01a), not a filtered line, so the Day line ignores the Hours filter until that card exists. | `HistoryViewModel` (HIST-00a); screens-and-states.md "History controls" |
| Q29 | `FlyoutViewModel.Update` takes `(reading, recent, now, roomName)` and shares the zone, trend and advice logic with `HeroViewModel` through `AirSummaryBuilder`. | `FlyoutViewModel` (TRAY-02c) |
| Q28 | Export CSV uses the device selected in the header and the full range. Import and export dialogs go through a small `IFileDialogs` service (SET-01g2, not built yet). | `DataSettingsViewModel` (SET-01g2) |
| Q4 | The selected device is remembered between launches in `settings.json` (`AppSettings.SelectedDevice`); `DeviceRegistry.LoadAsync(preferred)` restores it when the device is still known. | screens-and-states.md "Settings" |
| Q6 | The temperature unit (°C / °F) is a setting (`AppSettings.TemperatureUnit`), shown through `MetricFormatter`. Stored readings and exported files stay in °C. Pressure unit and read interval are not settings. Keep in tray and Show CO₂ on tray icon are added with their own cards. | screens-and-states.md "Settings" |
