# TODO index

*Last reviewed: 2026-10*

Every TODO card in the code, with its size and the model that should take it. Format and protocol: [m1-skeleton-brief.md](m1-skeleton-brief.md) sections 4 and 6. Update the status in the same change that removes the card.

- **Size:** S = a few lines of one method; M = one class or a small group of methods; L = timing, threading, Windows APIs or drawing.
- **Status:** `todo`, `doing`, `done`.
- **Test:** `skipped test` means a skipped test with reason `TODO(ID)` already exists; unskip it and make it pass. `manual / gallery` means the card is done when the gallery or a manual check shows it.
- Controls, views and Windows glue are stubs too: an empty control template, an empty view, or a method that throws `NotImplementedException("TODO(ID)")`. The card sits above the stub (in the XAML file for templates and views; for controls, in both the `.cs` and the `.xaml` file).

Totals: 51 cards. Sizes: L: 12, M: 24, S: 15. Suggested models: haiku: 25, sonnet: 26.

| ID | File | Size | Suggested model | Status | Test |
|---|---|---|---|---|---|
| ALERT-02a | src/Deskair.App/Platform/Notifications/ToastNotifier.cs | L | sonnet | todo | manual / gallery |
| ALERT-03a | src/Deskair.App/Platform/Notifications/DoNotDisturbMonitor.cs | M | sonnet | todo | manual / gallery |
| ARANET-01a | src/Deskair.Sensors/Aranet4/Aranet4Protocol.cs | M | sonnet | todo | manual / gallery |
| ARANET-02a | src/Deskair.Sensors/Aranet4/Aranet4Parser.cs | M | sonnet | todo | skipped test |
| ARANET-02b | src/Deskair.Sensors/Aranet4/Aranet4Parser.cs | L | sonnet | todo | skipped test |
| ARANET-03a | src/Deskair.Sensors/Aranet4/Aranet4Sensor.cs | L | sonnet | todo | skipped test |
| ARANET-03b | src/Deskair.Sensors/Aranet4/Aranet4Sensor.cs | M | sonnet | todo | skipped test |
| ARANET-03c | src/Deskair.Sensors/Aranet4/Aranet4Sensor.cs | L | sonnet | todo | skipped test |
| ARANET-04a | src/Deskair.Sensors/Aranet4/Aranet4Provider.cs | M | sonnet | todo | skipped test |
| BLE-01a | src/Deskair.App/Platform/Bluetooth/WindowsBleScanner.cs | L | sonnet | todo | manual / gallery |
| BLE-02a | src/Deskair.App/Platform/Bluetooth/WindowsBleConnection.cs | L | sonnet | todo | manual / gallery |
| BLE-02b | src/Deskair.App/Platform/Bluetooth/WindowsBleConnection.cs | M | sonnet | todo | manual / gallery |
| BLE-02c | src/Deskair.App/Platform/Bluetooth/WindowsBleConnection.cs | L | sonnet | todo | manual / gallery |
| BLE-03a | src/Deskair.App/Platform/Bluetooth/BluetoothRadioMonitor.cs | M | sonnet | doing | manual check (implemented, needs a Bluetooth toggle test) |
| CHART-14 | src/Deskair.App/Ui/Charts/Layers/RangeBarLayer.cs | M | haiku | todo | manual / gallery |
| CHART-15 | src/Deskair.App/Ui/Charts/Layers/AverageLineLayer.cs | S | haiku | todo | manual / gallery |
| CHART-16 | src/Deskair.App/Ui/Charts/Layers/WeekendShadingLayer.cs | S | haiku | todo | manual / gallery |
| CHART-17 | src/Deskair.App/Ui/Charts/Layers/GapHatchLayer.cs | M | sonnet | todo | manual / gallery |
| CHART-21 | src/Deskair.App/Ui/Charts/HistoryChart.cs | L | sonnet | todo | manual / gallery |
| CHART-22 | src/Deskair.App/Ui/Charts/TypicalDayChart.cs | M | sonnet | todo | manual / gallery |
| CTRL-06 | src/Deskair.App/Ui/Controls/EmptyStateCard/EmptyStateCard.cs | S | haiku | todo | manual / gallery |
| CTRL-10 | src/Deskair.App/Ui/Controls/CoverageChip/CoverageChip.cs | S | haiku | todo | manual / gallery |
| CTRL-11 | src/Deskair.App/Ui/Controls/ZoneShareBar/ZoneShareBar.cs | S | haiku | todo | manual / gallery |
| CTRL-12 | src/Deskair.App/Ui/Controls/InsightBox/InsightBox.cs | S | haiku | todo | manual / gallery |
| CTRL-14 | src/Deskair.App/Ui/Controls/SettingsCard/SettingsCard.cs | S | haiku | todo | manual / gallery |
| CTRL-15 | src/Deskair.App/Ui/Controls/SettingsRow/SettingsRow.cs | S | haiku | todo | manual / gallery |
| FIRST-01b | src/Deskair.App/Ui/Views/Onboarding/OnboardingView.xaml | L | sonnet | todo | manual / gallery |
| HIST-00b | src/Deskair.App/Ui/Views/History/HistoryView.xaml | S | haiku | todo | manual / gallery |
| HIST-01c | src/Deskair.App/Ui/Views/History/HistoryToolbar.xaml | M | haiku | todo | manual / gallery |
| HIST-05d | src/Deskair.App/Ui/Views/History/TypicalDayCard.xaml | S | haiku | todo | manual / gallery |
| HIST-06c | src/Deskair.App/Ui/Views/History/StatsCard.xaml | M | haiku | todo | manual / gallery |
| LOG-01a | src/Deskair.App/Composition/RollingFileLoggerProvider.cs | M | haiku | todo | skipped test |
| SET-01g2 | src/Deskair.App/ViewModels/Settings/DataSettingsViewModel.cs | M | haiku | todo | test to write |
| SET-01i | src/Deskair.App/Ui/Views/Settings/SettingsView.xaml | S | haiku | todo | manual / gallery |
| SET-01j | src/Deskair.App/Ui/Views/Settings/AppearanceSettingsView.xaml | S | haiku | todo | manual / gallery |
| SET-01k | src/Deskair.App/Ui/Views/Settings/AlertsSettingsView.xaml | S | haiku | todo | manual / gallery |
| SET-01l | src/Deskair.App/Ui/Views/Settings/DevicesSettingsView.xaml | M | haiku | todo | manual / gallery |
| SET-01m | src/Deskair.App/Ui/Views/Settings/DataSettingsView.xaml | M | haiku | todo | manual / gallery |
| SET-01n | src/Deskair.App/Ui/Views/Settings/StartupSettingsView.xaml | S | haiku | todo | manual / gallery |
| SET-03a | src/Deskair.App/Platform/Startup/StartupRegistration.cs | M | sonnet | todo | manual / gallery |
| SET-03b | src/Deskair.App/Platform/Startup/StartupRegistration.cs | M | sonnet | todo | manual / gallery |
| SLEEP-01a | src/Deskair.App/Ui/Views/History/SleepCard.xaml | M | haiku | todo | manual / gallery |
| STATE-01b | src/Deskair.App/Ui/Views/States/OfflineView.xaml | M | haiku | todo | manual / gallery |
| STATE-01c | src/Deskair.App/Ui/Views/States/EmptyHistoryView.xaml | S | haiku | todo | manual / gallery |
| SYNC-03a | src/Deskair.App/Platform/Power/PowerEvents.cs | M | sonnet | todo | manual / gallery |
| TRAY-01a | src/Deskair.App/Platform/Tray/TrayIconHost.cs | L | sonnet | todo | manual / gallery |
| TRAY-01b | src/Deskair.App/Platform/Tray/TrayIconRenderer.cs | L | sonnet | todo | manual / gallery |
| TRAY-02a | src/Deskair.App/Platform/Tray/FlyoutPositioner.cs | M | sonnet | todo | skipped test |
| TRAY-02b | src/Deskair.App/Ui/Views/Flyout/FlyoutView.xaml | M | haiku | todo | manual / gallery |
| WIN-01a | src/Deskair.App/Platform/Theming/BackdropService.cs | L | sonnet | todo | manual / gallery |
| WIN-02a | src/Deskair.App/Composition/SingleInstance.cs | M | sonnet | todo | manual / gallery |
