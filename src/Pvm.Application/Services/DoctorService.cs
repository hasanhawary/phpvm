using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Application.Services;

/// <summary>
/// Orchestrates system diagnostic checks and automated remediation for PVM Doctor.
/// </summary>
public sealed class DoctorService
{
    private readonly IReadOnlyList<IDoctorCheck> _checks;

    public DoctorService(IEnumerable<IDoctorCheck> checks)
    {
        _checks = checks?.ToList() ?? throw new ArgumentNullException(nameof(checks));
    }

    public async Task<IReadOnlyList<DoctorCheckResult>> RunAllChecksAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DoctorCheckResult>();
        foreach (var check in _checks)
        {
            var res = await check.RunCheckAsync(cancellationToken);
            results.Add(res);
        }
        return results;
    }

    public async Task<Result<int>> FixAllAsync(CancellationToken cancellationToken = default)
    {
        var fixedCount = 0;
        foreach (var check in _checks)
        {
            var res = await check.RunCheckAsync(cancellationToken);
            if ((res.Status == DoctorStatus.Warning || res.Status == DoctorStatus.Error) && res.CanFix)
            {
                var fixRes = await check.FixAsync(cancellationToken);
                if (fixRes.IsSuccess)
                {
                    fixedCount++;
                }
                else
                {
                    return Result.Fail<int>($"Failed to fix '{res.CheckName}': {fixRes.Error}");
                }
            }
        }
        return Result.Ok(fixedCount);
    }
}
