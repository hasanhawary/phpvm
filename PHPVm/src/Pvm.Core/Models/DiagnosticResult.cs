using Pvm.Core.Enums;

namespace Pvm.Core.Models;

/// <summary>
/// Represents the structured output of a single diagnostic health check.
/// </summary>
/// <param name="CheckId">The unique identifier of the check (e.g., "PATH001").</param>
/// <param name="CheckName">The human-readable name of the check.</param>
/// <param name="Category">The category of the check.</param>
/// <param name="Status">The pass/warning/fail status of the check.</param>
/// <param name="Message">A concise summary message of the result.</param>
/// <param name="Details">Additional detailed information or diagnostic lines.</param>
/// <param name="Suggestion">An optional human-readable recommendation for fixing issues.</param>
/// <param name="CanAutoFix">A value indicating whether this check supports automatic repair via doctor --fix.</param>
public sealed record DiagnosticResult(
    string CheckId,
    string CheckName,
    DiagnosticCategory Category,
    DiagnosticStatus Status,
    string Message,
    IReadOnlyList<string> Details,
    string? Suggestion,
    bool CanAutoFix
);
