using Pvm.Core.Models;
using Pvm.Core.Ports;
using Pvm.Infrastructure.Platform;
using Xunit;

namespace Pvm.Infrastructure.Tests;

public class WindowsPathManagerTests
{
    private sealed class FakeNotifier : IEnvironmentNotifier
    {
        public int NotifyCount { get; private set; }
        public Result NotifyEnvironmentChanged()
        {
            NotifyCount++;
            return Result.Ok();
        }
    }

    [Fact]
    public void GetUserPath_ReturnsListWithoutThrowing()
    {
        var notifier = new FakeNotifier();
        var pm = new WindowsPathManager(notifier);

        var path = pm.GetUserPath();

        Assert.NotNull(path);
    }

    [Fact]
    public void GetMachinePath_ReturnsListWithoutThrowing()
    {
        var notifier = new FakeNotifier();
        var pm = new WindowsPathManager(notifier);

        var path = pm.GetMachinePath();

        Assert.NotNull(path);
    }

    [Fact]
    public void FindDuplicateEntries_ReturnsListWithoutThrowing()
    {
        var notifier = new FakeNotifier();
        var pm = new WindowsPathManager(notifier);

        var dups = pm.FindDuplicateEntries();

        Assert.NotNull(dups);
    }

    [Fact]
    public void FindConflictingPhpEntries_ReturnsListWithoutThrowing()
    {
        var notifier = new FakeNotifier();
        var pm = new WindowsPathManager(notifier);

        var conflicts = pm.FindConflictingPhpEntries();

        Assert.NotNull(conflicts);
    }
}
