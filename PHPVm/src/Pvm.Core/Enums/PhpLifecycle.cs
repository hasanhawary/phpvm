namespace Pvm.Core.Enums;

/// <summary>
/// Represents the support lifecycle status of a PHP version branch.
/// </summary>
public enum PhpLifecycle
{
    /// <summary>
    /// Active support (regular bug and security fixes).
    /// </summary>
    Active,

    /// <summary>
    /// Security support only (only critical security fixes).
    /// </summary>
    Security,

    /// <summary>
    /// End of Life (no longer supported or updated).
    /// </summary>
    Eol
}
