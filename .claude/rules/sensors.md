---
paths:
  - "src/Deskair.Sensors/**"
---
<!-- Last reviewed: 2026-10 -->

# Sensors rules

One folder per sensor. `net10.0`. May depend on Services and Domain.

- Never: WPF, Windows APIs, file access, user-visible text.
- A sensor is a folder implementing `ISensorProvider` and `ISensor`, with one `Add<Name>(IServiceCollection)` registration. Guide: `docs/guides/adding-a-sensor.md`.
- Parsers are pure: bytes in, `Reading` out, in °C and hPa. Test them with recorded byte arrays.
- Talk to the device only through `IBleConnection`.
- Return resource keys (display name, onboarding steps), never text.
- `Simulated/` is the reference implementation and feeds the gallery and tests.
- Phone-export import rules: `docs/engineering/sync-and-import.md`.
