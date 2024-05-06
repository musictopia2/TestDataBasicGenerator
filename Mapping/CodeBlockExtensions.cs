namespace TestDataBasicGenerator.Mapping;
internal static class CodeBlockExtensions
{
    public static ICodeBlock PopulateCreateNewObject(this ICodeBlock w, ResultsModel result)
    {
        w.WriteLine(w =>
        {
            w.PopulateModelCompleteNamespace(result)
            .Write(" global::TestDataUSBasicLibrary.SourceGeneratorHelpers.IMapPropertiesForTesting<")
            .PopulateModelCompleteNamespace(result)
            .Write(">.CreateNewObject()");
        }).WriteCodeBlock(w =>
        {
            if (result.HasRequiredConstructors == false)
            {
                w.WriteLine("return new();");
            }
            else
            {
                w.WriteLine("return null!;");
            }
        });
        return w;
    }
    public static ICodeBlock PopulateGetProperties(this ICodeBlock w, ResultsModel result)
    {
        w.WriteLine(w =>
        {
            w.Write("Dictionary<string, global::TestDataUSBasicLibrary.SourceGeneratorHelpers.PropertyMapper<")
            .PopulateModelCompleteNamespace(result)
            .Write(">> global::TestDataUSBasicLibrary.SourceGeneratorHelpers.IMapPropertiesForTesting<")
            .PopulateModelCompleteNamespace(result)
            .Write(">.GetProperties()");
        }).WriteCodeBlock(w =>
        {
            w.WriteLine(w =>
            {
                w.Write("Dictionary<string, global::TestDataUSBasicLibrary.SourceGeneratorHelpers.PropertyMapper<")
                .PopulateModelCompleteNamespace(result)
                .Write(">> output = [];");
            });
            w.WriteLine(w =>
            {
                w.Write("global::TestDataUSBasicLibrary.SourceGeneratorHelpers.PropertyMapper<")
                .PopulateModelCompleteNamespace(result)
                .Write("> map;");
            });
            foreach (var property in result.Properties)
            {
                w.PopulateProperty(property);
            }
            w.WriteLine(" return output;");
        });
        return w;
    }
    private static ICodeBlock PopulateProperty(this ICodeBlock w, PropertyInformation p)
    {
        w.WriteLine("map = new();");
        if (p.Category != "")
        {
            w.WriteLine($"map.RuleCategory = {p.Category};");
        }
        w.WriteLine($"map.Type = typeof({p.Type});")
        .WriteLine("map.Action = (obj, result) =>")
        .WriteCodeBlock(w =>
        {
            w.WriteLine("if (result is not null)")
            .WriteCodeBlock(w =>
            {
                w.WriteLine($"obj.{p.Name} = ({p.Type}) result;");
            });
        }, true);
        w.WriteLine($"""
            output.Add("{p.Name}", map);
            """);
        return w;
    }
}