using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages parsing, modifying, backing up, and validating php.ini configuration files.
/// </summary>
public interface IPhpIniManager
{
    /// <summary>
    /// Ensures that a php.ini file exists in the installation directory, creating it from a template if necessary.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="template">The template name ("development" or "production").</param>
    /// <returns>A result indicating success or failure.</returns>
    Result EnsureExists(string installPath, string template = "development");

    /// <summary>
    /// Gets the value of a configuration directive from php.ini.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="key">The directive name (e.g., "memory_limit").</param>
    /// <returns>The directive value, or null if not set.</returns>
    string? GetValue(string installPath, string key);

    /// <summary>
    /// Sets or updates the value of a configuration directive in php.ini.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="key">The directive name.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result SetValue(string installPath, string key, string value);

    /// <summary>
    /// Enables a PHP extension in php.ini (e.g., "extension=curl").
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="extensionName">The name of the extension (without .dll prefix or suffix).</param>
    /// <returns>A result indicating success or failure.</returns>
    Result EnableExtension(string installPath, string extensionName);

    /// <summary>
    /// Disables a PHP extension in php.ini by commenting it out.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="extensionName">The name of the extension.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result DisableExtension(string installPath, string extensionName);

    /// <summary>
    /// Lists all extension entries configured in php.ini and their enabled/disabled status.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <returns>A read-only list of extension names and whether they are enabled.</returns>
    IReadOnlyList<(string Name, bool IsEnabled)> ListExtensions(string installPath);

    /// <summary>
    /// Creates a backup of the existing php.ini file.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <returns>A result containing the path to the backup file.</returns>
    Result<string> Backup(string installPath);

    /// <summary>
    /// Restores php.ini from a previously created backup file.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="backupPath">The path to the backup file.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result Restore(string installPath, string backupPath);

    /// <summary>
    /// Resets php.ini by overwriting it with a clean template.
    /// </summary>
    /// <param name="installPath">The PHP installation directory path.</param>
    /// <param name="template">The template name ("development" or "production").</param>
    /// <returns>A result indicating success or failure.</returns>
    Result Reset(string installPath, string template = "development");
}
