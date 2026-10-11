# Adding a sensor (for forks and AI assistants)

*Last reviewed: 2026-10*

Goal: support another sensor without touching UI, storage or analytics. The sensor must provide CO₂ (ppm), temperature, humidity and pressure.

## Steps

1. **Document the protocol.** Capture the sensor's Bluetooth LE data (for example with nRF Connect) and write what you learn in `docs/sensors/<name>.md`: the service and characteristic UUIDs, byte layouts, units, scaling, how history is read, and the update interval. A *service* groups *characteristics*; a characteristic is one readable value identified by a UUID. Save raw example byte arrays.
2. **Add a folder** `src/Deskair.Sensors/<Name>/`. It may use only `Deskair.Services` (the contracts) and `Deskair.Domain`. No UI, files or Windows APIs.
3. **Write the parser** as a pure class: bytes in, `Reading` out, converted to °C and hPa. Add unit tests in `tests/Deskair.Tests/Sensors/<Name>/` using the recorded bytes.
4. **Write the sensor** implementing `ISensor` on top of `IBleConnection`. Set `Capabilities` honestly (History, Battery, SignalStrength).
5. **Write the provider** implementing `ISensorProvider`: scanning (filter by name or service UUID), creating sensors, and the resource keys for the display name and onboarding steps.
6. **Optional:** an importer implementing `IReadingImporter` for the vendor's export files.
7. **Register:** add `Add<Name>(this IServiceCollection)` and call it from `src/Deskair.App/Composition/ServiceRegistration.cs`. This is the only edit outside your folder, apart from strings.
8. **Strings:** add your sensor name and onboarding steps to `src/Deskair.App/Localization/Strings.resx` (and `Strings.uk.resx`).
9. **Check:** the dependency-rules test passes, and the sensor appears in the first-run sensor list.

Use `Sensors/Simulated` as a minimal reference.

## Prompt template for an AI assistant

> Read `docs/engineering/architecture.md` (sections 3 to 5) and `docs/guides/adding-a-sensor.md`. I have a BLE sensor called `<name>`. Here is its protocol description and example byte captures: `<paste>`. Create `src/Deskair.Sensors/<Name>/` following the `Aranet4/` folder: a pure parser with unit tests built from my captures, a sensor on `IBleConnection`, a provider, and the `Add<Name>` registration. Do not modify any other file except the registration line and the string resources.
