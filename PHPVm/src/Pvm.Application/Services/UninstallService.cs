using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Manages safely removing installed PHP versions from disk.
/// </summary>
public sealed class UninstallService
{
    private readonly IInstallationScanner _scanner;
    private readonly IAliasManager? _aliasManager;

    public UninstallService(IInstallationScanner scanner, IAliasManager? aliasManager = null)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _aliasManager = aliasManager;
    }

    public async Task<Result> UninstallAsync(VersionSpecifier specifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specifier);

        var rawSpec = specifier.Raw;
        if (_aliasManager is not null)
        {
            rawSpec = await _aliasManager.ResolveAliasAsync(rawSpec, cancellationToken);
        }
        var targetSpecifier = VersionSpecifier.Parse(rawSpec);

        var all = await _scanner.ScanInstalledAsync(cancellationToken);
        var matching = all.Where(x => targetSpecifier.Matches(x.Version)).ToList();

        if (matching.Count == 0)
        {
            return Result.Fail($"No installed PHP version found matching '{targetSpecifier.Raw}'.");
        }

        if (matching.Count > 1 && !targetSpecifier.IsExact)
        {
            var matchedVersions = string.Join(", ", matching.Select(x => x.Version.ToString()));
            return Result.Fail($"Specifier '{targetSpecifier.Raw}' matched multiple versions ({matchedVersions}). Please specify an exact version to uninstall.");
        }

        var target = matching.First();
        if (target.IsActive)
        {
            return Result.Fail($"Cannot uninstall PHP {target.Version} because it is currently the active version. Switch to another version first.");
        }

        try
        {
            if (Directory.Exists(target.Path))
            {
                Directory.Delete(target.Path, true);
            }
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to delete installation directory '{target.Path}': {ex.Message}");
        }
    }
}
