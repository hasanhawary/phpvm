using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Represents a diagnostic report on the current system and user PATH environment variables regarding PVM.
/// </summary>
/// <param name="CurrentJunction">The expected PVM junction path.</param>
/// <param name="IsJunctionInPath">A value indicating whether the junction is present in User or Machine PATH.</param>
/// <param name="Duplicates">A list of duplicate directory entries in User PATH.</param>
/// <param name="ConflictingPhpEntries">A list of non-PVM directories in PATH that contain php.exe.</param>
public sealed record PathStatusReport(
    string CurrentJunction,
    bool IsJunctionInPath,
    IReadOnlyList<string> Duplicates,
    IReadOnlyList<string> ConflictingPhpEntries
);

/// <summary>
/// Orchestrates PATH inspection, automation, conflict detection, and duplicate cleaning.
/// </summary>
public sealed class PathService
{
    private readonly IPathManager _pathManager;
    private readonly IInstallationScanner _scanner;

    public PathService(IPathManager pathManager, IInstallationScanner scanner)
    {
        _pathManager = pathManager ?? throw new ArgumentNullException(nameof(pathManager));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public string GetCurrentJunctionPath()
    {
        var pvmRoot = Path.GetDirectoryName(_scanner.VersionsDirectory)
                      ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pvm");
        return Path.Combine(pvmRoot, "current");
    }

    public Task<Result> EnsureCurrentJunctionInPathAsync(CancellationToken cancellationToken = default)
    {
        var junction = GetCurrentJunctionPath();
        if (_pathManager.ContainsEntry(junction))
        {
            return Task.FromResult(Result.Ok());
        }

        var addResult = _pathManager.AddToUserPath(junction);
        return Task.FromResult(addResult);
    }

    public Task<PathStatusReport> GetPathStatusAsync(CancellationToken cancellationToken = default)
    {
        var junction = GetCurrentJunctionPath();
        var inPath = _pathManager.ContainsEntry(junction);
        var duplicates = _pathManager.FindDuplicateEntries();
        var conflicts = _pathManager.FindConflictingPhpEntries();

        var report = new PathStatusReport(junction, inPath, duplicates, conflicts);
        return Task.FromResult(report);
    }

    public Task<Result> CleanDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        var result = _pathManager.CleanDuplicateEntries();
        return Task.FromResult(result);
    }
}
