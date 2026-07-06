using System.IO.Compression;
using System.Security.Cryptography;
using Pvm.Infrastructure.FileSystem;
using Xunit;

namespace Pvm.Infrastructure.Tests;

public class ArchiveExtractorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _zipPath;
    private readonly string _extractDir;
    private readonly string _expectedSha256;

    public ArchiveExtractorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pvm_extract_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _extractDir = Path.Combine(_tempDir, "extracted");

        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "Hello PVM Archive");

        _zipPath = Path.Combine(_tempDir, "test.zip");
        ZipFile.CreateFromDirectory(sourceDir, _zipPath);

        using var stream = File.OpenRead(_zipPath);
        var hash = SHA256.HashData(stream);
        _expectedSha256 = Convert.ToHexString(hash);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task VerifyChecksumAsync_MatchingHash_ReturnsSuccess()
    {
        var extractor = new ArchiveExtractor();
        var result = await extractor.VerifyChecksumAsync(_zipPath, _expectedSha256);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyChecksumAsync_MismatchHash_ReturnsFailure()
    {
        var extractor = new ArchiveExtractor();
        var result = await extractor.VerifyChecksumAsync(_zipPath, "0000000000000000000000000000000000000000000000000000000000000000");

        Assert.True(result.IsFailure);
        Assert.Contains("checksum mismatch", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_ValidZip_ExtractsFiles()
    {
        var extractor = new ArchiveExtractor();
        var result = await extractor.ExtractAsync(_zipPath, _extractDir);

        Assert.True(result.IsSuccess);
        var extractedFile = Path.Combine(_extractDir, "test.txt");
        Assert.True(File.Exists(extractedFile));
        Assert.Equal("Hello PVM Archive", File.ReadAllText(extractedFile));
    }
}
