# Spikes (milestone M0)

*Last reviewed: 2026-10-14*

All spikes are answered. The throwaway spike code is removed (S1, S3 and S5 last in commit `07d4f12`); the notes stay as records, and the answers live in the real code. What is still unchecked on real hardware is listed in `handoff.md`.

| ID | Question | Result | Note |
|---|---|---|---|
| S1 | Can we read the sensor's stored history reliably (about 7 days) and match the phone export? | Yes: 5040 readings every 120 s (exactly 7 days), values match the phone export row by row, fine with the phone app connected | [spike-s1-ble-history.md](spike-s1-ble-history.md) |
| S2 | Can the flyout be placed correctly next to the tray icon? | Checked through the real flyout: `FlyoutPositioner` works from the cursor position at the click (Q38). Verified on a bottom taskbar, one monitor. Other edges, DPIs and monitors are unchecked | Q38 in [questions.md](questions.md) |
| S3 | Which title bar approach keeps snap layouts and Mica? | `WindowChrome` with the system caption buttons plus Mica through DWM, no package. Windows 11 only verified | [spike-s3-title-bar.md](spike-s3-title-bar.md), Q14 |
| S4 | Do tray icons stay crisp? | Yes, drawn at the exact size Windows asks for. Renders only; the 16 px value icon is hard to read | [spike-s4-tray-icons.md](spike-s4-tray-icons.md) |
| S5 | Toast setup for packaged and unpackaged builds | Inbox `Windows.UI.Notifications` with a registered identity and COM activator, no package. Unpackaged path verified; the packaged build and a real user button press are not | [spike-s5-toasts.md](spike-s5-toasts.md), Q41 |
| S6 | Import speed and correctness (a real .xlsx of about 60,000 rows) | Not run at that size. The importer is covered by golden tests on a real one-week export (about 5,000 rows) | Q23, Q35 |
