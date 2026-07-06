using Pvm.Application.Services;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Application.Tests;

public class SwitchServiceTests
{
    private sealed class FakeScanner : IInstallationScanner
    {
        public List<PhpInstallation> Installations { get; } = new();
        public string VersionsDirectory { get; } = @"C:\php\versions";
        public PhpVersion? ActiveVersion { get; set; }

        public Task<IReadOnlyList<PhpInstallation>> ScanInstalledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PhpInstallation>>(Installations);

        public Task<PhpInstallation?> GetInstallationAsync(PhpVersion version, CancellationToken cancellationToken = default)
            => Task.FromResult(Installations.FirstOrDefault(x => x.Version == version));

        public Task<PhpVersion?> GetActiveVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveVersion);
    }

    private sealed class FakeJunctionManager : IJunctionManager
    {
        public string? LastJunctionPath { get; private set; }
        public string? LastTargetPath { get; private set; }
        public bool ShouldFail { get; set; }

        public Result CreateOrUpdateJunction(string junctionPath, string targetPath)
        {
            if (ShouldFail) return Result.Fail("Mock junction failure.");
            LastJunctionPath = junctionPath;
            LastTargetPath = targetPath;
            return Result.Ok();
        }

        public Result DeleteJunction(string junctionPath) => Result.Ok();
        public string? GetJunctionTarget(string junctionPath) => LastTargetPath;
        public bool IsJunction(string path) => LastJunctionPath == path;
    }

    private sealed class FakePhpProcess : IPhpProcess
    {
        public bool IsValid { get; set; } = true;

        public Task<PhpVersion?> GetVersionAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult<PhpVersion?>(new PhpVersion(8, 4, 0));

        public Task<IReadOnlyList<string>> GetLoadedExtensionsAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<string?> GetIniPathAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<PhpRuntimeInfo> GetConfigurationAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new PhpRuntimeInfo(new PhpVersion(8, 4, 0), null, Array.Empty<string>()));

        public Task<bool> IsValidAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult(IsValid);
    }

    [Fact]
    public async Task SwitchAsync_NoInstallations_ReturnsFailure()
    {
        var scanner = new FakeScanner();
        var junction = new FakeJunctionManager();
        var php = new FakePhpProcess();
        var service = new SwitchService(scanner, junction, php);

        var result = await service.SwitchAsync(VersionSpecifier.Parse("8.4"));

        Assert.True(result.IsFailure);
        Assert.Contains("No PHP versions are installed", result.Error);
    }

    [Fact]
    public async Task SwitchAsync_MatchingVersion_UpdatesJunctionAndReturnsSuccess()
    {
        var scanner = new FakeScanner();
        var v84 = new PhpInstallation(new PhpVersion(8, 4, 0), @"C:\php\8.4.0", Architecture.X64, ThreadSafety.Nts, true, false);
        scanner.Installations.Add(v84);

        var junction = new FakeJunctionManager();
        var php = new FakePhpProcess();
        var service = new SwitchService(scanner, junction, php);

        var result = await service.SwitchAsync(VersionSpecifier.Parse("8.4"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(@"C:\php\8.4.0", junction.LastTargetPath);
    }

    [Fact]
    public async Task SwitchAsync_JunctionUpdateFails_ReturnsFailure()
    {
        var scanner = new FakeScanner();
        var v84 = new PhpInstallation(new PhpVersion(8, 4, 0), @"C:\php\8.4.0", Architecture.X64, ThreadSafety.Nts, true, false);
        scanner.Installations.Add(v84);

        var junction = new FakeJunctionManager { ShouldFail = true };
        var php = new FakePhpProcess();
        var service = new SwitchService(scanner, junction, php);

        var result = await service.SwitchAsync(VersionSpecifier.Parse("8.4"));

        Assert.True(result.IsFailure);
        Assert.Contains("Failed to update NTFS junction", result.Error);
    }
}
