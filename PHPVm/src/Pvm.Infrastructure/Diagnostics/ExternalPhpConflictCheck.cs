using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Diagnostics;

/// <summary>
/// Checks for external PHP installations in PATH that might shadow PVM.
/// </summary>
public sealed class ExternalPhpConflictCheck : IDoctorCheck
{
    private readonly IPathManager _pathManager;

    public ExternalPhpConflictCheck(IPathManager pathManager)
    {
        _pathManager = pathManager ?? throw new ArgumentNullException(nameof(pathManager));
    }

    public string Name => "External PHP Shadowing Conflicts";
    public string Description => "Checks if XAMPP, WAMP, Laragon, or standalone PHP in PATH might shadow PVM.";

    public Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default)
    {
        var conflicts = _pathManager.FindConflictingPhpEntries();
        if (conflicts.Count > 0)
        {
            var list = string.Join(", ", conflicts);
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorStatus.Warning,
                $"Found {conflicts.Count} external PHP directory(s) in PATH: {list}",
                "Remove these directories from your System or User PATH to ensure PVM controls your active PHP version.",
                CanFix: false
            ));
        }

        return Task.FromResult(new DoctorCheckResult(
            Name,
            DoctorStatus.Pass,
            "No conflicting external PHP installations found in PATH."
        ));
    }

    public Task<Result> FixAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}
