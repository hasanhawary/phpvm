using System.Diagnostics;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Orchestrates php.ini inspection, extension toggling, directive configuration, and editor launching for the active or target PHP version.
/// </summary>
public sealed class IniService
{
    private readonly IIniManager _iniManager;
    private readonly IInstallationScanner _scanner;

    public IniService(IIniManager iniManager, IInstallationScanner scanner)
    {
        _iniManager = iniManager ?? throw new ArgumentNullException(nameof(iniManager));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public async Task<Result<string>> GetTargetIniPathAsync(string? versionSpecifier = null, CancellationToken cancellationToken = default)
    {
        PhpInstallation? targetInst = null;
        if (!string.IsNullOrWhiteSpace(versionSpecifier))
        {
            var spec = VersionSpecifier.Parse(versionSpecifier);
            var all = await _scanner.ScanInstalledAsync(cancellationToken);
            targetInst = all.Where(x => spec.Matches(x.Version)).OrderByDescending(x => x.Version).FirstOrDefault();
            if (targetInst is null)
            {
                return Result.Fail<string>($"PHP version matching '{versionSpecifier}' is not installed.");
            }
        }
        else
        {
            var activeVer = await _scanner.GetActiveVersionAsync(cancellationToken);
            if (activeVer is not null)
            {
                targetInst = await _scanner.GetInstallationAsync(activeVer, cancellationToken);
            }

            if (targetInst is null)
            {
                return Result.Fail<string>("No active PHP version is currently set and no version was specified.");
            }
        }

        var iniPath = Path.Combine(targetInst.Path, "php.ini");
        if (!File.Exists(iniPath))
        {
            return Result.Fail<string>($"php.ini not found at '{iniPath}'. You may need to reinstall this version or copy php.ini-development.");
        }

        return Result.Ok(iniPath);
    }

    public async Task<Result<IReadOnlyList<PhpExtension>>> ListExtensionsAsync(string? versionSpecifier = null, CancellationToken cancellationToken = default)
    {
        var iniPathResult = await GetTargetIniPathAsync(versionSpecifier, cancellationToken);
        if (iniPathResult.IsFailure) return Result.Fail<IReadOnlyList<PhpExtension>>(iniPathResult.Error);

        return await _iniManager.GetExtensionsAsync(iniPathResult.Value!, cancellationToken);
    }

    public async Task<Result> ToggleExtensionAsync(string extensionName, bool enable, string? versionSpecifier = null, CancellationToken cancellationToken = default)
    {
        var iniPathResult = await GetTargetIniPathAsync(versionSpecifier, cancellationToken);
        if (iniPathResult.IsFailure) return Result.Fail(iniPathResult.Error);

        return await _iniManager.SetExtensionStatusAsync(iniPathResult.Value!, extensionName, enable, cancellationToken);
    }

    public async Task<Result<string?>> GetDirectiveAsync(string directiveName, string? versionSpecifier = null, CancellationToken cancellationToken = default)
    {
        var iniPathResult = await GetTargetIniPathAsync(versionSpecifier, cancellationToken);
        if (iniPathResult.IsFailure) return Result.Fail<string?>(iniPathResult.Error);

        return await _iniManager.GetDirectiveValueAsync(iniPathResult.Value!, directiveName, cancellationToken);
    }

    public async Task<Result> SetDirectiveAsync(string directiveName, string value, string? versionSpecifier = null, CancellationToken cancellationToken = default)
    {
        var iniPathResult = await GetTargetIniPathAsync(versionSpecifier, cancellationToken);
        if (iniPathResult.IsFailure) return Result.Fail(iniPathResult.Error);

        return await _iniManager.SetDirectiveValueAsync(iniPathResult.Value!, directiveName, value, cancellationToken);
    }

    public async Task<Result> OpenEditorAsync(string? versionSpecifier = null, CancellationToken cancellationToken = default)
    {
        var iniPathResult = await GetTargetIniPathAsync(versionSpecifier, cancellationToken);
        if (iniPathResult.IsFailure) return Result.Fail(iniPathResult.Error);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = iniPathResult.Value!,
                UseShellExecute = true
            };
            Process.Start(psi);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to launch editor for '{iniPathResult.Value}': {ex.Message}");
        }
    }
}
