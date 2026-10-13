namespace Deskair.App.ViewModels.Onboarding;

/// <summary>One line of the checklist on the welcome card. The text is already localized.</summary>
public sealed record OnboardingStep(int Number, string Text, bool IsDone);
