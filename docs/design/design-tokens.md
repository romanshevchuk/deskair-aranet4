# Design tokens

*Last reviewed: 2026-10*

Design size is 1200 × 728 px at 100 % scale. Implement every value as a named resource in a light and a dark `ResourceDictionary`.

## Neutrals

| Token | Light | Dark | Use |
|---|---|---|---|
| window.bg | #EEF1F3 | #12161A | App background (Mica fallback) |
| surface | #FFFFFF | #1B2026 | Cards, popovers |
| border | #DCE2E6 | #2C343C | Outlines |
| divider | #E6EAED | #262D34 | Lines inside cards |
| grid | #E3E8EB | #232A31 | Chart gridlines |
| segment.bg | #E4E9EC | #232A31 | Segmented track |
| segment.selected | #FFFFFF | #323A43 | Selected segment (1 px border) |
| text.primary | #182026 | #EEF2F5 | Main text |
| text.secondary | #5A6670 | #9AA5AF | Labels |
| text.tertiary | #5B6771 | #8D99A4 | Axis, quiet labels |
| text.strong2 | #35414A | #CBD3DA | Chart annotations |
| insight.bg | #F1F4F5 | #222930 | Insight box |
| toggle.off / on | #C3CBD1 / #1E8A5C | #3E4852 / #1E8A5C | Switch track |

## Zone colours

| Token | Good (L / D) | Elevated (L / D) | High (L / D) |
|---|---|---|---|
| card.bg | #F2FBF6 / #16261E | #FFFBF1 / #2A2315 | #FFF5F3 / #2B1A18 |
| card.border (1.5 px) | #7CC7A0 / #2F7A58 | #E3B552 / #8A6A1F | #D9776D / #8E3A33 |
| pill.bg | #D3EFE1 / #1F4A38 | #FCEBC4 / #4A3A12 | #F9D9D4 / #4F2420 |
| pill.text | #14573A / #9FE0BF | #7A4800 / #F2CE7A | #8E241A / #F5A9A0 |
| dot | #1E8A5C | #D4961A | #C7453A |
| advice.bg | #DDF2E7 / #1B3328 | #FBEFD0 / #33291A | #FCE2DD / #35201D |
| advice.border | #B3DCC4 / #2C5A44 | #EFD596 / #6A5219 | #EFB4AB / #6B3029 |
| divider | #CBE6D8 / #2A4A3A | #EADFC6 / #4A3E24 | #F0D3CE / #4E2C28 |
| advice.icon | #14573A / #9FE0BF | #8A5300 / #F2CE7A | #8E241A / #F5A9A0 |

Gauge and ribbon segments: #3FA77E, #E6B04B, #C7453A. Chart lines: #1E8A5C, #C47F00, #C23B2B. Trend text: falling #1B7A53 (dark #6FD3A3), rising #B3342A (dark #FF8A7E). Live chart bands: green rgba(63,167,126,.10), amber rgba(230,176,75,.14), red rgba(199,69,58,.08). The gauge scale is 400 to 2,000 ppm.

## Metric colours

| Metric | Light / Dark | Unit | Ideal range |
|---|---|---|---|
| CO₂ | #D4961A (dot) | ppm | zones |
| Temperature | #E07A1F / #F2A15E | °C (°F option) | 20 to 24 °C |
| Humidity | #2563C9 / #6E9BE0 | % | 30 to 50 % |
| Pressure | #6D4FC2 / #A58DF0 | hPa | none |

Non-CO₂ charts: plot area filled with the metric colour at 7 % opacity, ideal range at 17 %, label "ideal 30 to 50".

## Typography

Segoe UI Variable Text, falling back to Segoe UI. Minimum size 12 px. Tabular figures for all numbers.

| Role | Size / weight |
|---|---|
| Hero value | 80 px, 300 |
| History average | 44 px, 300 |
| Mini card value | 32 px, 500 |
| Titles | 16 px, 600 |
| Body | 14 px |
| Secondary, controls | 13 px |
| Captions, chart text, section labels | 12 px. The HTML prototype shows labels uppercase with .06 em spacing; WPF has neither, so labels use normal case and spacing |

## Spacing, radius, elevation

| Token | Value |
|---|---|
| Header / footer height | 52 px / 40 px |
| Page padding | 24 px sides, 8 px top, 20 px bottom |
| Card gap | 16 px |
| Radius: card, hero | 16 px |
| Radius: advice panel, banner, insight | 12 px and 10 px |
| Radius: segmented track / segment | 10 px / 8 px |
| Radius: window | 10 px (Windows 11); 1 px border at 30 % grey |
| Hero padding | 22 / 24 / 20 px |
| Settings card / row | card padding 12 / 22 / 2 px; row at least 40 px tall with 4 px above and below, 16 px between label and control, a 1 px divider above every row but the first |
| Live grid | 3 equal columns; rows 1 fr and 132 px |
| History | toolbar 40 px; stats card 360 px wide |
| Flyout | 360 px wide, radius 12 px, shadow 0 12 36 at 35 % black |
| Toast | drawn by Windows (card, fonts, corners); Deskair supplies the zone-coloured ring (96 px image, the same ring as the tray icon) and the texts; no left edge, no custom colours |

## Icons

Logo and tray ring: 28 × 28 viewbox, circle r 11, stroke 2.75 to 3, centre dot r 4.5, in the zone colour (the zone dot colours, the same in both themes). Tray icon not connected: dashed ring and slash in `Tray.Offline` (#7B858D). Tray icon with the value: rounded square in the zone colour with the CO₂ in thousands ("1.1k"), text `Tray.Value.OnLight` (#FFFFFF) on green and red and `Tray.Value.OnAmber` (#182026) on amber. Advice icons: 18 × 18 line icons (window, check circle, warning triangle), 1.6 px stroke. Window glyph: 10 × 10. Battery: 26 × 12; green above 20 %, amber at 20 % or below, red at 10 % or below.

## Contrast

All text pairs reach 4.5:1 and all chart colours 3:1, in both themes. Re-check if a token changes.
