---
paths:
  - "src/Deskair.App/ViewModels/**"
---
<!-- Last reviewed: 2026-10 -->

# View model rules

MVVM with `CommunityToolkit.Mvvm` only.

- Never `System.Windows`. No colours: expose enums (zone, metric) and let the view map them to tokens.
- Presentation logic only: shape data for display. Calculations live in Domain and Services.
- Get data from `ReadingQueryService` and other services, never from storage.
- Use `[ObservableProperty]` and `[RelayCommand]`. Child view models per screen part; about 150 lines at most.
- Text goes through `ILocalizer`.
- Test with fake services.
