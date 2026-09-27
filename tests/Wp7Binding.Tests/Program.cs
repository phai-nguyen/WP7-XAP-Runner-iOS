using System.Reflection;
using Wp7Binding;

var failures = 0;
Run(nameof(IdentityRequiresExactVersionCultureAndToken), IdentityRequiresExactVersionCultureAndToken);
Run(nameof(CatalogNameAndCultureAreCaseInsensitiveButTokenIsExact), CatalogNameAndCultureAreCaseInsensitiveButTokenIsExact);
Run(nameof(PackageExactMatchPrecedesRedirect), PackageExactMatchPrecedesRedirect);
Run(nameof(ConfiguredRedirectResolvesPresentTarget), ConfiguredRedirectResolvesPresentTarget);
Run(nameof(RedirectRequiresPresentTarget), RedirectRequiresPresentTarget);
Run(nameof(NoNameOnlyFallback), NoNameOnlyFallback);
Run(nameof(FormatterEscapesNewlinesAndKeepsOneMarkerPerLine), FormatterEscapesNewlinesAndKeepsOneMarkerPerLine);
Run(nameof(FormatterEmitsRequestRedirectResolveAndMissingMarkers), FormatterEmitsRequestRedirectResolveAndMissingMarkers);
Run(nameof(UnresolvedIdentityProducesBindFailAndEndMarkers), UnresolvedIdentityProducesBindFailAndEndMarkers);
Run(nameof(UnrelatedProbeFailuresAreNotCompatibilityBoundaries), UnrelatedProbeFailuresAreNotCompatibilityBoundaries);
Run(nameof(MissingAssemblyReportsTheActualRequestedIdentity), MissingAssemblyReportsTheActualRequestedIdentity);
Run(nameof(MissingTypeAndMemberDoNotInventEntryAssemblyAttribution), MissingTypeAndMemberDoNotInventEntryAssemblyAttribution);
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

static void FormatterEscapesNewlinesAndKeepsOneMarkerPerLine()
{
    var identity = Identity("Legacy\r\n.Contract", new Version(1, 2, 3, 4), "neutral", "null");
    var lines = new[]
    {
        BindingLogFormatter.FormatRequest(identity),
        BindingLogFormatter.FormatRedirect(identity, Identity("Compat.Contract", new Version(1, 0, 0, 0), "neutral", "null"), "redirect\r\nreason"),
        BindingLogFormatter.FormatResolve(identity, identity, "package\r\nsource"),
        BindingLogFormatter.FormatMissingType("Ns.Type\r\nName", "Legacy.Contract"),
        BindingLogFormatter.FormatMissingMember("Run\r\nNow", "Ns.Type", "Legacy.Contract")
    };

    foreach (var line in lines)
    {
        Require(!line.Contains('\r') && !line.Contains('\n'), $"marker must occupy one physical line, got {line}");
        Require(line.Contains("\\r\\n", StringComparison.Ordinal), $"embedded CR/LF must be escaped, got {line}");
    }
}

static void FormatterEmitsRequestRedirectResolveAndMissingMarkers()
{
    var from = Identity("Legacy.Contract", new Version(1, 2, 3, 4), "neutral", "0102030405060708");
    var to = Identity("Compat.Contract", new Version(1, 0, 0, 0), "neutral", "null");
    var lines = new[]
    {
        (BindingLogFormatter.FormatRequest(from), "[BIND1][REQUEST]"),
        (BindingLogFormatter.FormatRedirect(from, to, "explicit"), "[BIND1][REDIRECT]"),
        (BindingLogFormatter.FormatResolve(from, to, "compat"), "[BIND1][RESOLVE_OK]"),
        (BindingLogFormatter.FormatMissingType("Ns.Game", "Legacy.Contract"), "[BIND1][MISSING_TYPE]"),
        (BindingLogFormatter.FormatMissingMember("Run()", "Ns.Game", "Legacy.Contract"), "[BIND1][MISSING_MEMBER]")
    };

    foreach (var (line, marker) in lines)
    {
        Require(line.StartsWith(marker + " ", StringComparison.Ordinal), $"expected marker {marker}, got {line}");
        Require(line.Contains("version=", StringComparison.Ordinal) || marker.StartsWith("[BIND1][MISSING_", StringComparison.Ordinal), $"identity fields missing from {line}");
    }
}

