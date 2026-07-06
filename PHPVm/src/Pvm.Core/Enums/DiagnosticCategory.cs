namespace Pvm.Core.Enums;

/// <summary>
/// Categorizes diagnostic health checks.
/// </summary>
public enum DiagnosticCategory
{
    /// <summary>
    /// Checks related to the pvm installation directory and configuration.
    /// </summary>
    Installation,

    /// <summary>
    /// Checks related to installed PHP binaries and extensions.
    /// </summary>
    Php,

    /// <summary>
    /// Checks related to system and user PATH configuration and junctions.
    /// </summary>
    Path,

    /// <summary>
    /// Checks related to system runtimes (e.g., Visual C++ Redistributables).
    /// </summary>
    Runtime,

    /// <summary>
    /// Checks related to network connectivity and download mirrors.
    /// </summary>
    Network
}
