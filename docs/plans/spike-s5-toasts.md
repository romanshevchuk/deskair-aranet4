# Spike S5: toasts from an unpackaged app

*Last reviewed: 2026-10*

Question (from [spikes.md](spikes.md)): toast setup for packaged and unpackaged builds. A minimal toast with Snooze and Open actions; the actions must work in both builds with one code path.

> **The spike code was removed on 2026-10-13** (last present in commit `07d4f12`, folder `src/Deskair.App/Composition/Spikes/`). The commands in this note no longer exist; the note stays as the record of what was tried and found. The result is the real toast code in `Platform/Notifications`. The spike used its own app identity `Deskair.SpikeS5`; if a `--cleanup` was never run on some machine, delete `HKCUSoftwareClassesDeskair.SpikeS5` and the COM class `{5C7E6B0A-...}` by hand (none are left on the owner's machine, checked 2026-10-13).

## Choosing the approach (researched 2026-10-13)

| Option | Verdict | Why |
|---|---|---|
| `CommunityToolkit.WinUI.Notifications` 7.1.2, `Microsoft.Toolkit.Uwp.Notifications` 7.1.3 | **Rejected: legacy** | The repository was [archived on 25 Feb 2026](https://github.com/CommunityToolkit/WindowsCommunityToolkit); the [NuGet page](https://www.nuget.org/packages/communitytoolkit.winui.notifications/) points to the Windows App SDK; the last release is from 2021. |
| In-app popup libraries (Notification.Wpf and similar) | **Rejected** | They draw WPF windows, not Windows toasts: no notification centre, no Do Not Disturb, nothing when the app is closed. |
| Windows App SDK `AppNotificationManager` (`Microsoft.WindowsAppSDK` 2.5.1) | **Fallback** | [Microsoft's recommended API](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/) for WPF and unpackaged apps and actively developed. Unpackaged use needs the Windows App SDK runtime installed or a self-contained build ([deployment overview](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/deploy-overview)), and two open issues hit exactly this case: [#6774](https://github.com/microsoft/WindowsAppSDK/issues/6774) (self-contained unpackaged: `Register()` throws and button clicks never arrive) and [#6821](https://github.com/microsoft/WindowsAppSDK/issues/6821) (the unpackaged sample shows no toast on one machine). Not verified: that 2.x supports WPF on .NET 10. |
| **Inbox `Windows.UI.Notifications`** (no package; our target framework already has it) | **Tried first** | Microsoft calls it "maintenance only" and prefers the Windows App SDK for new apps, but it is the platform API underneath and needs no dependency. For an unpackaged app we register the app identity and a COM activator ourselves (about 150 lines, in the spike). |

Decision of the owner (2026-10-13): try the inbox API first; ask again before adding the Windows App SDK.

## What the spike does

`--spike-s5` is throwaway code in `src/Deskair.App/Composition/Spikes/SpikeS5.cs`. It needs no package.

1. Registers, for the current user only (`HKCU\Software\Classes`): the app identity `Deskair.SpikeS5` (display name and the activator) and the COM class `{5C7E6B0A-3A52-4F5C-9E55-3D7C1B2A9F41}` with `LocalServer32` pointing at the program.
2. Registers the class factory in the running process (on a thread pool thread, so Windows can call it without a message loop).
3. Shows a toast "CO₂ is elevated · Office" with the buttons "Snooze 1 h" and "Open Deskair".
4. Waits for the click and records the arguments (`action=snooze` or `action=open`).

Every run appends to `artifacts/spike-s5/report.txt`.

## Results so far (agent, 2026-10-13, Windows 11 26200, not elevated, unpackaged)

- The first toast of a new identity works. Reading `ToastNotifier.Setting` before the first toast fails with `0x80070490` (not found); from the second run it reads `Enabled`. The real code must not depend on reading it first.
- `Show` succeeds, and the banner is reported as shown then timed out (`Dismissed: TimedOut`).
- **Program not running:** Windows starts it through the registered COM class with `-Embedding`; the class factory is registered in time; the activator receives the arguments (`action=snooze`) and the program records them. Tested by creating the class the way the notification system does, not by a real click.
- **Not yet verified:** that a real click on the toast's button makes Windows do exactly that (the toast could not be driven through UI Automation). This needs the owner's click.
- **Not tested:** the packaged (MSIX) build. It uses the same activator class and arguments; the identity and the COM class are declared in the package manifest (`windows.toastNotificationActivation` and `windows.comServer`) instead of the registry, and the notifier is created without an identity. A package project does not exist yet.

## Run it (owner)

From the repository root. Press the buttons on the real toast.

1. **Program running.** Run, then press **Snooze 1 h** within two minutes:

   ```
   dotnet run --project src/Deskair.App -- --spike-s5
   ```

   Expected last lines of `artifacts/spike-s5/report.txt`: `ACTIVATED with arguments 'action=snooze'` and `RESULT: the Snooze button reached the program.` Run again and press **Open Deskair**, and the toast body once, and note what the report says for each.
2. **Program not running.** Run, and let it exit:

   ```
   dotnet run --project src/Deskair.App -- --spike-s5 --leave
   ```

   Then press **Snooze 1 h** on the toast (open the notification centre if the banner is gone). A line `started BY WINDOWS ...` and then `ACTIVATED ... 'action=snooze'` appears in the report.
3. If a toast does not appear at all: check that Focus assist / Do Not Disturb is off and that "Deskair (toast spike S5)" is listed and on in Settings, System, Notifications.
4. **Clean up afterwards:** `dotnet run --project src/Deskair.App -- --spike-s5 --cleanup` removes the registry entries.

## Exit criteria (from spikes.md)

- [x] Actions work unpackaged, program running (owner, 2026-10-13: `ACTIVATED ... action=snooze`). Program not running: the click path was verified by calling the class as Windows does (agent); a real click is still to be confirmed.
- [ ] Actions work packaged (needs a package project; later).
- [ ] One code path: the same activator and arguments in both; only the registration differs.
