using Pvm.Core.Enums;

namespace Pvm.Core.Models;

/// <summary>
/// Represents an immutable record of an installed PHP version on disk.
/// </summary>
/// <param name="Version">The resolved semantic PHP version.</param>
/// <param name="Path">The full directory path where this version is installed.</param>
/// <param name="Architecture">The processor architecture of the installation.</param>
/// <param name="ThreadSafety">The thread safety mode of the installation.</param>
/// <param name="HasPhpIni">A value indicating whether a php.ini file exists in the installation directory.</param>
/// <param name="IsActive">A value indicating whether this installation is the currently active version pointed to by the junction.</param>
public sealed record PhpInstallation(
    PhpVersion Version,
    string Path,
    Architecture Architecture,
    ThreadSafety ThreadSafety,
    bool HasPhpIni,
    bool IsActive
);
