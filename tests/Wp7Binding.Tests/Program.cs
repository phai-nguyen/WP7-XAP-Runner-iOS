using System.Reflection;
using Wp7Binding;

var failures = 0;
Run(nameof(IdentityRequiresExactVersionCultureAndToken), IdentityRequiresExactVersionCultureAndToken);
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

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
