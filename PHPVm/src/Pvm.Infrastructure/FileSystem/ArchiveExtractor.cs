using System.IO.Compression;
using System.Security.Cryptography;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.FileSystem;

/// <summary>
/// Implementation of IArchiveExtractor using native .NET cryptography and zip compression.
/// </summary>
public sealed class ArchiveExtractor : IArchiveExtractor
{
    public async Task<Result> VerifyChecksumAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return Result.Fail($"Archive file not found for checksum verification: {filePath}");
        }

        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            return Result.Fail("Expected SHA256 checksum was empty.");
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
            var actualHash = Convert.ToHexString(hashBytes);

            if (!actualHash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Fail($"SHA256 checksum mismatch. Expected: {expectedSha256}, Actual: {actualHash}");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to compute SHA256 checksum: {ex.Message}");
        }
    }

    public async Task<Result> ExtractAsync(string archivePath, string destinationDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
        {
            return Result.Fail($"Archive file not found for extraction: {archivePath}");
        }

        try
        {
            Directory.CreateDirectory(destinationDirectory);

            using var archive = ZipFile.OpenRead(archivePath);
            var totalEntries = archive.Entries.Count;
            if (totalEntries == 0)
            {
                return Result.Fail("Archive is empty.");
            }

            int extractedCount = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
                if (!destinationPath.StartsWith(Path.GetFullPath(destinationDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Fail($"Zip slip vulnerability detected in archive entry: {entry.FullName}");
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    var parentDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    // Extract file asynchronously
                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await entryStream.CopyToAsync(fileStream, cancellationToken);
                    }
                }

                extractedCount++;
                progress?.Report((double)extractedCount / totalEntries * 100.0);
            }

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Extraction was cancelled.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to extract archive: {ex.Message}");
        }
    }
}
