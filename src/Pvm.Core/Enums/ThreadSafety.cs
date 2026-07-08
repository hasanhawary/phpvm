namespace Pvm.Core.Enums;

/// <summary>
/// Represents the thread safety mode of a PHP build.
/// </summary>
public enum ThreadSafety
{
    /// <summary>
    /// Non-Thread Safe (NTS). Recommended for FastCGI, CLI, and IIS on Windows.
    /// </summary>
    Nts,

    /// <summary>
    /// Thread Safe (TS). Recommended for Apache mod_php on Windows.
    /// </summary>
    Ts
}
