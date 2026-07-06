using System.Net.Http.Json;
using System.Text.Json;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Pvm.Core.Ports;

namespace Pvm.Infrastructure.Network;

/// <summary>
/// Discovers and downloads PHP builds from official windows.php.net mirrors.
/// </summary>
public sealed class OfficialPhpBuildSource : IBuildSource
{
    private const string ReleasesJsonUrl = "https://windows.php.net/downloads/releases/releases.json";
    private const string BaseDownloadUrl = "https://windows.php.net/downloads/releases/";

    private readonly HttpClient _httpClient;

    public OfficialPhpBuildSource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string Name => "Official PHP Windows Mirror (windows.php.net)";

    public async Task<IReadOnlyList<PhpBuild>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(ReleasesJsonUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<PhpBuild>();
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var builds = new List<PhpBuild>();

            foreach (var branchProp in doc.RootElement.EnumerateObject())
            {
                var branchObj = branchProp.Value;
                if (branchObj.ValueKind != JsonValueKind.Object) continue;

                if (!branchObj.TryGetProperty("version", out var versionProp) || versionProp.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var versionStr = versionProp.GetString();
                if (string.IsNullOrWhiteSpace(versionStr) || !PhpVersion.TryParse(versionStr, out var phpVersion) || phpVersion is null)
                {
                    continue;
                }

                foreach (var flavorProp in branchObj.EnumerateObject())
                {
                    var flavorKey = flavorProp.Name.ToLowerInvariant();
                    if (flavorKey == "version" || flavorKey == "source" || flavorKey == "test_pack") continue;
                    if (!flavorKey.StartsWith("ts-") && !flavorKey.StartsWith("nts-")) continue;

                    var flavorObj = flavorProp.Value;
                    if (flavorObj.ValueKind != JsonValueKind.Object) continue;

                    if (!flavorObj.TryGetProperty("zip", out var zipProp) || zipProp.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (!zipProp.TryGetProperty("path", out var pathProp) || pathProp.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var path = pathProp.GetString();
                    if (string.IsNullOrWhiteSpace(path)) continue;

                    var sha256 = zipProp.TryGetProperty("sha256", out var shaProp) && shaProp.ValueKind == JsonValueKind.String
                        ? shaProp.GetString() ?? string.Empty
                        : string.Empty;

                    var sizeStr = zipProp.TryGetProperty("size", out var sizeProp) && sizeProp.ValueKind == JsonValueKind.String
                        ? sizeProp.GetString() ?? string.Empty
                        : string.Empty;

                    long? fileSizeBytes = null;
                    if (!string.IsNullOrWhiteSpace(sizeStr))
                    {
                        var s = sizeStr.Trim().ToUpperInvariant();
                        if (s.EndsWith("MB") && double.TryParse(s[..^2].Trim(), out var mb))
                        {
                            fileSizeBytes = (long)(mb * 1024 * 1024);
                        }
                        else if (s.EndsWith("KB") && double.TryParse(s[..^2].Trim(), out var kb))
                        {
                            fileSizeBytes = (long)(kb * 1024);
                        }
                        else if (long.TryParse(s, out var b))
                        {
                            fileSizeBytes = b;
                        }
                    }

                    var ts = flavorKey.StartsWith("nts-") ? ThreadSafety.Nts : ThreadSafety.Ts;
                    
                    Architecture arch = Architecture.X64;
                    if (flavorKey.EndsWith("-x86") || flavorKey.EndsWith("-x32") || flavorKey.EndsWith("-win32"))
                    {
                        arch = Architecture.X86;
                    }
                    else if (flavorKey.EndsWith("-arm64") || flavorKey.EndsWith("-aarch64"))
                    {
                        arch = Architecture.Arm64;
                    }
                    else if (!flavorKey.EndsWith("-x64"))
                    {
                        continue;
                    }

                    var parts = flavorKey.Split('-');
                    var vsVersion = parts.FirstOrDefault(p => p.StartsWith("vs") || p.StartsWith("vc")) ?? "vs17";

                    var downloadUrl = new Uri(new Uri(BaseDownloadUrl), path);
                    builds.Add(new PhpBuild(phpVersion, arch, ts, downloadUrl, sha256, fileSizeBytes, vsVersion));
                }
            }

            return builds;
        }
        catch
        {
            return Array.Empty<PhpBuild>();
        }
    }

    public async Task<PhpBuild?> GetLatestVersionAsync(string? branch = null, CancellationToken cancellationToken = default)
    {
        var all = await GetAvailableVersionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(branch))
        {
            return all.OrderByDescending(x => x.Version).FirstOrDefault();
        }

        return all
            .Where(x => x.Version.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();
    }

    public async Task<Result<string>> DownloadBuildAsync(
        PhpBuild build,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (build is null) return Result.Fail<string>("Build cannot be null.");
        if (string.IsNullOrWhiteSpace(destinationPath)) return Result.Fail<string>("Destination path cannot be empty.");

        try
        {
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            using var response = await _httpClient.GetAsync(build.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail<string>($"Failed to download build from {build.DownloadUrl}. HTTP status: {response.StatusCode}");
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progress is not null;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    progress!.Report((double)totalRead / totalBytes * 100.0);
                }
            }

            return Result.Ok(destinationPath);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(destinationPath))
            {
                try { File.Delete(destinationPath); } catch { }
            }
            return Result.Fail<string>("Download was cancelled.");
        }
        catch (Exception ex)
        {
            if (File.Exists(destinationPath))
            {
                try { File.Delete(destinationPath); } catch { }
            }
            return Result.Fail<string>($"Download failed: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, ReleasesJsonUrl);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
