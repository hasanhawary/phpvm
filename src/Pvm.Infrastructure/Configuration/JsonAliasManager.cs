using System.Text.Json;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Configuration;

/// <summary>
/// Implements version alias storage using a JSON file in ~/.pvm/aliases.json.
/// </summary>
public sealed class JsonAliasManager : IAliasManager
{
    private readonly string _filePath;

    public JsonAliasManager(string? customPath = null)
    {
        if (customPath is not null)
        {
            _filePath = customPath;
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var pvmDir = Path.Combine(userProfile, ".pvm");
            Directory.CreateDirectory(pvmDir);
            _filePath = Path.Combine(pvmDir, "aliases.json");
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAliasesAsync(CancellationToken cancellationToken = default)
    {
        var dict = await ReadDictAsync(cancellationToken);
        return dict;
    }

    public async Task<string> ResolveAliasAsync(string aliasOrSpecifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasOrSpecifier)) return aliasOrSpecifier;

        var dict = await ReadDictAsync(cancellationToken);
        var current = aliasOrSpecifier;
        var depth = 0;

        while (dict.TryGetValue(current, out var next) && depth < 5)
        {
            if (string.Equals(current, next, StringComparison.OrdinalIgnoreCase)) break;
            current = next;
            depth++;
        }

        return current;
    }

    public async Task<Result> SetAliasAsync(string aliasName, string targetSpecifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasName))
        {
            return Result.Fail("Alias name cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(targetSpecifier))
        {
            return Result.Fail("Target version specifier cannot be empty.");
        }

        try
        {
            var dict = new Dictionary<string, string>(await ReadDictAsync(cancellationToken), StringComparer.OrdinalIgnoreCase);
            dict[aliasName.ToLowerInvariant()] = targetSpecifier;
            await WriteDictAsync(dict, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to save alias '{aliasName}': {ex.Message}");
        }
    }

    public async Task<Result> RemoveAliasAsync(string aliasName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasName))
        {
            return Result.Fail("Alias name cannot be empty.");
        }

        try
        {
            var dict = new Dictionary<string, string>(await ReadDictAsync(cancellationToken), StringComparer.OrdinalIgnoreCase);
            if (!dict.Remove(aliasName.ToLowerInvariant()))
            {
                return Result.Fail($"Alias '{aliasName}' does not exist.");
            }

            await WriteDictAsync(dict, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remove alias '{aliasName}': {ex.Message}");
        }
    }

    private async Task<Dictionary<string, string>> ReadDictAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var dict = JsonSerializer.Deserialize(json, PvmJsonSerializerContext.Default.DictionaryStringString);
            return dict != null
                ? new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task WriteDictAsync(Dictionary<string, string> dict, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(dict, PvmJsonSerializerContext.Default.DictionaryStringString);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }
}
