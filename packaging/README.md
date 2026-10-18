# Packaging (Microsoft Store)

The step-by-step plan, the reasons and the Store policy notes are in [`docs/plans/store-packaging.md`](../docs/plans/store-packaging.md). This folder holds the files that make the package. It is not part of `Deskair.sln`.

| File | What it is |
|---|---|
| `AppxManifest.xml` | The package's ID card: identity from Partner Center, capabilities (`bluetooth`, `runFullTrust`), toast activation, startup task, languages |
| `Assets/` | Logos (generated; `StoreListing-300x300.png` is for Partner Center, not the package) |
| `new-logos.ps1` | Redraws the logos (only to change them) |
| `build-msix.ps1` | Publishes the app and packs `artifacts\msix\Deskair_<version>_x64.msix` (needs the Windows 11 SDK) |
| `sign-test.ps1`, `trust-test-cert.ps1` | Phase 3 only: sign with a test certificate and trust it, to install the package on this PC |

## Build

```powershell
.\packaging\build-msix.ps1 -Version 0.1.0.0
```

The version has four numbers and the last must stay `0` (the Store uses it). Every upload to the Store needs a higher version than the one before.

## Try it on this PC (phase 3)

```powershell
.\packaging\sign-test.ps1 -Package artifacts\msix\Deskair_0.1.0.0_x64.msix
# once, in an administrator PowerShell window:
.\packaging\trust-test-cert.ps1
# then, in a normal window:
Add-AppxPackage artifacts\msix\Deskair_0.1.0.0_x64.msix
```

Remove again: `Get-AppxPackage RomanShevchuk.Deskair | Remove-AppxPackage`, and `.\packaging\trust-test-cert.ps1 -Remove` in an administrator window. Close the unpackaged Deskair first; two copies fight over the tray and the single-instance lock.

## Things to know

- The package is self-contained (it carries .NET), so it is large (about 70 MB). There is no shared .NET 10 package to depend on.
- Identity values must match Partner Center exactly: `RomanShevchuk.Deskair`, `CN=C6F2FCB9-D5F4-41BE-9616-7B99EB94EFA1`, `Roman Shevchuk` (product identity page of the reserved name).
- The toast class id and the startup task id in the manifest must match `ToastIdentity.ActivatorClassId` and `PackagedStartupRegistration.TaskId`.
- Logos have the exact sizes, with no scale variants. A sharper taskbar icon at high scaling would need `resources.pri` (MakePri); not done yet.
- In the packaged app, writes under `%LOCALAPPDATA%` (logs, the toast pictures, the data-folder choice) are redirected by Windows to the package's own folder and removed on uninstall. Readings are in `Documents\Deskair` and are not affected.
