using System.Reflection;

namespace Wp7Binding;

public sealed record Bind1ExceptionClassification(string Marker);

public static class Bind1ExceptionClassifier
{
    public static Bind1ExceptionClassification? Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is ReflectionTypeLoadException reflectionFailure)
        {
            foreach (var loaderFailure in reflectionFailure.LoaderExceptions)
            {
                if (loaderFailure is null)
                    continue;

                var classified = Classify(loaderFailure);
                if (classified is not null)
                    return classified;
            }

            return null;
        }

        if (exception is FileNotFoundException missingFile && TryGetRequestedIdentity(missingFile, out var requested))
        {
            return new Bind1ExceptionClassification(BindingLogFormatter.FormatBindFail(
                requested,
                $"{exception.GetType().Name}: {exception.Message}"));
        }

        if (exception is TypeLoadException typeFailure && !string.IsNullOrWhiteSpace(typeFailure.TypeName))
        {
            return new Bind1ExceptionClassification(BindingLogFormatter.FormatMissingType(
                typeFailure.TypeName,
                "unknown",
                typeFailure.Message));
        }

        if (exception is MissingMemberException memberFailure)
        {
            return new Bind1ExceptionClassification(BindingLogFormatter.FormatMissingMember(
                "unknown",
                "unknown",
                "unknown",
                memberFailure.Message));
        }

        return null;
    }

    private static bool TryGetRequestedIdentity(FileNotFoundException exception, out AssemblyIdentity identity)
    {
        identity = null!;
        if (string.IsNullOrWhiteSpace(exception.FileName))
            return false;

        try
        {
            identity = AssemblyIdentity.FromAssemblyName(new AssemblyName(exception.FileName));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
