using Pvm.Core.Enums;

namespace Pvm.Core.Models;

/// <summary>
/// Represents defaults and user configuration settings for pvm.
/// </summary>
public sealed class PhpVmConfig
{
    /// <summary>
    /// Gets or sets the schema version of the configuration file.
    /// </summary>
    public int ConfigVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the default architecture to install when not specified.
    /// </summary>
    public Architecture DefaultArchitecture { get; set; } = Architecture.X64;

    /// <summary>
    /// Gets or sets the default thread safety mode to install when not specified.
    /// </summary>
    public ThreadSafety DefaultThreadSafety { get; set; } = ThreadSafety.Nts;

    /// <summary>
    /// Gets or sets the default php.ini template ("development" or "production").
    /// </summary>
    public string DefaultIniTemplate { get; set; } = "development";

    /// <summary>
    /// Gets or sets the custom download mirror URL, if configured.
    /// </summary>
    public string? DownloadMirror { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether SHA256 checksums should be verified after download.
    /// </summary>
    public bool VerifyChecksums { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether downloaded archives should be cached locally.
    /// </summary>
    public bool CacheDownloads { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed downloads.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the download timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets an optional HTTP/HTTPS proxy server URL.
    /// </summary>
    public string? Proxy { get; set; }

    /// <summary>
    /// Gets or sets user-defined version aliases (e.g., "default" -> "8.4", "work" -> "8.3").
    /// </summary>
    public Dictionary<string, string> Aliases { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = "8.4"
    };
}
