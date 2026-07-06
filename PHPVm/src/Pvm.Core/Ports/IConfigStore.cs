using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages reading and writing user configuration settings from persistence.
/// </summary>
public interface IConfigStore
{
    /// <summary>
    /// Loads the configuration from disk, applying migrations and defaults if necessary.
    /// </summary>
    /// <returns>The loaded configuration object.</returns>
    PhpVmConfig Load();

    /// <summary>
    /// Saves the specified configuration object to disk.
    /// </summary>
    /// <param name="config">The configuration object to save.</param>
    void Save(PhpVmConfig config);

    /// <summary>
    /// Resets the configuration on disk to factory defaults.
    /// </summary>
    void Reset();
}
