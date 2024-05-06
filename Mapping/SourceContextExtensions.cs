namespace TestDataBasicGenerator.Mapping;
internal static class SourceContextExtensions
{
    public static void RaiseException(this SourceProductionContext context, string information)
    {
        context.ReportDiagnostic(Diagnostic.Create(Context(information), Location.None));
    }
    private static DiagnosticDescriptor Context(string information) => new("First",
       "Source Generator Failed",
       information,
       "SourceGenerator",
       DiagnosticSeverity.Error,
       true
       );
}