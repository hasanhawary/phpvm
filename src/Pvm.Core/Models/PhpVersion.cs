using System.Diagnostics.CodeAnalysis;

namespace Pvm.Core.Models;

/// <summary>
/// Represents a fully resolved, immutable PHP semantic version (Major.Minor.Patch).
/// </summary>
public sealed class PhpVersion : IComparable<PhpVersion>, IEquatable<PhpVersion>
{
    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets the branch identifier (e.g., "8.4" for "8.4.23").
    /// </summary>
    public string Branch => $"{Major}.{Minor}";

    /// <summary>
    /// Initializes a new instance of the <see cref="PhpVersion"/> class.
    /// </summary>
    public PhpVersion(int major, int minor, int patch)
    {
        if (major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), "Version components cannot be negative.");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// Parses a string into a <see cref="PhpVersion"/>.
    /// </summary>
    /// <param name="version">The version string (e.g., "8.4.23").</param>
    /// <returns>The parsed <see cref="PhpVersion"/>.</returns>
    /// <exception cref="FormatException">Thrown when the version string is invalid.</exception>
    public static PhpVersion Parse(string version)
    {
        if (!TryParse(version, out var result))
        {
            throw new FormatException($"The string '{version}' is not a valid semantic PHP version (expected Major.Minor.Patch).");
        }

        return result;
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="PhpVersion"/>.
    /// </summary>
    /// <param name="version">The version string (e.g., "8.4.23" or "php-8.4.23").</param>
    /// <param name="result">When this method returns, contains the parsed version if successful; otherwise, null.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? version, [NotNullWhen(true)] out PhpVersion? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var cleaned = version.Trim();
        if (cleaned.StartsWith("php-", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[4..];
        }
        else if (cleaned.StartsWith('v') || cleaned.StartsWith('V'))
        {
            cleaned = cleaned[1..];
        }

        var parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (int.TryParse(parts[0], out var major) &&
            int.TryParse(parts[1], out var minor) &&
            int.TryParse(parts[2], out var patch))
        {
            if (major >= 0 && minor >= 0 && patch >= 0)
            {
                result = new PhpVersion(major, minor, patch);
                return true;
            }
        }

        return false;
    }

    public int CompareTo(PhpVersion? other)
    {
        if (other is null) return 1;
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;
        return Patch.CompareTo(other.Patch);
    }

    public bool Equals(PhpVersion? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
    }

    public override bool Equals(object? obj) => Equals(obj as PhpVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public static bool operator ==(PhpVersion? left, PhpVersion? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(PhpVersion? left, PhpVersion? right) => !(left == right);

    public static bool operator <(PhpVersion? left, PhpVersion? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(PhpVersion? left, PhpVersion? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(PhpVersion? left, PhpVersion? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(PhpVersion? left, PhpVersion? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}