static void UnresolvedIdentityProducesBindFailAndEndMarkers()
{
    var request = Identity("Unavailable.Contract", new Version(1, 0, 0, 0), "neutral", "null");
    var resolver = new AssemblyBindingResolver(
        new PackageAssemblyCatalog([]),
        new Dictionary<AssemblyIdentity, AssemblyIdentity>());
    var resolution = resolver.Resolve(ToAssemblyName(request));
    var bindFail = BindingLogFormatter.FormatBindFail(resolution.Requested, "FileNotFoundException: not found\r\ndetails");
    var end = BindingLogFormatter.FormatEnd(passed: false);

    Require(resolution.Target is null && resolution.Source is null, "missing identity must remain unresolved");
    Require(bindFail.StartsWith("[BIND1][ASSEMBLY_BIND_FAIL] ", StringComparison.Ordinal), "unresolved identity must emit the assembly bind-fail marker");
    Require(end == "[BIND1][END] FAIL", $"expected a controlled fail end marker, got {end}");
    Require(!bindFail.Contains('\r') && !bindFail.Contains('\n'), "exception data must stay on one marker line");
    Require(bindFail.Contains("\\r\\ndetails", StringComparison.Ordinal), "exception newlines must be escaped");
}

static void UnrelatedProbeFailuresAreNotCompatibilityBoundaries()
{
    Require(Bind1ExceptionClassifier.Classify(new BadImageFormatException("invalid assembly image")) is null,
        "invalid assembly bytes must not count as a diagnosed missing dependency");
    Require(Bind1ExceptionClassifier.Classify(new InvalidOperationException("unexpected probe failure")) is null,
        "unclassified runtime errors must not count as diagnosed compatibility boundaries");
    Require(Bind1ExceptionClassifier.Classify(new FileNotFoundException("missing file without assembly identity")) is null,
        "a file-not-found exception without a requested assembly identity must not be guessed as a bind failure");
}

static void MissingAssemblyReportsTheActualRequestedIdentity()
{
    var exception = new FileNotFoundException(
        "Could not load dependency.",
        "Dependency.Contract, Version=2.3.4.5, Culture=neutral, PublicKeyToken=0102030405060708");
    var result = Bind1ExceptionClassifier.Classify(exception);

    Require(result is not null, "a missing assembly with a complete requested identity is a compatibility boundary");
    var marker = result?.Marker ?? string.Empty;
    Require(marker.Contains("name=Dependency.Contract", StringComparison.Ordinal), "marker must identify the requested dependency");
    Require(marker.Contains("version=2.3.4.5", StringComparison.Ordinal), "marker must preserve the requested version");
    Require(!marker.Contains("name=Aleterated", StringComparison.Ordinal), "marker must not substitute the entry assembly");
}

static void MissingTypeAndMemberDoNotInventEntryAssemblyAttribution()
{
    var missingType = Bind1ExceptionClassifier.Classify(new TypeLoadException(
        "Could not load referenced dependency type.", "Dependency.Namespace.Widget", null));
    var missingMember = Bind1ExceptionClassifier.Classify(new MissingMemberException(
        "Dependency.Namespace.Widget.Render is missing."));

    Require(missingType is not null && missingType.Marker.Contains("type=Dependency.Namespace.Widget", StringComparison.Ordinal),
        "type marker must preserve the actual missing type");
    Require(missingType!.Marker.Contains("assembly=unknown", StringComparison.Ordinal),
        "type marker must not claim the entry assembly when the declaring assembly is unknown");
    Require(missingMember is not null && missingMember.Marker.Contains("member=unknown type=unknown assembly=unknown", StringComparison.Ordinal),
        "member marker must leave unproven declaring metadata unknown");
    Require(missingMember!.Marker.Contains("detail=Dependency.Namespace.Widget.Render is missing.", StringComparison.Ordinal),
        "member marker must preserve exception details rather than fabricate an entry member");
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
