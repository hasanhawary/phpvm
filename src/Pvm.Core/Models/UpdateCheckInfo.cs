namespace Pvm.Core.Models;

public sealed record UpdateCheckInfo(
    string CurrentVersion,
    string LatestVersion,
    bool IsUpdateAvailable,
    string? DownloadUrl,
    string? ReleaseNotes
);
