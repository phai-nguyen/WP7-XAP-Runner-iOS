using System.Reflection;
using Wp7Binding;

var failures = 0;
Run(nameof(IdentityRequiresExactVersionCultureAndToken), IdentityRequiresExactVersionCultureAndToken);
Run(nameof(CatalogNameAndCultureAreCaseInsensitiveButTokenIsExact), CatalogNameAndCultureAreCaseInsensitiveButTokenIsExact);
Run(nameof(PackageExactMatchPrecedesRedirect), PackageExactMatchPrecedesRedirect);
Run(nameof(ConfiguredRedirectResolvesPresentTarget), ConfiguredRedirectResolvesPresentTarget);
Run(nameof(RedirectRequiresPresentTarget), RedirectRequiresPresentTarget);
Run(nameof(NoNameOnlyFallback), NoNameOnlyFallback);
return failures == 0 ? 0 : 1;

void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

static void IdentityRequiresExactVersionCultureAndToken()
{
    var assemblyName = new AssemblyName("Legacy.Contract, Version=1.2.3.4, Culture=neutral");
    assemblyName.SetPublicKeyToken([1, 2, 3, 4, 5, 6, 7, 8]);

    var actual = AssemblyIdentity.FromAssemblyName(assemblyName);
    var expected = new AssemblyIdentity("Legacy.Contract", new Version(1, 2, 3, 4), "neutral", "0102030405060708");

    Require(actual == expected, $"expected normalized identity {expected}, got {actual}");
    Require(actual != expected with { Version = new Version(1, 2, 3, 5) }, "version mismatch must not compare equal");
    Require(actual != expected with { Culture = "en-US" }, "culture mismatch must not compare equal");
    Require(actual != expected with { PublicKeyToken = "1111111111111111" }, "public-key token mismatch must not compare equal");
}

static void PackageExactMatchPrecedesRedirect()
{
    var request = Identity("Legacy.Contract", new Version(1, 2, 3, 4), "neutral", "0102030405060708");
    var redirectTarget = Identity("Compat.Contract", new Version(1, 0, 0, 0), "neutral", "null");
    var catalog = new PackageAssemblyCatalog(
    [
        new PackageAssembly(request, "Legacy.Contract.dll", [1]),
        new PackageAssembly(redirectTarget, "Compat.Contract.dll", [2])
    ]);
    var resolver = new AssemblyBindingResolver(
        catalog,
        new Dictionary<AssemblyIdentity, AssemblyIdentity> { [request] = redirectTarget });

    var result = resolver.Resolve(ToAssemblyName(request));

    Require(result.Target?.Identity == request, "exact package match must win over a configured redirect");
    Require(result.Source == "package", $"expected package source, got {result.Source ?? "<null>"}");
}

static void CatalogNameAndCultureAreCaseInsensitiveButTokenIsExact()
{
    var storedIdentity = Identity("Legacy.Contract", new Version(1, 2, 3, 4), "EN-us", "0102030405060708");
    var request = Identity("legacy.contract", new Version(1, 2, 3, 4), "en-US", "0102030405060708");
    var otherToken = request with { PublicKeyToken = "1111111111111111" };
    var catalog = new PackageAssemblyCatalog([new PackageAssembly(storedIdentity, "Legacy.Contract.dll", [1])]);

    Require(catalog.TryGetExact(request, out _), "name and culture casing must not prevent an exact identity match");
    Require(!catalog.TryGetExact(otherToken, out _), "public-key token must match exactly");
}

static void RedirectRequiresPresentTarget()
{
    var request = Identity("Legacy.Contract", new Version(1, 2, 3, 4), "neutral", "0102030405060708");
    var absentTarget = Identity("Compat.Contract", new Version(1, 0, 0, 0), "neutral", "null");
    var resolver = new AssemblyBindingResolver(
        new PackageAssemblyCatalog([]),
        new Dictionary<AssemblyIdentity, AssemblyIdentity> { [request] = absentTarget });

    var result = resolver.Resolve(ToAssemblyName(request));

    Require(result.Target is null && result.Source is null, "redirect to absent target must remain unresolved");
}

static void ConfiguredRedirectResolvesPresentTarget()
{
    var request = Identity("Legacy.Contract", new Version(1, 2, 3, 4), "neutral", "0102030405060708");
    var target = Identity("Compat.Contract", new Version(1, 0, 0, 0), "neutral", "null");
    var catalog = new PackageAssemblyCatalog([new PackageAssembly(target, "Compat.Contract.dll", [2])]);
    var resolver = new AssemblyBindingResolver(
        catalog,
        new Dictionary<AssemblyIdentity, AssemblyIdentity> { [request] = target });

    var result = resolver.Resolve(ToAssemblyName(request));

    Require(result.Target?.Identity == target, "configured redirect must resolve to its exact present target");
    Require(result.Source == "compat", $"expected compat source, got {result.Source ?? "<null>"}");
    Require(result.RedirectReason is not null, "configured redirect must report why it redirected");
}

static void NoNameOnlyFallback()
{
    var request = Identity("Legacy.Contract", new Version(1, 2, 3, 4), "neutral", "0102030405060708");
    var otherVersion = Identity("Legacy.Contract", new Version(1, 2, 3, 5), "neutral", "0102030405060708");
    var catalog = new PackageAssemblyCatalog([new PackageAssembly(otherVersion, "Legacy.Contract.dll", [1])]);
    var resolver = new AssemblyBindingResolver(catalog, new Dictionary<AssemblyIdentity, AssemblyIdentity>());

    var result = resolver.Resolve(ToAssemblyName(request));

    Require(result.Target is null, "same name with different version must not bind");
}

static AssemblyIdentity Identity(string name, Version version, string culture, string publicKeyToken) =>
    new(name, version, culture, publicKeyToken);

static AssemblyName ToAssemblyName(AssemblyIdentity identity) =>
    new($"{identity.Name}, Version={identity.Version}, Culture={identity.Culture}, PublicKeyToken={identity.PublicKeyToken}");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
