using System.Diagnostics;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Platform;

/// <summary>
/// Executes PHP binaries and retrieves version, loaded extensions, and configuration info.
/// </summary>
public sealed class PhpProcessRunner : IPhpProcess
{
    public async Task<PhpVersion?> GetVersionAsync(string phpExePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(phpExePath)) return null;

        try
        {
            var output = await RunCommandAsync(phpExePath, "-v", cancellationToken);
            if (string.IsNullOrWhiteSpace(output)) return null;

            var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (firstLine is not null && firstLine.StartsWith("PHP ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && PhpVersion.TryParse(parts[1], out var version))
                {
                    return version;
                }
            }
        }
        catch
        {
            // Ignore execution errors
        }

        return null;
    }

    public async Task<IReadOnlyList<string>> GetLoadedExtensionsAsync(string phpExePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(phpExePath)) return Array.Empty<string>();

        try
        {
            var output = await RunCommandAsync(phpExePath, "-m", cancellationToken);
            if (string.IsNullOrWhiteSpace(output)) return Array.Empty<string>();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var extensions = new List<string>();
            var inSection = false;

            foreach (var line in lines)
            {
                if (line.Equals("[PHP Modules]", StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    continue;
                }
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    inSection = false;
                    continue;
                }
                if (inSection && !string.IsNullOrWhiteSpace(line))
                {
                    extensions.Add(line);
                }
            }

            return extensions;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<string?> GetIniPathAsync(string phpExePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(phpExePath)) return null;

        try
        {
            var output = await RunCommandAsync(phpExePath, "--ini", cancellationToken);
            if (string.IsNullOrWhiteSpace(output)) return null;

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("Loaded Configuration File:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var path = parts[1].Trim();
                        if (!string.Equals(path, "(none)", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(path))
                        {
                            return path;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore execution errors
        }

        return null;
    }

    public async Task<PhpRuntimeInfo> GetConfigurationAsync(string phpExePath, CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(phpExePath, cancellationToken) ?? new PhpVersion(0, 0, 0);
        var iniPath = await GetIniPathAsync(phpExePath, cancellationToken);
        var extensions = await GetLoadedExtensionsAsync(phpExePath, cancellationToken);

        return new PhpRuntimeInfo(version, iniPath, extensions);
    }

    public async Task<bool> IsValidAsync(string phpExePath, CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(phpExePath, cancellationToken);
        return version is not null && version.Major > 0;
    }

    private static async Task<string> RunCommandAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return await outputTask;
    }
}
