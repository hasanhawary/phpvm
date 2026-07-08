using Pvm.Core.Models;
using Xunit;

namespace Pvm.Core.Tests;

public class PhpVersionTests
{
    [Theory]
    [InlineData("8.4.23", 8, 4, 23, "8.4")]
    [InlineData("php-8.3.10", 8, 3, 10, "8.3")]
    [InlineData("v8.2.0", 8, 2, 0, "8.2")]
    public void TryParse_ValidVersions_ReturnsTrueAndCorrectComponents(string input, int major, int minor, int patch, string expectedBranch)
    {
        var success = PhpVersion.TryParse(input, out var version);

        Assert.True(success);
        Assert.NotNull(version);
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(expectedBranch, version.Branch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("8.4")]
    [InlineData("invalid")]
    [InlineData("8.-1.0")]
    public void TryParse_InvalidVersions_ReturnsFalse(string input)
    {
        var success = PhpVersion.TryParse(input, out var version);

        Assert.False(success);
        Assert.Null(version);
    }

    [Fact]
    public void CompareTo_SortsCorrectly()
    {
        var v1 = new PhpVersion(8, 2, 10);
        var v2 = new PhpVersion(8, 3, 0);
        var v3 = new PhpVersion(8, 4, 1);

        var list = new List<PhpVersion> { v2, v3, v1 };
        list.Sort();

        Assert.Equal(v1, list[0]);
        Assert.Equal(v2, list[1]);
        Assert.Equal(v3, list[2]);
    }

    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        var v1 = new PhpVersion(8, 4, 0);
        var v2 = new PhpVersion(8, 4, 0);

        Assert.True(v1 == v2);
        Assert.Equal(v1, v2);
        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }
}
