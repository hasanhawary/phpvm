using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

public sealed class SelfUpdateService
{
    private readonly ISelfUpdateManager _updateManager;

    public SelfUpdateService(ISelfUpdateManager updateManager)
    {
        _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
    }

    public async Task<Result<UpdateCheckInfo>> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        return await _updateManager.CheckForUpdateAsync(cancellationToken);
    }

    public async Task<Result> ApplyUpdateAsync(UpdateCheckInfo updateInfo, CancellationToken cancellationToken = default)
    {
        return await _updateManager.ApplyUpdateAsync(updateInfo, cancellationToken);
    }
}
