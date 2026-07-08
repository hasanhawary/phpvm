using Pvm.Infrastructure.Configuration;
using Xunit;

namespace Pvm.Infrastructure.Tests;

public class PhpIniManagerTests : IDisposable
{
    private readonly string _tempIni;

    public PhpIniManagerTests()
    {
        _tempIni = Path.Combine(Path.GetTempPath(), "pvm_test_" + Guid.NewGuid().ToString("N") + ".ini");
        File.WriteAllLines(_tempIni, new[]
        {
            "; This is a sample php.ini",
            "memory_limit = 128M",
            ";max_execution_time = 30",
            "extension=curl",
            ";extension=php_mbstring.dll",
            "zend_extension=xdebug"
        });
    }

    public void Dispose()
    {
        if (File.Exists(_tempIni)) File.Delete(_tempIni);
    }

    [Fact]
    public async Task GetExtensionsAsync_ReturnsAllExtensionsWithStatus()
    {
        var manager = new PhpIniManager();
        var result = await manager.GetExtensionsAsync(_tempIni);

        Assert.True(result.IsSuccess);
        var exts = result.Value!;
        Assert.Equal(3, exts.Count);

        var curl = exts.First(x => x.Name == "curl");
        Assert.True(curl.IsEnabled);
        Assert.False(curl.IsZendExtension);

        var mbstring = exts.First(x => x.Name == "mbstring");
        Assert.False(mbstring.IsEnabled);
        Assert.False(mbstring.IsZendExtension);

        var xdebug = exts.First(x => x.Name == "xdebug");
        Assert.True(xdebug.IsEnabled);
        Assert.True(xdebug.IsZendExtension);
    }

    [Fact]
    public async Task SetExtensionStatusAsync_EnablesDisabledExtension()
    {
        var manager = new PhpIniManager();
        var result = await manager.SetExtensionStatusAsync(_tempIni, "mbstring", true);

        Assert.True(result.IsSuccess);
        var exts = (await manager.GetExtensionsAsync(_tempIni)).Value!;
        Assert.True(exts.First(x => x.Name == "mbstring").IsEnabled);
    }

    [Fact]
    public async Task SetExtensionStatusAsync_DisablesEnabledExtension()
    {
        var manager = new PhpIniManager();
        var result = await manager.SetExtensionStatusAsync(_tempIni, "curl", false);

        Assert.True(result.IsSuccess);
        var exts = (await manager.GetExtensionsAsync(_tempIni)).Value!;
        Assert.False(exts.First(x => x.Name == "curl").IsEnabled);
    }

    [Fact]
    public async Task GetDirectiveValueAsync_ReturnsCorrectValue()
    {
        var manager = new PhpIniManager();
        var result = await manager.GetDirectiveValueAsync(_tempIni, "memory_limit");

        Assert.True(result.IsSuccess);
        Assert.Equal("128M", result.Value);
    }

    [Fact]
    public async Task SetDirectiveValueAsync_UpdatesExistingDirective()
    {
        var manager = new PhpIniManager();
        var result = await manager.SetDirectiveValueAsync(_tempIni, "memory_limit", "512M");

        Assert.True(result.IsSuccess);
        var val = (await manager.GetDirectiveValueAsync(_tempIni, "memory_limit")).Value;
        Assert.Equal("512M", val);
    }
}
