using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Diagnostics;

/// <summary>
/// Verifies that PVM's current junction is registered in the User PATH.
/// </summary>
public sealed class JunctionPathCheck : IDoctorCheck
{
    private readonly IPathManager _pathManager;

    public JunctionPathCheck(IPathManager pathManager)
    {
        _pathManager = pathManager ?? throw new ArgumentNullException(nameof(pathManager));
    }

    public string Name => "PVM Junction PATH Registration";
    public string Description => "Verifies that ~/.pvm/current is registered in your Windows User PATH.";

    public Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var junctionPath = Path.Combine(userProfile, ".pvm", "current");
        var entries = _pathManager.GetUserPath();
        var inPath = entries.Any(x => string.Equals(x.TrimEnd('\\', '/'), junctionPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase));

        if (!inPath)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorStatus.Warning,
                "PVM junction ('~/.pvm/current') is not currently in your User PATH.",
                "Run 'pvm doctor --fix' or 'pvm use <version>' to automatically register it.",
                CanFix: true
            ));
        }

        return Task.FromResult(new DoctorCheckResult(
            Name,
            DoctorStatus.Pass,
            "PVM junction ('~/.pvm/current') is properly registered in User PATH."
        ));
    }

    public Task<Result> FixAsync(CancellationToken cancellationToken = default)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var junctionPath = Path.Combine(userProfile, ".pvm", "current");
        return Task.FromResult(_pathManager.AddToUserPath(junctionPath));
    }
}
