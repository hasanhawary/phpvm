using Pvm.Core.Enums;

namespace Pvm.Core.Models;

/// <summary>
/// Represents an immutable record of a downloadable PHP build from a remote or local source.
/// </summary>
/// <param name="Version">The semantic PHP version of the build.</param>
/// <param name="Architecture">The processor architecture of the build.</param>
/// <param name="ThreadSafety">The thread safety mode of the build.</param>
/// <param name="DownloadUrl">The URI where the build archive can be downloaded.</param>
/// <param name="Checksum">The optional SHA256 checksum of the archive.</param>
/// <param name="FileSize">The optional size of the archive in bytes.</param>
/// <param name="VsVersion">The Visual Studio compiler version used (e.g., "vs17", "vs16").</param>
public sealed record PhpBuild(
    PhpVersion Version,
    Architecture Architecture,
    ThreadSafety ThreadSafety,
    Uri DownloadUrl,
    string? Checksum,
    long? FileSize,
    string VsVersion
);
