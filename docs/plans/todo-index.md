# TODO index

*Last reviewed: 2026-10*

Every TODO card in the code, with its size and the model that should take it. Format and protocol: [m1-skeleton-brief.md](m1-skeleton-brief.md) sections 4 and 6. Update the status in the same change that removes the card.

- **Size:** S = a few lines of one method; M = one class or a small group of methods; L = timing, threading, Windows APIs or drawing.
- **Status:** `todo`, `doing`, `done`.
- **Test:** `skipped test` means a skipped test with reason `TODO(ID)` already exists; unskip it and make it pass. `manual / gallery` means the card is done when the gallery or a manual check shows it.
- Controls, views and Windows glue are stubs too: an empty control template, an empty view, or a method that throws `NotImplementedException("TODO(ID)")`. The card sits above the stub (in the XAML file for templates and views; for controls, in both the `.cs` and the `.xaml` file).

Totals: 14 cards. Sizes: L: 5, M: 8, S: 1. Suggested models: haiku: 4, sonnet: 10.

| ID | File | Size | Suggested model | Status | Test |
|---|---|---|---|---|---|
| ALERT-02a | src/Deskair.App/Platform/Notifications/ToastNotifier.cs | L | sonnet | todo | manual / gallery |
| ALERT-03a | src/Deskair.App/Platform/Notifications/DoNotDisturbMonitor.cs | M | sonnet | todo | manual / gallery |
| BLE-02c | src/Deskair.App/Platform/Bluetooth/WindowsBleConnection.cs | L | sonnet | doing | manual check (implemented; the Aranet4 history does not use notifications, so it needs a sensor that does) |
| BLE-03a | src/Deskair.App/Platform/Bluetooth/BluetoothRadioMonitor.cs | M | sonnet | doing | manual check (implemented, needs a Bluetooth toggle test) |
| SET-01k | src/Deskair.App/Ui/Views/Settings/AlertsSettingsView.xaml | S | haiku | doing | manual / gallery (switches done; quiet hours times and the paused-by-Windows line wait for ALERT-03a) |
| SLEEP-01a | src/Deskair.App/Ui/Views/History/SleepCard.xaml | M | haiku | todo | manual / gallery |
| STATE-01b | src/Deskair.App/Ui/Views/States/OfflineView.xaml | M | haiku | todo | manual / gallery |
| SYNC-03a | src/Deskair.App/Platform/Power/PowerEvents.cs | M | sonnet | todo | manual / gallery |
| TRAY-01a | src/Deskair.App/Platform/Tray/TrayIconHost.cs | L | sonnet | todo | manual / gallery |
| TRAY-01b | src/Deskair.App/Platform/Tray/TrayIconRenderer.cs | L | sonnet | todo | manual / gallery |
| TRAY-02a | src/Deskair.App/Platform/Tray/FlyoutPositioner.cs | M | sonnet | todo | skipped test |
| TRAY-02b | src/Deskair.App/Ui/Views/Flyout/FlyoutView.xaml | M | haiku | todo | manual / gallery |
| WIN-01a | src/Deskair.App/Platform/Theming/BackdropService.cs | L | sonnet | todo | manual / gallery |
| WIN-02a | src/Deskair.App/Composition/SingleInstance.cs | M | sonnet | todo | manual / gallery |
