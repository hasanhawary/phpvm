using Pvm.Core.Enums;

namespace Pvm.Core.Models;

public sealed record DoctorCheckResult(
    string CheckName,
    DoctorStatus Status,
    string Message,
    string? FixRecommendation = null,
    bool CanFix = false
);
