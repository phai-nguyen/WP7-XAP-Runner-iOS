using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Foundation;
using Wp7Binding;

namespace WP7ILRun1;

internal sealed record Bind1ProbeResult(bool DiagnosticSuccess, string Log, string? SavedLogPath);

internal static class Bind1ProbeExecutor
{
    public static Bind1ProbeResult Execute(Action<string>? onLine = null)
    {
        var log = new StringBuilder();
        var diagnosticSuccess = false;
        var passed = false;
        var boundaryFound = false;
        var entryIdentity = new AssemblyIdentity("Aleterated", new Version(1, 0, 0, 0), "neutral", "null");
        var entryTypeName = "Aleterated.Game1";
        ResolveEventHandler? resolveHandler = null;

        void Emit(string line)
        {
            log.AppendLine(line);
            AppLog.Write(line);
            onLine?.Invoke(line);
        }

        void RecordBoundary(string line)
        {
            if (boundaryFound)
                return;
            boundaryFound = true;
            diagnosticSuccess = true;
            Emit(line);
        }

        void RecordException(Exception exception)
        {
            var classification = Bind1ExceptionClassifier.Classify(exception);
            if (classification is not null)
            {
                RecordBoundary(classification.Marker);
                return;
            }

            Emit($"[BIND1][PROBE_FAIL] {exception.GetType().Name}: {exception.Message}");
        }

        Emit("[BIND1][START]");
        Emit($"[BIND1][RUNTIME] {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Emit($"[BIND1][OS] {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

        try
        {
            var bundle = NSBundle.MainBundle.BundlePath;
            var xapPath = Path.Combine(bundle, "Aleterated.xap");
            var reportPath = Path.Combine(bundle, "Aleterated.xapscan1.json");
            Emit($"[BIND1][XAP_PATH] {xapPath}");

            if (!File.Exists(xapPath))
            {
                Emit("[BIND1][XAP_NOT_FOUND]");
            }
            else if (!File.Exists(reportPath))
            {
                Emit("[BIND1][REPORT_NOT_FOUND]");
            }
            else
            {
                var xapBytes = File.ReadAllBytes(xapPath);
                var reportBytes = File.ReadAllBytes(reportPath);
                using var report = JsonDocument.Parse(reportBytes);
                var root = report.RootElement;
                var package = root.GetProperty("package");
                var expectedHash = package.GetProperty("sha256").GetString();
                var actualHash = Convert.ToHexString(SHA256.HashData(xapBytes)).ToLowerInvariant();
                Emit($"[BIND1][XAP_SHA256] {actualHash}");

                if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                {
                    Emit("[BIND1][REPORT_XAP_HASH_MISMATCH]");
                }
                else
                {
                    var manifest = root.GetProperty("appManifest");
                    var assemblyName = RequiredString(manifest, "entryPointAssembly");
                    entryTypeName = RequiredString(manifest, "entryPointType");
                    if (!string.Equals(assemblyName, "Aleterated", StringComparison.Ordinal))
                        throw new InvalidDataException($"Unexpected entry assembly: {assemblyName}");

                    var entryPoint = root.GetProperty("entryPoint");
                    if (entryPoint.GetProperty("assemblyResolved").ValueKind != JsonValueKind.True ||
                        entryPoint.GetProperty("typeResolved").ValueKind != JsonValueKind.True)
                        throw new InvalidDataException("XAPSCAN1 did not resolve the fixture entry point.");

                    var assemblyRows = root.GetProperty("assemblies");
                    var packageAssemblies = new List<PackageAssembly>();
                    PackageAssembly? entryAssembly = null;

                    using (var memory = new MemoryStream(xapBytes, writable: false))
                    using (var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: false))
                    {
                        foreach (var row in assemblyRows.EnumerateArray())
                        {
                            if (!row.TryGetProperty("isManaged", out var managed) || managed.ValueKind != JsonValueKind.True)
                                continue;

                            var path = RequiredString(row, "path");
                            var simpleName = RequiredString(row, "name");
                            var version = Version.Parse(RequiredString(row, "version"));
                            var culture = NullableString(row, "culture") ?? "neutral";
                            var token = NullableString(row, "publicKeyToken") ?? "null";
                            var entry = archive.Entries.FirstOrDefault(candidate =>
                                string.Equals(candidate.FullName, path, StringComparison.OrdinalIgnoreCase));
                            if (entry is null)
                                throw new InvalidDataException($"XAP assembly entry is missing: {path}");

                            using var input = entry.Open();
                            using var image = new MemoryStream();
                            input.CopyTo(image);
                            var packageAssembly = new PackageAssembly(
                                new AssemblyIdentity(simpleName, version, culture, token), path, image.ToArray());
                            packageAssemblies.Add(packageAssembly);

                            if (string.Equals(simpleName, assemblyName, StringComparison.OrdinalIgnoreCase))
                                entryAssembly = packageAssembly;
                        }
                    }

                    if (entryAssembly is null)
                        throw new InvalidDataException("The entry assembly is not present as a managed XAP image.");

                    entryIdentity = entryAssembly.Identity;
                    Emit(BindingLogFormatter.FormatRequest(entryIdentity));
                    var catalog = new PackageAssemblyCatalog(packageAssemblies);
                    var resolver = new AssemblyBindingResolver(
                        catalog,
                        new Dictionary<AssemblyIdentity, AssemblyIdentity>());
                    var loadedByPath = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

                    resolveHandler = (_, args) =>
                    {
                        if (boundaryFound)
                            return null;

                        AssemblyIdentity requested;
                        try
                        {
                            Emit(BindingLogFormatter.FormatRequest(
                                AssemblyIdentity.FromAssemblyName(new AssemblyName(args.Name))));
                            requested = AssemblyIdentity.FromAssemblyName(new AssemblyName(args.Name));
                        }
                        catch (Exception ex)
                        {
                            Emit($"[BIND1][PROBE_FAIL] Invalid assembly request: {ex.GetType().Name}: {ex.Message}");
                            return null;
                        }

                        var resolution = resolver.Resolve(new AssemblyName(args.Name));
                        var target = resolution.Target;
                        if (target is null)
                        {
                            RecordBoundary(BindingLogFormatter.FormatBindFail(requested,
                                "FileNotFoundException: no exact package assembly or explicit compatibility redirect"));
                            return null;
                        }

                        if (resolution.Source == "compat")
                            Emit(BindingLogFormatter.FormatRedirect(requested, target.Identity,
                                resolution.RedirectReason ?? "configured redirect"));

                        try
                        {
                            if (!loadedByPath.TryGetValue(target.EntryPath, out var loaded))
                            {
                                loaded = Assembly.Load(target.Image);
                                loadedByPath[target.EntryPath] = loaded;
                            }
                            Emit(BindingLogFormatter.FormatResolve(
                                requested,
                                AssemblyIdentity.FromAssemblyName(loaded.GetName()),
                                resolution.Source ?? "package"));
                            return loaded;
                        }
                        catch (Exception ex)
                        {
                            RecordBoundary(BindingLogFormatter.FormatBindFail(requested,
                                $"{ex.GetType().Name}: {ex.Message}"));
                            return null;
                        }
                    };

                    AppDomain.CurrentDomain.AssemblyResolve += resolveHandler;
                    try
                    {
                        var loadedEntry = Assembly.Load(entryAssembly.Image);
                        loadedByPath[entryAssembly.EntryPath] = loadedEntry;
                        var loadedIdentity = AssemblyIdentity.FromAssemblyName(loadedEntry.GetName());
                        if (loadedIdentity != entryIdentity)
                            throw new BadImageFormatException("Loaded entry identity does not match the validated XAPSCAN1 identity.");

                        Emit("[BIND1][ASSEMBLY_LOAD_OK]");
                        var entryType = loadedEntry.GetType(entryTypeName, throwOnError: false, ignoreCase: false);
                        if (entryType is null)
                        {
                            if (!boundaryFound)
                                RecordBoundary(BindingLogFormatter.FormatMissingType(entryTypeName, entryIdentity.Name));
                        }
                        else if (!boundaryFound)
                        {
                            Emit($"[BIND1][TYPE_RESOLVE_OK] {entryTypeName}");
                            _ = entryType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.Instance | BindingFlags.Static |
                                                     BindingFlags.DeclaredOnly);
                            if (!boundaryFound)
                            {
                                Emit("[BIND1][MEMBER_METADATA_OK]");
                                passed = true;
                                diagnosticSuccess = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordException(ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Emit($"[BIND1][INPUT_FAIL] {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (resolveHandler is not null)
                AppDomain.CurrentDomain.AssemblyResolve -= resolveHandler;
        }

        Emit(BindingLogFormatter.FormatEnd(passed && !boundaryFound));
        var savedLogPath = AppLog.TakeThisPath;
        if (savedLogPath is not null)
            Emit($"[BIND1][LOG_SAVED] {savedLogPath}");
        return new Bind1ProbeResult(diagnosticSuccess, log.ToString(), savedLogPath);
    }

    private static string RequiredString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"Required string property is missing: {name}");
        return value.GetString()!;
    }

    private static string? NullableString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            throw new InvalidDataException($"Required identity property is missing: {name}");
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new InvalidDataException($"Identity property must be a string or null: {name}")
        };
    }
}
