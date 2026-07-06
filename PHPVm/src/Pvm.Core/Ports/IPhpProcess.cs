using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Executes PHP binaries and retrieves version and configuration metadata.
/// </summary>
public interface IPhpProcess
{
    /// <summary>
    /// Executes php -v and parses the reported semantic version.
    /// </summary>
    /// <param name="phpExePath">The full path to php.exe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The reported PHP version, or null if execution failed.</returns>
    Task<PhpVersion?> GetVersionAsync(string phpExePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes php -m and returns the list of loaded extension names.
    /// </summary>
    /// <param name="phpExePath">The full path to php.exe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of loaded extension names.</returns>
    Task<IReadOnlyList<string>> GetLoadedExtensionsAsync(string phpExePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes php --ini and retrieves the full path to the loaded php.ini file.
    /// </summary>
    /// <param name="phpExePath">The full path to php.exe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The path to php.ini, or null if none is loaded.</returns>
    Task<string?> GetIniPathAsync(string phpExePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves complete runtime configuration metadata from the specified PHP binary.
    /// </summary>
    /// <param name="phpExePath">The full path to php.exe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The runtime information record.</returns>
    Task<PhpRuntimeInfo> GetConfigurationAsync(string phpExePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the specified file exists and is a valid, executable PHP binary.
    /// </summary>
    /// <param name="phpExePath">The full path to php.exe.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if valid and executable; otherwise, <c>false</c>.</returns>
    Task<bool> IsValidAsync(string phpExePath, CancellationToken cancellationToken = default);
}
