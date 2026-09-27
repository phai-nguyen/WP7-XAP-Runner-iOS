namespace Wp7Binding;

public static class BindingLogFormatter
{
    public static string FormatRequest(AssemblyIdentity identity) =>
        $"[BIND1][REQUEST] {FormatIdentity(identity)}";

    public static string FormatRedirect(AssemblyIdentity from, AssemblyIdentity to, string reason) =>
        $"[BIND1][REDIRECT] from=({FormatIdentity(from)}) to=({FormatIdentity(to)}) reason={Escape(reason)}";

    public static string FormatResolve(AssemblyIdentity requested, AssemblyIdentity resolved, string source) =>
        $"[BIND1][RESOLVE_OK] requested=({FormatIdentity(requested)}) resolved=({FormatIdentity(resolved)}) source={Escape(source)}";

    public static string FormatMissingType(string type, string assembly) =>
        $"[BIND1][MISSING_TYPE] type={Escape(type)} assembly={Escape(assembly)}";

    public static string FormatMissingMember(string member, string type, string assembly) =>
        $"[BIND1][MISSING_MEMBER] member={Escape(member)} type={Escape(type)} assembly={Escape(assembly)}";

    public static string FormatBindFail(AssemblyIdentity requested, string exception) =>
        $"[BIND1][ASSEMBLY_BIND_FAIL] requested=({FormatIdentity(requested)}) exception={Escape(exception)}";

    public static string FormatEnd(bool passed) => passed ? "[BIND1][END] PASS" : "[BIND1][END] FAIL";

    private static string FormatIdentity(AssemblyIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return $"name={Escape(identity.Name)} version={Escape(identity.Version.ToString())} culture={Escape(identity.Culture)} pkt={Escape(identity.PublicKeyToken)}";
    }

    private static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
