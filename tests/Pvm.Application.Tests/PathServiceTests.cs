using Pvm.Application.Services;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Application.Tests;

public class PathServiceTests
{
    private sealed class FakePathManager : IPathManager
    {
        public List<string> UserPath { get; } = new();
        public List<string> MachinePath { get; } = new();
        public List<string> ConflictingEntries { get; } = new();

        public IReadOnlyList<string> GetUserPath() => UserPath;
        public IReadOnlyList<string> GetMachinePath() => MachinePath;

        public Result AddToUserPath(string entry)
        {
            if (!UserPath.Contains(entry, StringComparer.OrdinalIgnoreCase))
            {
                UserPath.Add(entry);
            }
            return Result.Ok();
        }

        public Result RemoveFromUserPath(string entry)
        {
            UserPath.RemoveAll(x => string.Equals(x, entry, StringComparison.OrdinalIgnoreCase));
            return Result.Ok();
        }

        public bool ContainsEntry(string entry)
            => UserPath.Contains(entry, StringComparer.OrdinalIgnoreCase) ||
               MachinePath.Contains(entry, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> FindDuplicateEntries()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();
            foreach (var p in UserPath)
            {
                if (!seen.Add(p)) duplicates.Add(p);
            }
            return duplicates;
        }

        public Result CleanDuplicateEntries()
        {
            var distinct = UserPath.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            UserPath.Clear();
            UserPath.AddRange(distinct);
            return Result.Ok();
        }

        public IReadOnlyList<string> FindConflictingPhpEntries() => ConflictingEntries;
    }

    private sealed class FakeScanner : IInstallationScanner
    {
        public string VersionsDirectory { get; } = @"C:\Users\test\.pvm\versions";
        public Task<IReadOnlyList<PhpInstallation>> ScanInstalledAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PhpInstallation>>(new List<PhpInstallation>());
        public Task<PhpInstallation?> GetInstallationAsync(PhpVersion version, CancellationToken cancellationToken = default) => Task.FromResult<PhpInstallation?>(null);
        public Task<PhpVersion?> GetActiveVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<PhpVersion?>(null);
    }

    [Fact]
    public async Task EnsureCurrentJunctionInPathAsync_WhenNotInPath_AddsToUserPath()
    {
        var pm = new FakePathManager();
        var scanner = new FakeScanner();
        var service = new PathService(pm, scanner);

        var result = await service.EnsureCurrentJunctionInPathAsync();

        Assert.True(result.IsSuccess);
        Assert.Contains(@"C:\Users\test\.pvm\current", pm.UserPath);
    }

    [Fact]
    public async Task EnsureCurrentJunctionInPathAsync_WhenAlreadyInPath_DoesNotDuplicate()
    {
        var pm = new FakePathManager();
        pm.UserPath.Add(@"C:\Users\test\.pvm\current");
        var scanner = new FakeScanner();
        var service = new PathService(pm, scanner);

        var result = await service.EnsureCurrentJunctionInPathAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(pm.UserPath);
    }

    [Fact]
    public async Task GetPathStatusAsync_ReportsCorrectStatus()
    {
        var pm = new FakePathManager();
        pm.UserPath.Add(@"C:\php\old");
        pm.UserPath.Add(@"C:\php\old"); // duplicate
        pm.ConflictingEntries.Add(@"C:\xampp\php");
        var scanner = new FakeScanner();
        var service = new PathService(pm, scanner);

        var status = await service.GetPathStatusAsync();

        Assert.False(status.IsJunctionInPath);
        Assert.Single(status.Duplicates);
        Assert.Single(status.ConflictingPhpEntries);
        Assert.Equal(@"C:\xampp\php", status.ConflictingPhpEntries[0]);
    }

    [Fact]
    public async Task CleanDuplicatesAsync_RemovesDuplicates()
    {
        var pm = new FakePathManager();
        pm.UserPath.Add(@"C:\dir1");
        pm.UserPath.Add(@"C:\dir2");
        pm.UserPath.Add(@"C:\dir1"); // duplicate
        var scanner = new FakeScanner();
        var service = new PathService(pm, scanner);

        var result = await service.CleanDuplicatesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, pm.UserPath.Count);
        Assert.Equal(@"C:\dir1", pm.UserPath[0]);
        Assert.Equal(@"C:\dir2", pm.UserPath[1]);
    }
}
