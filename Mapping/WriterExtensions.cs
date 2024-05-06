namespace TestDataBasicGenerator.Mapping;
internal static class WriterExtensions
{
    public static IWriter PopulateModelCompleteNamespace(this IWriter w, ResultsModel result)
    {
        w.Write(result.ModelNamespace)
            .Write(".")
            .Write(result.ModelName);
        return w;
    }
}