# Research: reading Windows Do Not Disturb (ALERT-03a)

*Last reviewed: 2026-10*

Question: how does an unpackaged desktop app find out that Windows Do Not Disturb is on, so the tray and Settings can say "Alerts paused by Windows"? (Windows holds native toasts by itself; this is only so Deskair can say why nothing appeared.)

## Options tried (Windows 11 build 26200, 2026-10-13)

The Do Not Disturb toggle in Settings → System → Notifications was switched with UI Automation, with the state read before and after each switch, then restored to Off.

| Way of asking | Off | On | Verdict |
|---|---|---|---|
| `FocusSessionManager.IsFocusActive` (Windows.UI.Shell, public, build 22621+) | false | false | Only reports Focus sessions, not Do Not Disturb. Also not in the app's 19041 Windows SDK projection. |
| `SHQueryUserNotificationState` (shell32, documented) | 5 (accepts notifications) | 5 | Reports full-screen and presentation states only. |
| Registry `NOC_GLOBAL_SETTING_TOASTS_ENABLED` | unset | unset | Not written by the current toggle. |
| **WNF state `WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED` (`0x0D83063EA3BF1C75`) through `NtQueryWnfStateData` (ntdll)** | 0 | 1 | **Chosen.** The only signal that followed the toggle. |

## Decision

`WindowsDoNotDisturbReader` reads the WNF state: a non-zero first data byte means on. If the call fails the answer is "unknown", and the app treats alerts as allowed (Q13). `DoNotDisturbMonitor` asks on demand for `IsActive` and looks every 3 seconds to announce changes (`DoNotDisturbChanged`).

## Risks, stated plainly

- `NtQueryWnfStateData` and the state name are **undocumented**; Microsoft can change them in any update. The failure mode is safe (unknown = allowed, so the line simply never shows).
- Only checked on this one build. Windows 10, early Windows 11 builds and other editions are **not verified**; the state name may differ or mean something else there.
- A Focus session was not tried separately (the API is out of reach at the app's Windows SDK level); whether Windows raises the same state for it is not verified.
- Priority-only mode ("alarms only", allowed apps) is not distinguished from Do Not Disturb.
