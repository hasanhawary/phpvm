using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages checking for and applying self-updates to the PVM binary from official GitHub Releases.
/// </summary>
public interface ISelfUpdateManager
{
    /// <summary>
    /// Checks whether a newer version of PVM is available.
    /// </summary>
    Task<Result<UpdateCheckInfo>> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and installs the latest version of PVM over the current executable.
    /// </summary>
    Task<Result> ApplyUpdateAsync(UpdateCheckInfo updateInfo, CancellationToken cancellationToken = default);
}
