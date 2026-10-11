# Spikes (milestone M0)

*Last reviewed: 2026-10*

Each spike is time-boxed to 2 to 3 days. Output: a short note in this folder with the answer and a recommendation.

| ID | Question | Approach | Exit criteria |
|---|---|---|---|
| S1 | Can we read the sensor's stored history reliably (about 7 days) and match the phone export? | Reuse the existing prototype's BLE code; sync, then compare against a phone export | Timestamps match within one interval; documented sensor interval and capacity; behaviour when the phone app is also connected |
| S2 | Can the flyout be placed correctly next to the tray icon? | Borderless window using the tray icon rectangle | Correct at all taskbar edges, 2 monitors, 100 to 200 % DPI, Windows 10 and 11; closes on click-away |
| S3 | Which title bar approach keeps snap layouts and Mica? | Compare native bar, WPF-UI, custom chrome | Chosen approach works on Windows 11; Windows 10 fallback looks right |
| S4 | Do tray icons stay crisp? | Render ring and value icons at 16, 20, 24, 32 px | Sharp at all DPI, light and dark taskbar |
| S5 | Toast setup for packaged and unpackaged builds | Minimal toast with Snooze and Open actions | Actions work in both; one code path |
| S6 | Import speed and correctness | Parse a real .xlsx of about 60,000 rows | Under a few seconds; correct local times across a daylight-saving change |
