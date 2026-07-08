using System.Net.Http.Headers;
using System.Text.Json;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Platform;

public sealed class GitHubSelfUpdateManager : ISelfUpdateManager
{
    private readonly HttpClient _httpClient;
    private readonly IArchiveExtractor _archiveExtractor;

    public GitHubSelfUpdateManager(HttpClient httpClient, IArchiveExtractor archiveExtractor)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _archiveExtractor = archiveExtractor ?? throw new ArgumentNullException(nameof(archiveExtractor));
        
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("pvm", PvmConstants.Version));
        }
    }

    public async Task<Result<UpdateCheckInfo>> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(PvmConstants.GitHubReleasesApiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail<UpdateCheckInfo>($"GitHub API returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V') ?? "";
            var body = root.TryGetProperty("body", out var b) ? b.GetString() : "";
            
            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (string.Equals(name, "pvm-win-x64.zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            var isNewer = !string.Equals(tag, PvmConstants.Version, StringComparison.OrdinalIgnoreCase) &&
                          Version.TryParse(tag, out var latestVer) &&
                          Version.TryParse(PvmConstants.Version, out var currVer) &&
                          latestVer > currVer;

            return Result.Ok(new UpdateCheckInfo(
                CurrentVersion: PvmConstants.Version,
                LatestVersion: tag,
                IsUpdateAvailable: isNewer,
                DownloadUrl: downloadUrl,
                ReleaseNotes: body
            ));
        }
        catch (Exception ex)
        {
            return Result.Fail<UpdateCheckInfo>($"Failed to check for updates: {ex.Message}");
        }
    }

    public async Task<Result> ApplyUpdateAsync(UpdateCheckInfo updateInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
        {
            return Result.Fail("No valid download URL found for the latest release asset ('pvm-win-x64.zip').");
        }

        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                return Result.Fail("Unable to determine current executable path for update.");
            }

            var tempDir = Path.Combine(Path.GetTempPath(), $"pvm_update_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var zipPath = Path.Combine(tempDir, "pvm_update.zip");
            using (var stream = await _httpClient.GetStreamAsync(updateInfo.DownloadUrl, cancellationToken))
            using (var fileStream = File.Create(zipPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var extractDir = Path.Combine(tempDir, "extracted");
            var extractResult = await _archiveExtractor.ExtractAsync(zipPath, extractDir, null, cancellationToken);
            if (extractResult.IsFailure)
            {
                return Result.Fail($"Failed to extract update package: {extractResult.Error}");
            }

            var newExePath = Path.Combine(extractDir, "pvm.exe");
            if (!File.Exists(newExePath))
            {
                return Result.Fail("Update archive did not contain 'pvm.exe'.");
            }

            var backupPath = exePath + ".old";
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            File.Move(exePath, backupPath);
            File.Copy(newExePath, exePath);

            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Self-update failed: {ex.Message}");
        }
    }
}
