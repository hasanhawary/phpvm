using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Diagnostics;

/// <summary>
/// Verifies that Microsoft Visual C++ Redistributable runtime DLLs are present on Windows.
/// </summary>
public sealed class VCRuntimeCheck : IDoctorCheck
{
    public string Name => "Microsoft Visual C++ Redistributable (vcruntime140.dll)";
    public string Description => "Verifies that required Windows C++ runtime DLLs are installed for PHP 8.x.";

    public Task<DoctorCheckResult> RunCheckAsync(CancellationToken cancellationToken = default)
    {
        var sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var vc140 = Path.Combine(sys32, "vcruntime140.dll");
        var vc140_1 = Path.Combine(sys32, "vcruntime140_1.dll");

        if (!File.Exists(vc140) && !File.Exists(vc140_1))
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorStatus.Error,
                "vcruntime140.dll not found in System32. PHP 8.x binaries will fail to run.",
                "Download and install the Microsoft Visual C++ Redistributable (x64) from https://aka.ms/vs/17/release/vc_redist.x64.exe",
                CanFix: false
            ));
        }

        return Task.FromResult(new DoctorCheckResult(
            Name,
            DoctorStatus.Pass,
            "Visual C++ Redistributable runtime DLLs are present."
        ));
    }

    public Task<Result> FixAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}
