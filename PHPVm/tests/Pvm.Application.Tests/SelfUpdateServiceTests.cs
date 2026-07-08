using Pvm.Application.Services;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Application.Tests;

public class SelfUpdateServiceTests
{
    private class FakeUpdateManager : ISelfUpdateManager
    {
        public bool CheckCalled { get; private set; }
        public bool ApplyCalled { get; private set; }

        public Task<Result<UpdateCheckInfo>> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            CheckCalled = true;
            var info = new UpdateCheckInfo("1.0.0", "1.1.0", true, "https://example.com/pvm.zip", "Release notes");
            return Task.FromResult(Result.Ok(info));
        }

        public Task<Result> ApplyUpdateAsync(UpdateCheckInfo updateInfo, CancellationToken cancellationToken = default)
        {
            ApplyCalled = true;
            return Task.FromResult(Result.Ok());
        }
    }

    [Fact]
    public async Task CheckForUpdateAsync_CallsManagerAndReturnsInfo()
    {
        var mgr = new FakeUpdateManager();
        var service = new SelfUpdateService(mgr);

        var res = await service.CheckForUpdateAsync();

        Assert.True(res.IsSuccess);
        Assert.True(mgr.CheckCalled);
        Assert.Equal("1.1.0", res.Value.LatestVersion);
    }

    [Fact]
    public async Task ApplyUpdateAsync_CallsManagerAndReturnsSuccess()
    {
        var mgr = new FakeUpdateManager();
        var service = new SelfUpdateService(mgr);
        var info = new UpdateCheckInfo("1.0.0", "1.1.0", true, "https://example.com/pvm.zip", null);

        var res = await service.ApplyUpdateAsync(info);

        Assert.True(res.IsSuccess);
        Assert.True(mgr.ApplyCalled);
    }
}
