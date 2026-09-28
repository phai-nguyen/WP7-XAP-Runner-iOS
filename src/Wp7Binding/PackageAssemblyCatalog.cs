namespace Wp7Binding;

public sealed record PackageAssembly(AssemblyIdentity Identity, string EntryPath, byte[] Image);

public sealed class PackageAssemblyCatalog
{
    private readonly PackageAssembly[] _assemblies;

    public PackageAssemblyCatalog(IEnumerable<PackageAssembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        _assemblies = assemblies.ToArray();
        if (_assemblies.Any(assembly => assembly is null))
            throw new ArgumentException("Catalog entries cannot be null.", nameof(assemblies));
    }

    public bool TryGetExact(AssemblyIdentity identity, out PackageAssembly assembly)
    {
        ArgumentNullException.ThrowIfNull(identity);

        foreach (var candidate in _assemblies)
        {
            if (AssemblyIdentityComparer.Instance.Equals(candidate.Identity, identity))
            {
                assembly = candidate;
                return true;
            }
        }

        assembly = null!;
        return false;
    }
}
