using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Diagnostics;

/// <summary>
/// Verifies that User PATH does not contain duplicate directory entries.
/// </summary>
public sealed class PathHygieneCheck : IDoctorCheck
{
    private readonly IPathManager _pathManager;

    public PathHygieneCheck(IPathManager pathManager)
    {
        _pathManager = pathManager ?? throw new ArgumentNullException(nameof(pathManager));
    }

    public string Name => "Windows PATH Hygiene & Duplicates";
    public string Description => "Checks your User PATH for redundant duplicate directory entries.";

    public Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default)
    {
        var entries = _pathManager.GetUserPath();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = 0;

        foreach (var entry in entries)
        {
            var clean = entry.Trim().TrimEnd('\\', '/');
            if (!string.IsNullOrWhiteSpace(clean))
            {
                if (!seen.Add(clean))
                {
                    duplicates++;
                }
            }
        }

        if (duplicates > 0)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorStatus.Warning,
                $"Found {duplicates} duplicate directory entry(s) in your User PATH.",
                "Run 'pvm doctor --fix' or 'pvm env --clean' to remove duplicates.",
                CanFix: true
            ));
        }

        return Task.FromResult(new DoctorCheckResult(
            Name,
            DoctorStatus.Pass,
            "No duplicate entries found in User PATH."
        ));
    }

    public Task<Result> FixAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_pathManager.CleanDuplicateEntries());
    }
}
