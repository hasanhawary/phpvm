using Pvm.Core.Enums;
using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Defines the contract for an individual diagnostic health check executed by pvm doctor.
/// </summary>
public interface IDiagnosticCheck
{
    /// <summary>
    /// Gets the unique identifier of the check (e.g., "PATH001", "PHP001").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable display name of the check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the category of the check.
    /// </summary>
    DiagnosticCategory Category { get; }

    /// <summary>
    /// Gets a value indicating whether this check can automatically repair detected issues via doctor --fix.
    /// </summary>
    bool CanAutoFix { get; }

    /// <summary>
    /// Executes the diagnostic check asynchronously and returns the detailed result.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the diagnostic check.</returns>
    Task<DiagnosticResult> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to automatically repair the issue detected by this check.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result indicating whether the repair succeeded.</returns>
    Task<Result> FixAsync(CancellationToken cancellationToken = default);
}
