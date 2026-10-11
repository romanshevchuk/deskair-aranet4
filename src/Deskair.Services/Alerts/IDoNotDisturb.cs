namespace Deskair.Services.Alerts;

/// <summary>Windows Do Not Disturb state. Alerts must never override it.</summary>
public interface IDoNotDisturb
{
    bool IsActive { get; }
}
