namespace Pvm.Core.Enums;

/// <summary>
/// Represents the result status of a diagnostic check.
/// </summary>
public enum DiagnosticStatus
{
    /// <summary>
    /// The check passed without issues.
    /// </summary>
    Pass,

    /// <summary>
    /// The check found a non-critical issue or recommendation.
    /// </summary>
    Warning,

    /// <summary>
    /// The check failed and indicates a broken or unusable configuration.
    /// </summary>
    Fail,

    /// <summary>
    /// The check was skipped due to prerequisites or configuration.
    /// </summary>
    Skipped
}
