using System.Diagnostics;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Orchestrates downloading, verifying, extracting, and configuring new PHP versions.
/// </summary>
public sealed class InstallService
{
    private readonly IBuildSource _buildSource;
    private readonly IInstallationScanner _scanner;
    private readonly IArchiveExtractor _extractor;
    private readonly IPhpProcess _phpProcess;
    private readonly IAliasManager? _aliasManager;

    public InstallService(
        IBuildSource buildSource,
        IInstallationScanner scanner,
        IArchiveExtractor extractor,
        IPhpProcess phpProcess,
        IAliasManager? aliasManager = null)
    {
        _buildSource = buildSource ?? throw new ArgumentNullException(nameof(buildSource));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _phpProcess = phpProcess ?? throw new ArgumentNullException(nameof(phpProcess));
        _aliasManager = aliasManager;
    }

    public async Task<Result<PhpInstallation>> InstallAsync(
        VersionSpecifier specifier,
        Architecture? arch = null,
        ThreadSafety? ts = null,
        bool force = false,
        IProgress<double>? downloadProgress = null,
        IProgress<double>? extractProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specifier);

        var rawSpec = specifier.Raw;
        if (_aliasManager is not null)
        {
            rawSpec = await _aliasManager.ResolveAliasAsync(rawSpec, cancellationToken);
        }
        var targetSpecifier = VersionSpecifier.Parse(rawSpec);

        var available = await _buildSource.GetAvailableVersionsAsync(cancellationToken);
        var matching = available
            .Where(b => targetSpecifier.Matches(b.Version))
            .Where(b => !arch.HasValue || b.Architecture == arch.Value)
            .Where(b => !ts.HasValue || b.ThreadSafety == ts.Value)
            .OrderByDescending(b => b.Version)
            .ToList();

        if (matching.Count == 0)
        {
            return Result.Fail<PhpInstallation>($"No remote PHP build found matching '{targetSpecifier.Raw}' (Architecture: {arch ?? Architecture.X64}, ThreadSafety: {ts ?? ThreadSafety.Nts}).");
        }

        var targetBuild = matching.First();
        var existing = await _scanner.GetInstallationAsync(targetBuild.Version, cancellationToken);

        if (existing is not null)
        {
            if (!force)
            {
                return Result.Fail<PhpInstallation>($"PHP {targetBuild.Version} is already installed at '{existing.Path}'. Use --force to reinstall.");
            }

            if (existing.IsActive)
            {
                return Result.Fail<PhpInstallation>($"PHP {targetBuild.Version} is currently active. Switch to another version or remove the junction before reinstalling.");
            }
        }

        var targetDir = Path.Combine(_scanner.VersionsDirectory, targetBuild.Version.ToString());
        var tempDownloadDir = Path.Combine(Path.GetTempPath(), "pvm_downloads");
        Directory.CreateDirectory(tempDownloadDir);

        var tempArchive = Path.Combine(tempDownloadDir, $"php-{targetBuild.Version}-{Guid.NewGuid():N}.zip");

        try
        {
            // 1. Download
            var downloadResult = await _buildSource.DownloadBuildAsync(targetBuild, tempArchive, downloadProgress, cancellationToken);
            if (downloadResult.IsFailure)
            {
                return Result.Fail<PhpInstallation>(downloadResult.Error);
            }

            // 2. Verify SHA256 Checksum
            if (!string.IsNullOrWhiteSpace(targetBuild.Checksum))
            {
                var checkResult = await _extractor.VerifyChecksumAsync(tempArchive, targetBuild.Checksum, cancellationToken);
                if (checkResult.IsFailure)
                {
                    return Result.Fail<PhpInstallation>($"Security check failed: {checkResult.Error}");
                }
            }

            // 3. Prepare target directory
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            // 4. Extract Archive
            var extractResult = await _extractor.ExtractAsync(tempArchive, targetDir, extractProgress, cancellationToken);
            if (extractResult.IsFailure)
            {
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                return Result.Fail<PhpInstallation>(extractResult.Error);
            }

            // 5. Configure default php.ini
            ConfigureDefaultIni(targetDir);

            // 6. Verify health of installed binary
            var phpExe = Path.Combine(targetDir, "php.exe");
            if (!File.Exists(phpExe))
            {
                return Result.Fail<PhpInstallation>("Extraction succeeded, but php.exe was not found in the installation directory.");
            }

            var isValid = await _phpProcess.IsValidAsync(phpExe, cancellationToken);
            if (!isValid)
            {
                return Result.Fail<PhpInstallation>("Installed php.exe failed basic runtime validation (it may be missing Visual C++ Redistributable dependencies).");
            }

            var hasIni = File.Exists(Path.Combine(targetDir, "php.ini"));
            var installation = new PhpInstallation(
                targetBuild.Version,
                targetDir,
                targetBuild.Architecture,
                targetBuild.ThreadSafety,
                HasPhpIni: hasIni,
                IsActive: false);

            return Result.Ok(installation);
        }
        finally
        {
            if (File.Exists(tempArchive))
            {
                try { File.Delete(tempArchive); } catch { }
            }
        }
    }

    private static void ConfigureDefaultIni(string targetDir)
    {
        try
        {
            var iniPath = Path.Combine(targetDir, "php.ini");
            if (File.Exists(iniPath)) return;

            var devIni = Path.Combine(targetDir, "php.ini-development");
            var prodIni = Path.Combine(targetDir, "php.ini-production");

            if (File.Exists(devIni))
            {
                File.Copy(devIni, iniPath);
            }
            else if (File.Exists(prodIni))
            {
                File.Copy(prodIni, iniPath);
            }
        }
        catch
        {
            // Non-fatal if copying ini fails
        }
    }
}
