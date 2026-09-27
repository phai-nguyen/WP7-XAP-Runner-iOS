using System.Reflection;

namespace Wp7Binding;

public sealed record BindingResolution(
    AssemblyIdentity Requested,
    PackageAssembly? Target,
    string? Source,
    string? RedirectReason);

public sealed class AssemblyBindingResolver
{
    private readonly PackageAssemblyCatalog _catalog;
    private readonly IReadOnlyDictionary<AssemblyIdentity, AssemblyIdentity> _redirects;

    public AssemblyBindingResolver(
        PackageAssemblyCatalog catalog,
        IReadOnlyDictionary<AssemblyIdentity, AssemblyIdentity> redirects)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _redirects = redirects ?? throw new ArgumentNullException(nameof(redirects));
    }

    public BindingResolution Resolve(AssemblyName requested)
    {
        var identity = AssemblyIdentity.FromAssemblyName(requested);
        if (_catalog.TryGetExact(identity, out var packageAssembly))
            return new BindingResolution(identity, packageAssembly, "package", null);

        return new BindingResolution(identity, null, null, null);
    }
}
