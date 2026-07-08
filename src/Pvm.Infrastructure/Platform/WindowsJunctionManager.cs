using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Platform;

/// <summary>
/// Manages NTFS directory junctions on Windows using native APIs and cmd.exe fallback.
/// </summary>
public sealed class WindowsJunctionManager : IJunctionManager
{
    public Result CreateOrUpdateJunction(string junctionPath, string targetPath)
    {
        if (string.IsNullOrWhiteSpace(junctionPath))
            return Result.Fail("Junction path cannot be empty.");
        if (string.IsNullOrWhiteSpace(targetPath))
            return Result.Fail("Target path cannot be empty.");

        try
        {
            var fullTarget = Path.GetFullPath(targetPath);
            var fullJunction = Path.GetFullPath(junctionPath);

            if (!Directory.Exists(fullTarget))
            {
                return Result.Fail($"Target directory does not exist: {fullTarget}");
            }

            var parentDir = Path.GetDirectoryName(fullJunction);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            if (Directory.Exists(fullJunction) || File.Exists(fullJunction))
            {
                var deleteResult = DeleteJunction(fullJunction);
                if (deleteResult.IsFailure)
                {
                    return deleteResult;
                }
            }

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{fullJunction}\" \"{fullTarget}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return Result.Fail("Failed to start cmd.exe to create NTFS junction.");
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                return Result.Fail($"mklink /J failed with exit code {process.ExitCode}: {error}");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Exception creating NTFS junction: {ex.Message}", ex);
        }
    }

    public Result DeleteJunction(string junctionPath)
    {
        if (string.IsNullOrWhiteSpace(junctionPath))
            return Result.Fail("Junction path cannot be empty.");

        try
        {
            var fullPath = Path.GetFullPath(junctionPath);
            if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
            {
                return Result.Ok();
            }

            if (IsJunction(fullPath))
            {
                Directory.Delete(fullPath, false);
            }
            else if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
            else if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to delete junction at {junctionPath}: {ex.Message}", ex);
        }
    }

    public string? GetJunctionTarget(string junctionPath)
    {
        if (string.IsNullOrWhiteSpace(junctionPath) || !Directory.Exists(junctionPath))
        {
            return null;
        }

        try
        {
            var dirInfo = new DirectoryInfo(junctionPath);
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                var target = dirInfo.LinkTarget;
                if (!string.IsNullOrEmpty(target))
                {
                    return Path.GetFullPath(target);
                }
            }
        }
        catch
        {
            // Ignore access errors or non-reparse points
        }

        return null;
    }

    public bool IsJunction(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch
        {
            return false;
        }
    }
}
