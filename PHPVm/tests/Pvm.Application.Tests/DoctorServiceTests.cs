using Pvm.Application.Services;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Application.Tests;

public class DoctorServiceTests
{
    private class FakeCheck : IDoctorCheck
    {
        public string Name => "Fake Check";
        public string Description => "Test check";
        public bool RunCalled { get; private set; }
        public bool FixCalled { get; private set; }

        public Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default)
        {
            RunCalled = true;
            return Task.FromResult(new DoctorCheckResult(Name, DoctorStatus.Warning, "Warning message", CanFix: true));
        }

        public Task<Result> FixAsync(CancellationToken cancellationToken = default)
        {
            FixCalled = true;
            return Task.FromResult(Result.Ok());
        }
    }

    [Fact]
    public async Task RunAllChecksAsync_ExecutesAllChecks()
    {
        var check1 = new FakeCheck();
        var check2 = new FakeCheck();
        var service = new DoctorService(new[] { check1, check2 });

        var results = await service.RunAllChecksAsync();

        Assert.Equal(2, results.Count);
        Assert.True(check1.RunCalled);
        Assert.True(check2.RunCalled);
    }

    [Fact]
    public async Task FixAllAsync_CallsFixOnFixableWarningsAndErrors()
    {
        var check = new FakeCheck();
        var service = new DoctorService(new[] { check });

        var res = await service.FixAllAsync();

        Assert.True(res.IsSuccess);
        Assert.Equal(1, res.Value);
        Assert.True(check.FixCalled);
    }
}
