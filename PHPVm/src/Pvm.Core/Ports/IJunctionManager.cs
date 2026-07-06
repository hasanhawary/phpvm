using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages NTFS directory junctions (symbolic links) for switching active PHP versions.
/// </summary>
public interface IJunctionManager
{
    /// <summary>
    /// Creates or updates an NTFS junction pointing to the target directory.
    /// </summary>
    /// <param name="junctionPath">The path of the junction to create or update.</param>
    /// <param name="targetPath">The target directory path that the junction should point to.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result CreateOrUpdateJunction(string junctionPath, string targetPath);

    /// <summary>
    /// Deletes an NTFS junction without deleting the target directory contents.
    /// </summary>
    /// <param name="junctionPath">The path of the junction to delete.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result DeleteJunction(string junctionPath);

    /// <summary>
    /// Gets the target directory path that a junction points to.
    /// </summary>
    /// <param name="junctionPath">The path of the junction.</param>
    /// <returns>The target directory path, or null if the junction does not exist or is not a junction.</returns>
    string? GetJunctionTarget(string junctionPath);

    /// <summary>
    /// Checks whether the specified path exists and is an NTFS junction or directory symbolic link.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path is a junction; otherwise, <c>false</c>.</returns>
    bool IsJunction(string path);
}
