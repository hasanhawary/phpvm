namespace Pvm.Core.Models;

/// <summary>
/// Represents an unresolved version specifier entered by the user (e.g., "8.4", "8.4.23", "latest", "default").
/// </summary>
public sealed class VersionSpecifier : IEquatable<VersionSpecifier>
{
    /// <summary>
    /// Gets the raw string specifier as provided by the user.
    /// </summary>
    public string Raw { get; }

    /// <summary>
    /// Gets a value indicating whether the specifier is an exact semantic version (e.g., "8.4.23").
    /// </summary>
    public bool IsExact { get; }

    private VersionSpecifier(string raw, bool isExact)
    {
        Raw = raw;
        IsExact = isExact;
    }

    /// <summary>
    /// Parses a user-supplied version string into a <see cref="VersionSpecifier"/>.
    /// </summary>
    /// <param name="input">The input string (e.g., "8.4", "latest").</param>
    /// <returns>A new <see cref="VersionSpecifier"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or whitespace.</exception>
    public static VersionSpecifier Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Version specifier cannot be empty.", nameof(input));
        }

        var cleaned = input.Trim();
        var isExact = PhpVersion.TryParse(cleaned, out _);
        return new VersionSpecifier(cleaned, isExact);
    }

    /// <summary>
    /// Checks whether the specified semantic version matches this specifier.
    /// </summary>
    public bool Matches(PhpVersion version)
    {
        if (version is null) return false;
        if (Raw.Equals("latest", StringComparison.OrdinalIgnoreCase) ||
            Raw.Equals("default", StringComparison.OrdinalIgnoreCase) ||
            Raw.Equals("*", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (PhpVersion.TryParse(Raw, out var exact) && exact != null && exact == version)
        {
            return true;
        }

        if (version.Branch.Equals(Raw, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return version.ToString().StartsWith(Raw, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(VersionSpecifier? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Raw, other.Raw, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as VersionSpecifier);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Raw);

    public override string ToString() => Raw;

    public static bool operator ==(VersionSpecifier? left, VersionSpecifier? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(VersionSpecifier? left, VersionSpecifier? right) => !(left == right);
}
