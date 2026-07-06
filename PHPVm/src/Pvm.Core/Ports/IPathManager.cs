using Pvm.Core.Models;

namespace Pvm.Core.Ports;

/// <summary>
/// Manages system and user PATH environment variable entries.
/// </summary>
public interface IPathManager
{
    /// <summary>
    /// Gets the current User PATH environment variable entries.
    /// </summary>
    /// <returns>A read-only list of directory paths in the User PATH.</returns>
    IReadOnlyList<string> GetUserPath();

    /// <summary>
    /// Gets the current Machine (System) PATH environment variable entries.
    /// </summary>
    /// <returns>A read-only list of directory paths in the Machine PATH.</returns>
    IReadOnlyList<string> GetMachinePath();

    /// <summary>
    /// Adds an entry to the User PATH environment variable if it is not already present.
    /// </summary>
    /// <param name="entry">The directory path to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result AddToUserPath(string entry);

    /// <summary>
    /// Removes an entry from the User PATH environment variable if it is present.
    /// </summary>
    /// <param name="entry">The directory path to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result RemoveFromUserPath(string entry);

    /// <summary>
    /// Checks whether the User or Machine PATH contains the specified entry.
    /// </summary>
    /// <param name="entry">The directory path to check.</param>
    /// <returns><c>true</c> if present; otherwise, <c>false</c>.</returns>
    bool ContainsEntry(string entry);

    /// <summary>
    /// Finds any duplicate entries in the User PATH.
    /// </summary>
    /// <returns>A read-only list of duplicate directory paths.</returns>
    IReadOnlyList<string> FindDuplicateEntries();

    /// <summary>
    /// Removes duplicate entries from the User PATH while preserving order.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result CleanDuplicateEntries();

    /// <summary>
    /// Finds any existing PHP installations in PATH that might conflict with pvm (e.g., XAMPP, WAMP, Laragon).
    /// </summary>
    /// <returns>A read-only list of conflicting directory paths.</returns>
    IReadOnlyList<string> FindConflictingPhpEntries();
}
