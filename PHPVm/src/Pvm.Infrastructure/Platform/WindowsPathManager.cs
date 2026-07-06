using System.Security;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Platform;

/// <summary>
/// Implements PATH environment variable inspection and modification on Windows using environment APIs.
/// </summary>
public sealed class WindowsPathManager : IPathManager
{
    private readonly IEnvironmentNotifier _notifier;

    public WindowsPathManager(IEnvironmentNotifier notifier)
    {
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
    }

    public IReadOnlyList<string> GetUserPath()
    {
        var raw = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        return ParsePathString(raw);
    }

    public IReadOnlyList<string> GetMachinePath()
    {
        var raw = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
        return ParsePathString(raw);
    }

    public Result AddToUserPath(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return Result.Fail("Cannot add an empty path entry.");
        }

        var normalizedEntry = NormalizePath(entry);
        var currentEntries = GetUserPath();

        if (currentEntries.Any(p => string.Equals(NormalizePath(p), normalizedEntry, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Ok(); // Already present
        }

        try
        {
            var raw = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            var newRaw = string.IsNullOrWhiteSpace(raw)
                ? normalizedEntry
                : $"{raw.TrimEnd(';')};{normalizedEntry}";

            Environment.SetEnvironmentVariable("PATH", newRaw, EnvironmentVariableTarget.User);
            _notifier.NotifyEnvironmentChanged();
            return Result.Ok();
        }
        catch (SecurityException ex)
        {
            return Result.Fail($"Security exception modifying User PATH: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to add entry to User PATH: {ex.Message}");
        }
    }

    public Result RemoveFromUserPath(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return Result.Fail("Cannot remove an empty path entry.");
        }

        var normalizedEntry = NormalizePath(entry);
        var currentEntries = GetUserPath();

        var remaining = currentEntries
            .Where(p => !string.Equals(NormalizePath(p), normalizedEntry, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (remaining.Count == currentEntries.Count)
        {
            return Result.Ok(); // Was not present
        }

        try
        {
            var newRaw = string.Join(";", remaining);
            Environment.SetEnvironmentVariable("PATH", newRaw, EnvironmentVariableTarget.User);
            _notifier.NotifyEnvironmentChanged();
            return Result.Ok();
        }
        catch (SecurityException ex)
        {
            return Result.Fail($"Security exception modifying User PATH: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remove entry from User PATH: {ex.Message}");
        }
    }

    public bool ContainsEntry(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry)) return false;
        var normalized = NormalizePath(entry);

        return GetUserPath().Any(p => string.Equals(NormalizePath(p), normalized, StringComparison.OrdinalIgnoreCase)) ||
               GetMachinePath().Any(p => string.Equals(NormalizePath(p), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> FindDuplicateEntries()
    {
        var entries = GetUserPath();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var norm = NormalizePath(entry);
            if (!seen.Add(norm))
            {
                duplicates.Add(norm);
            }
        }

        return duplicates.ToList();
    }

    public Result CleanDuplicateEntries()
    {
        try
        {
            var entries = GetUserPath();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var distinct = new List<string>();

            foreach (var entry in entries)
            {
                var norm = NormalizePath(entry);
                if (seen.Add(norm))
                {
                    distinct.Add(entry);
                }
            }

            if (distinct.Count == entries.Count)
            {
                return Result.Ok(); // No duplicates
            }

            var newRaw = string.Join(";", distinct);
            Environment.SetEnvironmentVariable("PATH", newRaw, EnvironmentVariableTarget.User);
            _notifier.NotifyEnvironmentChanged();
            return Result.Ok();
        }
        catch (SecurityException ex)
        {
            return Result.Fail($"Security exception modifying User PATH: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to clean duplicates from User PATH: {ex.Message}");
        }
    }

    public IReadOnlyList<string> FindConflictingPhpEntries()
    {
        var allEntries = GetUserPath().Concat(GetMachinePath()).Distinct(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<string>();

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var pvmRoot = Path.GetFullPath(Path.Combine(userProfile, ".pvm"));

        foreach (var entry in allEntries)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entry) || !Directory.Exists(entry))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(entry);
                if (fullPath.StartsWith(pvmRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var phpExe = Path.Combine(fullPath, "php.exe");
                if (File.Exists(phpExe))
                {
                    conflicts.Add(fullPath);
                }
            }
            catch
            {
                // Skip invalid or inaccessible directory paths in PATH
            }
        }

        return conflicts;
    }

    private static IReadOnlyList<string> ParsePathString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
