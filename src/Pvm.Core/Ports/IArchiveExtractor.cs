using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages extracting downloaded PHP archives and verifying cryptographic checksums.
/// </summary>
public interface IArchiveExtractor
{
    /// <summary>
    /// Verifies the SHA256 checksum of a local file against an expected hex hash string.
    /// </summary>
    /// <param name="filePath">The local file path.</param>
    /// <param name="expectedSha256">The expected hex-encoded SHA256 string.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result indicating whether the checksum matched.</returns>
    Task<Result> VerifyChecksumAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a zip archive to the specified destination directory with optional progress reporting.
    /// </summary>
    /// <param name="archivePath">The local path to the zip archive.</param>
    /// <param name="destinationDirectory">The target installation directory.</param>
    /// <param name="progress">An optional progress reporter (0.0 to 100.0).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExtractAsync(string archivePath, string destinationDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
