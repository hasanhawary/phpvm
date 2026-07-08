using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Orchestrates version alias creation, removal, and resolution.
/// </summary>
public sealed class AliasService
{
    private readonly IAliasManager _aliasManager;

    public AliasService(IAliasManager aliasManager)
    {
        _aliasManager = aliasManager ?? throw new ArgumentNullException(nameof(aliasManager));
    }

    public Task<IReadOnlyDictionary<string, string>> GetAllAliasesAsync(CancellationToken cancellationToken = default)
        => _aliasManager.GetAllAliasesAsync(cancellationToken);

    public Task<string> ResolveAsync(string aliasOrSpecifier, CancellationToken cancellationToken = default)
        => _aliasManager.ResolveAliasAsync(aliasOrSpecifier, cancellationToken);

    public Task<Result> SetAliasAsync(string aliasName, string targetSpecifier, CancellationToken cancellationToken = default)
        => _aliasManager.SetAliasAsync(aliasName, targetSpecifier, cancellationToken);

    public Task<Result> RemoveAliasAsync(string aliasName, CancellationToken cancellationToken = default)
        => _aliasManager.RemoveAliasAsync(aliasName, cancellationToken);
}
