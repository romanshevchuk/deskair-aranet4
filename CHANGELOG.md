# Changelog

What changed in each Microsoft Store release of Deskair. Newest first.

Each release has two parts:

- **Store text:** the user-facing notes. Paste it into Partner Center as "What's new in this version". It must stay under 1500 characters and use plain words, with no backlog IDs or code names.
- **Details:** the backlog IDs and commits, for us.

**Rule:** every change that a user can notice adds a line under "Unreleased" in the same branch, in both parts. A change with nothing to notice (tests, docs, refactors) adds nothing.

## How to publish an update

1. Run the release checks on `main`: `dotnet build Deskair.sln`, `dotnet test tests/Deskair.Tests`, `dotnet format Deskair.sln --verify-no-changes`. A render pass of the changed screens is worth doing too (see `docs/guides/run-the-gallery.md`).
2. Pick the version: one higher than the last row in "Released" below. It has four numbers and the last one is always 0 (for example `0.1.5.0`). Check it against the package of the live submission in Partner Center (Packages); the Store refuses a version it already has.
3. Build the upload package from that commit: `.\packaging\build-msix.ps1 -Version 0.1.5.0`. The result is `artifacts\msix\Deskair_0.1.5.0_x64.msix`. Do not sign it with the test certificate; the Store signs it.
4. In Partner Center, open Deskair and choose **Start update**. Then:
   - **Packages:** remove the old package and upload the new `.msix`.
   - **Store listings → English → What's new in this version:** paste the "Store text" of "Unreleased".
   - Leave everything else as it is, and submit.
5. In this file, rename "Unreleased" to `0.1.5 · <date submitted>`, fill in the commit, and add a new empty "Unreleased" section above it.
6. Tag the commit (`git tag v0.1.5` and `git push origin v0.1.5`) and add a row to "Released".
7. When the Store says the update is live, add the date to its row.

## Released

| Version | Package | Commit | Submitted | Live |
|---|---|---|---|---|
| 0.1.5 | `0.1.5.0` | `76a6421` | 2026-11-23 | 2026-11-23 |
| 0.1.4 | `0.1.4.0` | `d219a40` or later (not recorded) | 2026-10-20 | 2026-10-20 or later |
| 0.1.3 | `0.1.3.0` | `1e64e94` | 2026-10-20 | 2026-10-20 |

0.1.4 was Store submission 2: BLE-10 only (Deskair connects to the sensor only while syncing; new "Sync history" button). It was found in Partner Center on 2026-11-23, when a second `0.1.4.0` upload was refused.

## Unreleased

Nothing yet.

## 0.1.5 · 2026-11-23

### Store text

```text
Works with the phone app
• If the sensor's broadcasts stop, Deskair starts listening again by itself.

Window and tray
• The window opens where you left it, at the same size, maximized or not.
• "Open Deskair" in the tray menu now brings back a minimized window.
• The tray icon shows the air quality again as soon as the sensor reconnects.

Fixes
• Readings are no longer lost while a data file is open in Excel or OneDrive.
• Phone exports with month-first (US) dates now import correctly.
• When Smart Home integration is off in the Aranet Home app, Deskair says so and shows how to turn it on.
• Settings has a link to the log folder, to help when you report a problem.

Clearer words and numbers
• Alerts come first in Settings and show their limits (1000 and 1400 ppm).
• The room name in the header no longer has a coloured dot that looked like a connection light.
• The tip under the Day chart says when it describes your usual day.
• A comfort label always matches the number shown (50 % is comfortable).
• Temperature and humidity rows are marked "least comfortable" instead of "worst".
• "Deep sleep" is now "Middle of the night".

Faster and smaller
• Starts faster, and the download is about 9 MB smaller.
```

### Details

| Area | IDs and commits |
|---|---|
| Sensor shared with the phone app | BLE-10 follow-up (`8076fde`), BLE-11 (`9b49ae6`); BLE-10 itself shipped in 0.1.4 |
| Window and tray | Window place, D-0031 (`657f665`, `0972a55`); restore a minimized window (`aca7638`); tray icon after reconnect (`08d9e8f`) |
| Fixes | FIX-02 log folder (`59163e2`), FIX-03 export date order (`844acb3`), FIX-04 Smart Home step (`d21148e`, `d621280`, `74edefc`), FIX-05 locked month file (`000d189`) |
| UX review, wave 1 | UX-01, UX-03, UX-04, UX-06, UX-08 |
| UX review, Settings, History and header | UX-30 alert labels, UX-34 Settings order, UX-22 "least comfortable", UX-39 no header dot (D-0034) |
| Size and speed | TIDY-01, SIZE-02 (.msix 69.2 → 60 MB), PERF-01 to PERF-05 |
| Small | History grid end column grows only when a tag needs it (`c6de091`) |
