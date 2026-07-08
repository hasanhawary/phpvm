using Pvm.Infrastructure.Configuration;
using Xunit;

namespace Pvm.Infrastructure.Tests;

public class JsonAliasManagerTests : IDisposable
{
    private readonly string _tempFile;

    public JsonAliasManagerTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), "pvm_aliases_" + Guid.NewGuid().ToString("N") + ".json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public async Task SetAndResolveAlias_WorksCorrectly()
    {
        var manager = new JsonAliasManager(_tempFile);
        await manager.SetAliasAsync("lts", "8.2");
        await manager.SetAliasAsync("prod", "lts"); // chained

        var ltsRes = await manager.ResolveAliasAsync("lts");
        var prodRes = await manager.ResolveAliasAsync("prod");
        var unknownRes = await manager.ResolveAliasAsync("8.4");

        Assert.Equal("8.2", ltsRes);
        Assert.Equal("8.2", prodRes);
        Assert.Equal("8.4", unknownRes);
    }

    [Fact]
    public async Task RemoveAlias_RemovesSuccessfully()
    {
        var manager = new JsonAliasManager(_tempFile);
        await manager.SetAliasAsync("test", "8.0");
        await manager.RemoveAliasAsync("test");

        var all = await manager.GetAllAliasesAsync();
        Assert.Empty(all);
    }
}
