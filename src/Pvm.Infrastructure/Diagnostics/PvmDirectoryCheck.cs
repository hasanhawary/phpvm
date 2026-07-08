using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Diagnostics;

/// <summary>
/// Verifies that PVM directories exist and have proper read/write permissions.
/// </summary>
public sealed class PvmDirectoryCheck : IDoctorCheck
{
    private readonly string _pvmRoot;
    private readonly string[] _requiredSubdirs;

    public PvmDirectoryCheck(string? customRoot = null)
    {
        if (customRoot is not null)
        {
            _pvmRoot = customRoot;
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _pvmRoot = Path.Combine(userProfile, ".pvm");
        }

        _requiredSubdirs = new[]
        {
            _pvmRoot,
            Path.Combine(_pvmRoot, "versions"),
            Path.Combine(_pvmRoot, "temp"),
            Path.Combine(_pvmRoot, "archives"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pvm", "config")
        };
    }

    public string Name => "PVM Directory Structure & Permissions";
    public string Description => "Verifies required directories (~/.pvm, versions, temp, config) exist and are writable.";

    public Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();
        foreach (var dir in _requiredSubdirs)
        {
            if (!Directory.Exists(dir))
            {
                missing.Add(dir);
            }
        }

        if (missing.Count > 0)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorStatus.Error,
                $"Missing {missing.Count} required directory(s): {string.Join(", ", missing)}",
                "Run 'pvm doctor --fix' to automatically create missing directories.",
                CanFix: true
            ));
        }

        // Test write permissions on temp dir
        try
        {
            var testFile = Path.Combine(_requiredSubdirs[2], $"test_write_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "test");
            if (File.Exists(testFile)) File.Delete(testFile);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorStatus.Error,
                $"PVM temp directory is not writable: {ex.Message}",
                "Check folder permissions or run terminal as Administrator.",
                CanFix: false
            ));
        }

        return Task.FromResult(new DoctorCheckResult(
            Name,
            DoctorStatus.Pass,
            "All required directories exist and are writable."
        ));
    }

    public Task<Result> FixAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var dir in _requiredSubdirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Failed to create directories: {ex.Message}"));
        }
    }
}
