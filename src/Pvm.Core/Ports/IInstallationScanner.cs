using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Discovers and inspects installed PHP versions on disk.
/// </summary>
public interface IInstallationScanner
{
    /// <summary>
    /// Gets the root directory where PHP versions are installed.
    /// </summary>
    string VersionsDirectory { get; }

    /// <summary>
    /// Scans the local installations directory and returns all installed PHP versions.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of installed PHP versions.</returns>
    Task<IReadOnlyList<PhpInstallation>> ScanInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a specific installed PHP version, if present.
    /// </summary>
    /// <param name="version">The exact semantic version to look up.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The installation record, or null if not installed.</returns>
    Task<PhpInstallation?> GetInstallationAsync(PhpVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active PHP version pointed to by the junction, if any.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The active PHP version, or null if no version is active.</returns>
    Task<PhpVersion?> GetActiveVersionAsync(CancellationToken cancellationToken = default);
}
