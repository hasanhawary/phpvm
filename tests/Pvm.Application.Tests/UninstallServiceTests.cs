using Pvm.Application.Services;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Application.Tests;

public class UninstallServiceTests
{
    private sealed class FakeScanner : IInstallationScanner
    {
        public List<PhpInstallation> Installations { get; } = new();
        public string VersionsDirectory { get; } = Path.Combine(Path.GetTempPath(), "pvm_fake_ver");

        public Task<IReadOnlyList<PhpInstallation>> ScanInstalledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PhpInstallation>>(Installations);

        public Task<PhpInstallation?> GetInstallationAsync(PhpVersion version, CancellationToken cancellationToken = default)
            => Task.FromResult(Installations.FirstOrDefault(x => x.Version == version));

        public Task<PhpVersion?> GetActiveVersionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<PhpVersion?>(null);
    }

    [Fact]
    public async Task UninstallAsync_ActiveVersion_ReturnsFailure()
    {
        var scanner = new FakeScanner();
        var v84 = new PhpVersion(8, 4, 0);
        scanner.Installations.Add(new PhpInstallation(v84, @"C:\php\8.4.0", Architecture.X64, ThreadSafety.Nts, HasPhpIni: false, IsActive: true));

        var service = new UninstallService(scanner);
        var result = await service.UninstallAsync(VersionSpecifier.Parse("8.4.0"));

        Assert.True(result.IsFailure);
        Assert.Contains("currently the active version", result.Error);
    }

    [Fact]
    public async Task UninstallAsync_InactiveVersion_DeletesDirectoryAndReturnsSuccess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pvm_uninstall_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "php.exe"), "dummy");

        var scanner = new FakeScanner();
        var v84 = new PhpVersion(8, 4, 0);
        scanner.Installations.Add(new PhpInstallation(v84, tempDir, Architecture.X64, ThreadSafety.Nts, false, false));

        var service = new UninstallService(scanner);
        var result = await service.UninstallAsync(VersionSpecifier.Parse("8.4.0"));

        Assert.True(result.IsSuccess);
        Assert.False(Directory.Exists(tempDir));
    }
}
