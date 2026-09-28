using System.Reflection;

namespace Wp7Binding;

public sealed record AssemblyIdentity(string Name, Version Version, string Culture, string PublicKeyToken)
{
    public static AssemblyIdentity FromAssemblyName(AssemblyName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var simpleName = name.Name;
        if (string.IsNullOrWhiteSpace(simpleName))
            throw new ArgumentException("Assembly name must include a simple name.", nameof(name));

        var version = name.Version;
        if (version is null)
            throw new ArgumentException("Assembly name must include a version.", nameof(name));

        var culture = string.IsNullOrEmpty(name.CultureName) ? "neutral" : name.CultureName;
        var tokenBytes = name.GetPublicKeyToken();
        var token = tokenBytes is null or { Length: 0 }
            ? "null"
            : Convert.ToHexString(tokenBytes).ToLowerInvariant();

        return new AssemblyIdentity(simpleName, version, culture, token);
    }
}

internal sealed class AssemblyIdentityComparer : IEqualityComparer<AssemblyIdentity>
{
    public static AssemblyIdentityComparer Instance { get; } = new();

    private AssemblyIdentityComparer()
    {
    }

    public bool Equals(AssemblyIdentity? left, AssemblyIdentity? right) =>
        ReferenceEquals(left, right) ||
        (left is not null && right is not null &&
         StringComparer.OrdinalIgnoreCase.Equals(left.Name, right.Name) &&
         Equals(left.Version, right.Version) &&
         StringComparer.OrdinalIgnoreCase.Equals(left.Culture, right.Culture) &&
         StringComparer.Ordinal.Equals(left.PublicKeyToken, right.PublicKeyToken));

    public int GetHashCode(AssemblyIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var hash = new HashCode();
        hash.Add(identity.Name, StringComparer.OrdinalIgnoreCase);
        hash.Add(identity.Version);
        hash.Add(identity.Culture, StringComparer.OrdinalIgnoreCase);
        hash.Add(identity.PublicKeyToken, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
