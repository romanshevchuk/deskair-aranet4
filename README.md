# Deskair

A Windows app for the Aranet4 CO₂ sensor. It shows live air quality, tells you when the room needs fresh air, and keeps your history on your PC.

> **Unofficial.** Deskair is an independent open-source project, not made by or connected to the maker of Aranet4. Aranet is a trademark of its owner. Privacy: [`PRIVACY.md`](PRIVACY.md).

![Deskair Live screen: CO₂ in the Good zone with a 24-hour chart](docs/screenshots/live.png)

## Features

- **Live:** CO₂ with a zone (Good, Elevated or High) and a short tip like "Open a window for about 10 minutes", plus temperature, humidity and pressure. Charts for 1, 6 or 24 hours.
- **Tray:** the icon changes colour with the air. Click it for a quick look.
- **Alerts:** a Windows notification when CO₂ stays high, with Snooze. It follows quiet hours and Do Not Disturb, and sends one toast per episode.
- **History:** by day, week, month or year, with a typical day and the hours when the air is usually worst. Gaps stay gaps; nothing is made up.
- **Your data:** no account, no cloud. Readings are CSV files in `Documents\Deskair`. Export them, or import the xlsx from the Aranet Home phone app.
- **Also:** English and Ukrainian, light and dark themes, °C and °F. Starts with Windows if you want, keeps running in the tray, and catches up on the sensor's stored history after sleep.

| Tray flyout | Dark theme |
|---|---|
| ![Tray flyout showing the last 24 hours at a glance](docs/screenshots/tray-flyout.png) | ![Live screen in the dark theme](docs/screenshots/live-dark.png) |

| History by week | History for one day |
|---|---|
| ![Week view: an hour-by-hour grid, one row per day](docs/screenshots/history-week.png) | ![Day view with the night shaded and the typical day as a line](docs/screenshots/history-day.png) |

*The screenshots use sample data.*

## What you need

- An **Aranet4** sensor.
- In the **Aranet Home** phone app, switch on **Smart Home integration** for the sensor. Without it the sensor refuses the readings Deskair asks for.
- A Windows PC with Bluetooth LE: Windows 11, or Windows 10 version 2004 or later. Windows 10 has not been tried yet.

## Install

- **Microsoft Store (recommended):** [Deskair in the Microsoft Store](https://apps.microsoft.com/detail/9N027HRZXSMH). Free, and it updates itself.
- **From source:** see below.

## Build and run

You need Windows and the .NET 10 SDK. WPF cannot be built on Linux or macOS.

```
dotnet build Deskair.sln
dotnet test tests/Deskair.Tests
dotnet run --project src/Deskair.App
```

No sensor at hand? Run the app on a simulated one. It uses its own temporary data folder, so your real data is untouched:

```
dotnet run --project src/Deskair.App -- --simulated
```

More switches (a control gallery, one real sensor by address, the first-run screen, a test toast) are listed in [`AGENTS.md`](AGENTS.md).

## How it works

Five projects, each depending only on the ones before it: `Domain` (types and calculations), `Services` (sync, live feed, alerts, queries), `Sensors` (one folder per sensor), `Storage` (monthly CSV files and settings) and `App` (WPF views and view models). A new sensor can be added without touching the rest; see [`docs/guides/adding-a-sensor.md`](docs/guides/adding-a-sensor.md).

## Troubleshooting

- **"Not connected" or no readings:** check that Bluetooth is on and that Smart Home integration is on for the sensor in the Aranet Home app.
- **No toasts:** Windows Do Not Disturb may be holding them; Deskair says so in Settings.
- **Logs (running from source):** `%LOCALAPPDATA%\Deskair\logs`.

## Acknowledgements

Thanks to [Aranet4-Python](https://github.com/Anrijs/Aranet4-Python) by Anrijs Jargans and contributors (MIT licence). Its protocol documentation and client were the reference for talking to the sensor. Deskair's C# code is separate and only uses the protocol identifiers and packet layouts. A capture of the sensor's history format is in `tests/Deskair.Tests/TestData/aranet4-history-capture.txt`.

## Documentation

- Start at [`docs/00-index.md`](docs/00-index.md): product rules, design, data model, architecture and decisions.
- Contributing: [`CONTRIBUTING.md`](CONTRIBUTING.md).

## License

[MIT](LICENSE).
