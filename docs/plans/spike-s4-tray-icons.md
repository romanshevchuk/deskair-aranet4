# Spike S4: tray icons

*Last reviewed: 2026-10-14*

**Question:** do tray icons stay crisp at 16, 20, 24 and 32 px, on a light and a dark taskbar?

**Answer:** yes, as drawn by `TrayIconRenderer` (TRAY-01b). The icon is drawn straight onto a bitmap of the size asked for (96 dpi, one device pixel per bitmap pixel), so there is no scaling step to blur it. The picture `artifacts/visual/tray-icons.png` (`$env:DESKAIR_RENDER=1; dotnet test tests/Deskair.Tests --filter "FullyQualifiedName~TrayIconRenderer"`) shows every icon at the four sizes on both backgrounds, as drawn and magnified 8 times.

- **Rings (Good, Elevated, High):** sharp and clear at all four sizes on both taskbars. The zone dot colours are the same in both themes and have enough contrast on white and on near-black.
- **Not connected:** a dashed ring with a slash. At 16 and 20 px the dashes are about 2 px and read almost as a solid ring with a slash, which is still clearly "not available".
- **Value icon (rounded square, "1.1k"):** readable at 24 and 32 px; at 16 and 20 px the four characters are tiny. The value on the icon is a setting that is off by default, so this is acceptable, but a user with a 16 px tray (100 % scale) may find it hard to read.
- **Windows scaling:** the tray asks for a size that follows the DPI (16 px at 100 %, 20 at 125 %, 24 at 150 %, 32 at 200 %). `TrayIconHost` (TRAY-01a) does this: it asks Windows for `SM_CXSMICON` and renders exactly that size.

**Not checked:** the real taskbar on a real display (the images are renders, not screenshots of the taskbar).
