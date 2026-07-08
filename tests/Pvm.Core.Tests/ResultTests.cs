using Pvm.Core.Models;
using Xunit;

namespace Pvm.Core.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessResult()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void Fail_ReturnsFailureResult()
    {
        var result = Result.Fail("Something went wrong.");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong.", result.Error);
    }

    [Fact]
    public void OkWithValue_ReturnsValue()
    {
        var result = Result.Ok("test value");

        Assert.True(result.IsSuccess);
        Assert.Equal("test value", result.Value);
    }

    [Fact]
    public void FailWithValue_AccessingValueThrowsInvalidOperationException()
    {
        var result = Result.Fail<string>("Failed operation.");

        Assert.True(result.IsFailure);
        var ex = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("Failed operation.", ex.Message);
    }
}
