using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Configuration;

/// <summary>
/// Implements reading and modifying php.ini files on Windows.
/// </summary>
public sealed class PhpIniManager : IIniManager
{
    public async Task<Result<IReadOnlyList<PhpExtension>>> GetExtensionsAsync(string iniPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(iniPath))
        {
            return Result.Fail<IReadOnlyList<PhpExtension>>($"php.ini file not found at '{iniPath}'.");
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(iniPath, cancellationToken);
            var extensions = new List<PhpExtension>();

            foreach (var line in lines)
            {
                if (TryParseExtensionLine(line, out var name, out var isEnabled, out var isZend))
                {
                    extensions.Add(new PhpExtension(name!, isEnabled, isZend, line));
                }
            }

            return Result.Ok<IReadOnlyList<PhpExtension>>(extensions);
        }
        catch (Exception ex)
        {
            return Result.Fail<IReadOnlyList<PhpExtension>>($"Failed to read php.ini: {ex.Message}");
        }
    }

    public async Task<Result> SetExtensionStatusAsync(string iniPath, string extensionName, bool enable, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            return Result.Fail("Extension name cannot be empty.");
        }

        if (!File.Exists(iniPath))
        {
            return Result.Fail($"php.ini file not found at '{iniPath}'.");
        }

        try
        {
            var lines = (await File.ReadAllLinesAsync(iniPath, cancellationToken)).ToList();
            var targetNorm = NormalizeExtensionName(extensionName);
            var found = false;
            var lastExtensionIndex = -1;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (TryParseExtensionLine(line, out var name, out var isCurrentlyEnabled, out var isZend))
                {
                    lastExtensionIndex = i;
                    if (string.Equals(name, targetNorm, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        if (enable && !isCurrentlyEnabled)
                        {
                            // Remove leading comment semicolon(s) and spaces
                            var trimmed = line.TrimStart(';', ' ', '\t');
                            lines[i] = trimmed;
                        }
                        else if (!enable && isCurrentlyEnabled)
                        {
                            // Prepend comment semicolon
                            lines[i] = $";{line}";
                        }
                        break;
                    }
                }
            }

            if (!found)
            {
                if (!enable)
                {
                    return Result.Ok(); // Already disabled / not present
                }

                var newLine = $"extension={extensionName}";
                if (lastExtensionIndex >= 0 && lastExtensionIndex < lines.Count)
                {
                    lines.Insert(lastExtensionIndex + 1, newLine);
                }
                else
                {
                    lines.Add(newLine);
                }
            }

            await File.WriteAllLinesAsync(iniPath, lines, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update extension status in php.ini: {ex.Message}");
        }
    }

    public async Task<Result<string?>> GetDirectiveValueAsync(string iniPath, string directiveName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directiveName))
        {
            return Result.Fail<string?>("Directive name cannot be empty.");
        }

        if (!File.Exists(iniPath))
        {
            return Result.Fail<string?>($"php.ini file not found at '{iniPath}'.");
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(iniPath, cancellationToken);
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(';')) continue; // Commented out

                if (TryParseDirectiveLine(trimmed, directiveName, out var val))
                {
                    return Result.Ok<string?>(val);
                }
            }

            return Result.Ok<string?>(null);
        }
        catch (Exception ex)
        {
            return Result.Fail<string?>($"Failed to read directive from php.ini: {ex.Message}");
        }
    }

    public async Task<Result> SetDirectiveValueAsync(string iniPath, string directiveName, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directiveName))
        {
            return Result.Fail("Directive name cannot be empty.");
        }

        if (!File.Exists(iniPath))
        {
            return Result.Fail($"php.ini file not found at '{iniPath}'.");
        }

        try
        {
            var lines = (await File.ReadAllLinesAsync(iniPath, cancellationToken)).ToList();
            var found = false;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var cleanLine = line.TrimStart(';', ' ', '\t');
                if (TryParseDirectiveLine(cleanLine, directiveName, out _))
                {
                    lines[i] = $"{directiveName} = {value}";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                lines.Add($"{directiveName} = {value}");
            }

            await File.WriteAllLinesAsync(iniPath, lines, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to set directive in php.ini: {ex.Message}");
        }
    }

    private static bool TryParseExtensionLine(string line, out string? name, out bool isEnabled, out bool isZend)
    {
        name = null;
        isEnabled = false;
        isZend = false;

        if (string.IsNullOrWhiteSpace(line)) return false;

        var trimmed = line.TrimStart();
        isEnabled = !trimmed.StartsWith(';');
        var clean = trimmed.TrimStart(';', ' ', '\t');

        if (clean.StartsWith("extension=", StringComparison.OrdinalIgnoreCase) ||
            clean.StartsWith("extension ", StringComparison.OrdinalIgnoreCase))
        {
            var idx = clean.IndexOf('=');
            if (idx >= 0)
            {
                var rawVal = clean.Substring(idx + 1);
                name = NormalizeExtensionName(rawVal);
                return !string.IsNullOrWhiteSpace(name);
            }
        }
        else if (clean.StartsWith("zend_extension=", StringComparison.OrdinalIgnoreCase) ||
                 clean.StartsWith("zend_extension ", StringComparison.OrdinalIgnoreCase))
        {
            isZend = true;
            var idx = clean.IndexOf('=');
            if (idx >= 0)
            {
                var rawVal = clean.Substring(idx + 1);
                name = NormalizeExtensionName(rawVal);
                return !string.IsNullOrWhiteSpace(name);
            }
        }

        return false;
    }

    private static bool TryParseDirectiveLine(string line, string targetDirective, out string? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        if (line.StartsWith(targetDirective, StringComparison.OrdinalIgnoreCase))
        {
            if (line.Length > targetDirective.Length)
            {
                var nextChar = line[targetDirective.Length];
                if (nextChar == ' ' || nextChar == '\t' || nextChar == '=')
                {
                    var idx = line.IndexOf('=');
                    if (idx >= 0)
                    {
                        value = line.Substring(idx + 1).Trim().Trim('"', '\'');
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string NormalizeExtensionName(string rawValue)
    {
        var name = rawValue.Trim().Trim('"', '\'');
        if (name.StartsWith("php_", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(4);
        }
        if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - 4);
        }
        return name.ToLowerInvariant();
    }
}
