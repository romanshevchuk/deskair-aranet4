using System.IO;
using System.Security;
using Deskair.App.Composition;
using Deskair.Services.Platform;
using Microsoft.Win32;

namespace Deskair.App.Platform.Startup;

/// <summary>
/// Start with Windows through the per-user Run key (unpackaged builds). A packaged build would use a startup task instead (docs/decisions.md D-0003);
/// that difference stays inside this class. The value is the quoted path of the running program plus <c>--background</c>, so it starts in the tray.
/// </summary>
public sealed class StartupRegistration : IStartupRegistration
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "Deskair";

    private readonly string keyPath;
    private readonly string valueName;
    private readonly string executablePath;

    public StartupRegistration()
        : this(RunKeyPath, ValueName, Environment.ProcessPath ?? string.Empty)
    {
    }

    /// <summary>For tests: another key, so the real Run key is never touched.</summary>
    public StartupRegistration(string keyPath, string valueName, string executablePath)
    {
        this.keyPath = keyPath;
        this.valueName = valueName;
        this.executablePath = executablePath;
    }

    /// <summary>True when the Run key has a value for the app. A key that cannot be read counts as not enabled.</summary>
    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);

                return key?.GetValue(valueName) is string { Length: > 0 };
            }
            catch (Exception exception) when (exception is SecurityException or UnauthorizedAccessException or IOException)
            {
                return false;
            }
        }
    }

    /// <summary>Adds or removes the Run value. Cancelling before the change leaves it as it was; a failed write throws.</summary>
    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            key.SetValue(valueName, $"\"{executablePath}\" {AppOptions.BackgroundArgument}", RegistryValueKind.String);
        }
        else
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(valueName, throwOnMissingValue: false);
        }

        return Task.CompletedTask;
    }
}
