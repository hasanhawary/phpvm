using System.Diagnostics.CodeAnalysis;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.FileSystem;

/// <summary>
/// Scans the local filesystem directory (e.g., %USERPROFILE%\.pvm\versions) for installed PHP versions.
/// </summary>
public sealed class InstallationDirectoryScanner : IInstallationScanner
{
    private readonly IConfigStore _configStore;
    private readonly IJunctionManager _junctionManager;

    public InstallationDirectoryScanner(IConfigStore configStore, IJunctionManager junctionManager)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _junctionManager = junctionManager ?? throw new ArgumentNullException(nameof(junctionManager));
    }

    public string VersionsDirectory => GetVersionsDirectory();

    public async Task<IReadOnlyList<PhpInstallation>> ScanInstalledAsync(CancellationToken cancellationToken = default)
    {
        var versionsDir = GetVersionsDirectory();
        if (!Directory.Exists(versionsDir))
        {
            return Array.Empty<PhpInstallation>();
        }

        var activeVersion = await GetActiveVersionAsync(cancellationToken);
        var results = new List<PhpInstallation>();

        foreach (var dir in Directory.EnumerateDirectories(versionsDir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dirName = Path.GetFileName(dir);
            if (TryDetectInstallation(dirName, dir, activeVersion, out var installation))
            {
                results.Add(installation);
            }
        }

        results.Sort((a, b) => b.Version.CompareTo(a.Version));
        return results;
    }

    public async Task<PhpInstallation?> GetInstallationAsync(PhpVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        var all = await ScanInstalledAsync(cancellationToken);
        return all.FirstOrDefault(x => x.Version == version);
    }

    public Task<PhpVersion?> GetActiveVersionAsync(CancellationToken cancellationToken = default)
    {
        var currentJunction = GetCurrentJunctionPath();
        if (!_junctionManager.IsJunction(currentJunction))
        {
            return Task.FromResult<PhpVersion?>(null);
        }

        var target = _junctionManager.GetJunctionTarget(currentJunction);
        if (string.IsNullOrEmpty(target) || !Directory.Exists(target))
        {
            return Task.FromResult<PhpVersion?>(null);
        }

        var dirName = Path.GetFileName(target.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (PhpVersion.TryParse(dirName, out var version))
        {
            return Task.FromResult<PhpVersion?>(version);
        }

        return Task.FromResult<PhpVersion?>(null);
    }

    private string GetVersionsDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".pvm", "versions");
    }

    private string GetCurrentJunctionPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".pvm", "current");
    }

    private static bool TryDetectInstallation(
        string dirName,
        string fullPath,
        PhpVersion? activeVersion,
        [NotNullWhen(true)] out PhpInstallation? installation)
    {
        installation = null;
        if (!PhpVersion.TryParse(dirName, out var version))
        {
            return false;
        }

        var phpExe = Path.Combine(fullPath, "php.exe");
        if (!File.Exists(phpExe))
        {
            return false;
        }

        var isActive = activeVersion != null && activeVersion == version;

        // In Phase 1, default detected architectures and thread safety if not encoded in dirname.
        // Future phases will inspect php -i / php -v output or directory metadata.
        var arch = Architecture.X64;
        var ts = dirName.Contains("-ts", StringComparison.OrdinalIgnoreCase)
            ? ThreadSafety.Ts
            : ThreadSafety.Nts;

        var hasIni = File.Exists(Path.Combine(fullPath, "php.ini"));
        installation = new PhpInstallation(version, fullPath, arch, ts, HasPhpIni: hasIni, IsActive: isActive);
        return true;
    }
}
