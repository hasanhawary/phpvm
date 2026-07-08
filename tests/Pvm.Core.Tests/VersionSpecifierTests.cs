using Pvm.Core.Models;
using Xunit;

namespace Pvm.Core.Tests;

public class VersionSpecifierTests
{
    [Theory]
    [InlineData("8.4.23", true)]
    [InlineData("8.4", false)]
    [InlineData("latest", false)]
    [InlineData("default", false)]
    public void Parse_SetsIsExactCorrectly(string input, bool expectedExact)
    {
        var specifier = VersionSpecifier.Parse(input);

        Assert.Equal(input, specifier.Raw);
        Assert.Equal(expectedExact, specifier.IsExact);
    }

    [Fact]
    public void Parse_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => VersionSpecifier.Parse(" "));
    }
}
