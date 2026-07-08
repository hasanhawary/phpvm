using Pvm.Core.Models;
using Pvm.Infrastructure.Configuration;
using Xunit;

namespace Pvm.Infrastructure.Tests;

public class JsonConfigStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configFile;

    public JsonConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pvm_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _configFile = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsDefaultConfigAndCreatesFile()
    {
        var store = new JsonConfigStore(_configFile);
        var config = store.Load();

        Assert.NotNull(config);
        Assert.Equal(1, config.ConfigVersion);
        Assert.True(File.Exists(_configFile));
    }

    [Fact]
    public void SaveAndLoad_PersistsCustomValues()
    {
        var store = new JsonConfigStore(_configFile);
        var config = new PhpVmConfig
        {
            ConfigVersion = 2,
            DefaultIniTemplate = "production",
            VerifyChecksums = false,
            TimeoutSeconds = 600
        };
        config.Aliases["work"] = "8.3.10";

        store.Save(config);
        var reloaded = store.Load();

        Assert.Equal(2, reloaded.ConfigVersion);
        Assert.Equal("production", reloaded.DefaultIniTemplate);
        Assert.False(reloaded.VerifyChecksums);
        Assert.Equal(600, reloaded.TimeoutSeconds);
        Assert.Equal("8.3.10", reloaded.Aliases["work"]);
    }
}
