namespace Deskair.Services.Platform;

public interface ISystemTheme
{
    bool IsDark { get; }

    event EventHandler? Changed;
}
