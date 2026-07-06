using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages version aliases (such as "default", "lts", "prod") mapping to version specifiers.
/// </summary>
public interface IAliasManager
{
    /// <summary>
    /// Gets all configured aliases.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAliasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alias or version specifier string to its underlying version specifier.
    /// If the input is not an alias, it is returned unchanged.
    /// </summary>
    Task<string> ResolveAliasAsync(string aliasOrSpecifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an alias mapping to a target version specifier.
    /// </summary>
    Task<Result> SetAliasAsync(string aliasName, string targetSpecifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an existing alias.
    /// </summary>
    Task<Result> RemoveAliasAsync(string aliasName, CancellationToken cancellationToken = default);
}
