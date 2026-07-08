using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Infrastructure.Diagnostics;
using Xunit;

namespace Pvm.Infrastructure.Tests;

public class PvmDirectoryCheckTests : IDisposable
{
    private readonly string _tempRoot;

    public PvmDirectoryCheckTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "pvm_doc_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true);
    }

    [Fact]
    public async Task RunCheckAsync_WhenDirectoriesMissing_ReturnsErrorAndCanFix()
    {
        var check = new PvmDirectoryCheck(_tempRoot);
        var result = await check.RunCheckAsync();

        Assert.Equal(DoctorStatus.Error, result.Status);
        Assert.True(result.CanFix);
    }

    [Fact]
    public async Task FixAsync_CreatesMissingDirectories()
    {
        var check = new PvmDirectoryCheck(_tempRoot);
        var fixRes = await check.FixAsync();

        Assert.True(fixRes.IsSuccess);
        Assert.True(Directory.Exists(_tempRoot));
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "versions")));

        var checkRes = await check.RunCheckAsync();
        Assert.Equal(DoctorStatus.Pass, checkRes.Status);
    }
}
