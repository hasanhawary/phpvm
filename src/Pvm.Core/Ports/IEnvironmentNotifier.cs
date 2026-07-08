using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Notifies the operating system and running processes of environment variable changes.
/// </summary>
public interface IEnvironmentNotifier
{
    /// <summary>
    /// Broadcasts a notification that environment variables (such as PATH) have been updated.
    /// </summary>
    /// <returns>A result indicating whether the broadcast succeeded.</returns>
    Result NotifyEnvironmentChanged();
}
