# Add a control

*Last reviewed: 2026-10-21*

1. Check `docs/design/components.md`; reuse or extend an existing control before creating one.
2. Create `src/Deskair.App/Ui/Controls/<Name>/` with `<Name>.cs` (a `Control` subclass with dependency properties) and `<Name>.xaml` (the template).
3. Register the template in `Themes/Generic.xaml`.
4. Use `DynamicResource` tokens from `docs/design/design-tokens.md`. No literal colours or sizes.
5. Any text comes from resources through `{loc:T Key}` (see `add-a-translation.md`).
6. Support keyboard focus, a visible focus ring, and `AutomationProperties.Name`.
7. Add the control to the gallery in all states, light and dark, English and Ukrainian.
8. Keep the XAML under about 120 lines; split into child controls if larger.
9. Done when: build and tests pass, the gallery shows every state, and it matches `docs/design/components.md`.
