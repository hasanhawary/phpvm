using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Discovers and downloads remote or local PHP builds.
/// </summary>
public interface IBuildSource
{
    /// <summary>
    /// Gets the display name of the build source (e.g., "Official PHP Mirrors", "Local Archive").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets all available PHP builds from this source.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of available builds.</returns>
    Task<IReadOnlyList<PhpBuild>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest available build, optionally filtered by branch (e.g., "8.4").
    /// </summary>
    /// <param name="branch">The optional branch prefix (e.g., "8.4").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The latest build, or null if no matching build is found.</returns>
    Task<PhpBuild?> GetLatestVersionAsync(string? branch = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a build archive to the specified local destination path.
    /// </summary>
    /// <param name="build">The build to download.</param>
    /// <param name="destinationPath">The local file path to save the downloaded archive.</param>
    /// <param name="progress">An optional progress reporter (0.0 to 100.0).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the local file path on success.</returns>
    Task<Result<string>> DownloadBuildAsync(
        PhpBuild build,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether this build source is currently reachable or available.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if available; otherwise, <c>false</c>.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
