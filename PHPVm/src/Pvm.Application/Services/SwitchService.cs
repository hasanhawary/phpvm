using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Orchestrates switching the currently active PHP version by updating the NTFS junction and verifying the runtime.
/// </summary>
public sealed class SwitchService
{
    private readonly IInstallationScanner _scanner;
    private readonly IJunctionManager _junctionManager;
    private readonly IPhpProcess _phpProcess;
    private readonly PathService? _pathService;
    private readonly IAliasManager? _aliasManager;

    public SwitchService(
        IInstallationScanner scanner,
        IJunctionManager junctionManager,
        IPhpProcess phpProcess,
        PathService? pathService = null,
        IAliasManager? aliasManager = null)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _junctionManager = junctionManager ?? throw new ArgumentNullException(nameof(junctionManager));
        _phpProcess = phpProcess ?? throw new ArgumentNullException(nameof(phpProcess));
        _pathService = pathService;
        _aliasManager = aliasManager;
    }

    /// <summary>
    /// Switches the active PHP version to the specified version specifier.
    /// </summary>
    /// <param name="specifier">The target version specifier (e.g., "8.4" or "8.4.23").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the newly activated installation on success.</returns>
    public async Task<Result<PhpInstallation>> SwitchAsync(
        VersionSpecifier specifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specifier);

        var rawSpec = specifier.Raw;
        if (_aliasManager is not null)
        {
            rawSpec = await _aliasManager.ResolveAliasAsync(rawSpec, cancellationToken);
        }
        var targetSpecifier = VersionSpecifier.Parse(rawSpec);

        var installed = await _scanner.ScanInstalledAsync(cancellationToken);
        if (installed.Count == 0)
        {
            return Result.Fail<PhpInstallation>("No PHP versions are installed. Use 'pvm install <version>' first.");
        }

        PhpInstallation? target = null;
        if (targetSpecifier.IsExact && PhpVersion.TryParse(targetSpecifier.Raw, out var exactVersion))
        {
            target = installed.FirstOrDefault(x => x.Version == exactVersion);
        }
        else
        {
            // Match branch prefix (e.g., "8.4" matches highest 8.4.x)
            target = installed
                .Where(x => x.Version.ToString().StartsWith(targetSpecifier.Raw, StringComparison.OrdinalIgnoreCase) ||
                            x.Version.Branch.Equals(targetSpecifier.Raw, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();
        }

        if (target is null)
        {
            return Result.Fail<PhpInstallation>($"PHP version matching '{targetSpecifier.Raw}' is not installed.");
        }

        if (target.IsActive)
        {
            if (_pathService is not null)
            {
                await _pathService.EnsureCurrentJunctionInPathAsync(cancellationToken);
            }
            return Result.Ok(target);
        }

        var junctionPath = GetCurrentJunctionPath();
        var junctionResult = _junctionManager.CreateOrUpdateJunction(junctionPath, target.Path);
        if (junctionResult.IsFailure)
        {
            return Result.Fail<PhpInstallation>($"Failed to update NTFS junction: {junctionResult.Error}", junctionResult.Exception);
        }

        var phpExeInJunction = Path.Combine(junctionPath, "php.exe");
        var isValid = await _phpProcess.IsValidAsync(phpExeInJunction, cancellationToken);
        if (!isValid)
        {
            // Try checking the target directory directly if junction execution failed
            var targetExe = Path.Combine(target.Path, "php.exe");
            if (!await _phpProcess.IsValidAsync(targetExe, cancellationToken))
            {
                return Result.Fail<PhpInstallation>($"Switched junction to {target.Version}, but php.exe failed to execute or report a valid version.");
            }
        }

        if (_pathService is not null)
        {
            await _pathService.EnsureCurrentJunctionInPathAsync(cancellationToken);
        }

        var updatedInstallation = target with { IsActive = true };
        return Result.Ok(updatedInstallation);
    }

    private static string GetCurrentJunctionPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".pvm", "current");
    }
}
