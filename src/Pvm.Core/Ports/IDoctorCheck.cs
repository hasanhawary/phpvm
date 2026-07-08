using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Represents an automated system health or configuration check for PVM Doctor.
/// </summary>
public interface IDoctorCheck
{
    /// <summary>
    /// Gets the display name of the check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of what this check verifies.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the diagnostic check.
    /// </summary>
    Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to automatically repair or remediate the issue if supported.
    /// </summary>
    Task<Result> FixAsync(CancellationToken cancellationToken = default);
}
