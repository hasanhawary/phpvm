using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Represents a PHP extension listed in a php.ini configuration file.
/// </summary>
/// <param name="Name">The normalized extension name (e.g., "curl", "mbstring", "xdebug").</param>
/// <param name="IsEnabled">A value indicating whether the extension is uncommented and active.</param>
/// <param name="IsZendExtension">A value indicating whether this is a zend_extension.</param>
/// <param name="RawLine">The original line of text in php.ini.</param>
public sealed record PhpExtension(string Name, bool IsEnabled, bool IsZendExtension, string RawLine);

/// <summary>
/// Manages reading, modifying, and configuring php.ini files.
/// </summary>
public interface IIniManager
{
    /// <summary>
    /// Gets all PHP extensions defined or commented out in the specified php.ini file.
    /// </summary>
    Task<Result<IReadOnlyList<PhpExtension>>> GetExtensionsAsync(string iniPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a specific PHP extension in the specified php.ini file.
    /// </summary>
    Task<Result> SetExtensionStatusAsync(string iniPath, string extensionName, bool enable, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the value of a configuration directive in the specified php.ini file.
    /// </summary>
    Task<Result<string?>> GetDirectiveValueAsync(string iniPath, string directiveName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or adds a configuration directive value in the specified php.ini file.
    /// </summary>
    Task<Result> SetDirectiveValueAsync(string iniPath, string directiveName, string value, CancellationToken cancellationToken = default);
}
