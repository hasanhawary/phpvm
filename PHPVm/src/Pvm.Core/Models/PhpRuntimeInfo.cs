namespace Pvm.Core.Models;

/// <summary>
/// Represents structured information returned by querying a running PHP executable.
/// </summary>
/// <param name="Version">The PHP version reported by the executable.</param>
/// <param name="IniPath">The path to the loaded php.ini file, if any.</param>
/// <param name="LoadedExtensions">The list of loaded PHP extensions.</param>
public sealed record PhpRuntimeInfo(
    PhpVersion Version,
    string? IniPath,
    IReadOnlyList<string> LoadedExtensions
);
