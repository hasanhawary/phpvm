using Pvm.Application.Services;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Application.Tests;

public class InstallServiceTests
{
    private sealed class FakeBuildSource : IBuildSource
    {
        public List<PhpBuild> Builds { get; } = new();
        public string Name => "Fake Source";

        public Task<IReadOnlyList<PhpBuild>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PhpBuild>>(Builds);

        public Task<PhpBuild?> GetLatestVersionAsync(string? branch = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Builds.FirstOrDefault());

        public Task<Result<string>> DownloadBuildAsync(PhpBuild build, string destinationPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            File.WriteAllText(destinationPath, "fake zip content");
            return Task.FromResult(Result.Ok(destinationPath));
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class FakeScanner : IInstallationScanner
    {
        public List<PhpInstallation> Installations { get; } = new();
        public string VersionsDirectory { get; } = Path.Combine(Path.GetTempPath(), "pvm_fake_versions_" + Guid.NewGuid().ToString("N"));

        public Task<IReadOnlyList<PhpInstallation>> ScanInstalledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PhpInstallation>>(Installations);

        public Task<PhpInstallation?> GetInstallationAsync(PhpVersion version, CancellationToken cancellationToken = default)
            => Task.FromResult(Installations.FirstOrDefault(x => x.Version == version));

        public Task<PhpVersion?> GetActiveVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<PhpVersion?>(null);
    }

    private sealed class FakeExtractor : IArchiveExtractor
    {
        public Task<Result> VerifyChecksumAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> ExtractAsync(string archivePath, string destinationDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(destinationDirectory);
            File.WriteAllText(Path.Combine(destinationDirectory, "php.exe"), "fake binary");
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class FakePhpProcess : IPhpProcess
    {
        public Task<PhpVersion?> GetVersionAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult<PhpVersion?>(new PhpVersion(8, 4, 0));
        public Task<IReadOnlyList<string>> GetLoadedExtensionsAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        public Task<string?> GetIniPathAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
        public Task<PhpRuntimeInfo> GetConfigurationAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new PhpRuntimeInfo(new PhpVersion(8, 4, 0), null, Array.Empty<string>()));
        public Task<bool> IsValidAsync(string phpExePath, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    [Fact]
    public async Task InstallAsync_NoMatchingBuild_ReturnsFailure()
    {
        var source = new FakeBuildSource();
        var scanner = new FakeScanner();
        var extractor = new FakeExtractor();
        var php = new FakePhpProcess();
        var service = new InstallService(source, scanner, extractor, php);

        var result = await service.InstallAsync(VersionSpecifier.Parse("8.4"));

        Assert.True(result.IsFailure);
        Assert.Contains("No remote PHP build found", result.Error);
    }

    [Fact]
    public async Task InstallAsync_AlreadyInstalledWithoutForce_ReturnsFailure()
    {
        var source = new FakeBuildSource();
        var v84 = new PhpVersion(8, 4, 0);
        source.Builds.Add(new PhpBuild(v84, Architecture.X64, ThreadSafety.Nts, new Uri("http://fake"), "hash", 10 * 1024 * 1024L, "vs17"));

        var scanner = new FakeScanner();
        scanner.Installations.Add(new PhpInstallation(v84, @"C:\php\8.4.0", Architecture.X64, ThreadSafety.Nts, false, false));

        var extractor = new FakeExtractor();
        var php = new FakePhpProcess();
        var service = new InstallService(source, scanner, extractor, php);

        var result = await service.InstallAsync(VersionSpecifier.Parse("8.4"));

        Assert.True(result.IsFailure);
        Assert.Contains("already installed", result.Error);
    }

    [Fact]
    public async Task InstallAsync_ValidBuild_DownloadsExtractsAndReturnsSuccess()
    {
        var source = new FakeBuildSource();
        var v84 = new PhpVersion(8, 4, 0);
        source.Builds.Add(new PhpBuild(v84, Architecture.X64, ThreadSafety.Nts, new Uri("http://fake"), "hash", 10 * 1024 * 1024L, "vs17"));

        var scanner = new FakeScanner();
        var extractor = new FakeExtractor();
        var php = new FakePhpProcess();
        var service = new InstallService(source, scanner, extractor, php);

        var result = await service.InstallAsync(VersionSpecifier.Parse("8.4"));

        Assert.True(result.IsSuccess);
        Assert.Equal(v84, result.Value.Version);
        if (Directory.Exists(scanner.VersionsDirectory)) Directory.Delete(scanner.VersionsDirectory, true);
    }
}
