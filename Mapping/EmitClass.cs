namespace TestDataBasicGenerator.Mapping;
internal class EmitClass(ImmutableArray<CompleteInformation> list, SourceProductionContext context)
{
    public void Emit()
    {
        if (list.Count() > 1)
        {
            context.RaiseException("Should only have one context class.  Make all in one class for registrations for maps");
            return;
        }
        foreach (var item1 in list)
        {
            foreach (var item2 in item1.Maps)
            {
                WriteItem(item2);
            }
        }
        GlobalProcesses();
    }
    private void WriteItem(ResultsModel item)
    {
        SourceCodeStringBuilder builder = new();
        builder.WriteLine("#nullable enable")
                .WriteLine(w =>
                {
                    w.Write("namespace ")
                    .Write(item.ClassNamespace)
                    .Write(";");
                })
            .WriteLine(w =>
            {
                w.Write("public class ")
                .Write(item.ClassName)
                .Write(" : global::TestDataUSBasicLibrary.SourceGeneratorHelpers.IMapPropertiesForTesting<")
                .Write($"{item.ModelNamespace}.{item.ModelName}>");
            })
            .WriteCodeBlock(w =>
            {
                PopulateDetails(w, item);
            });
        context.AddSource($"{item.ClassName}.MapRules.g.cs", builder.ToString()); //change sample to what you want.
    }
    private void PopulateDetails(ICodeBlock w, ResultsModel result)
    {
        w.PopulateCreateNewObject(result).PopulateGetProperties(result);
    }
    private void GlobalProcesses()
    {
        SourceCodeStringBuilder builder = new();
        CompleteInformation complete = list.Single();
        builder.WriteLine("#nullable enable")
                .WriteLine(w =>
                {
                    w.Write("namespace ")
                    .Write(complete.Namespace)
                    .Write(";");
                })
            .WriteLine(w =>
            {
                w.Write("public class RegisterGlobalClass");

            })
            .WriteCodeBlock(w =>
            {
                PopulateDetails(w, complete);
            });
        context.AddSource($"MapRules.Globals.g.cs", builder.ToString()); //change sample to what you want.
    }
    private void PopulateDetails(ICodeBlock w, CompleteInformation complete)
    {
        w.WriteLine("public static void Register()")
            .WriteCodeBlock(w =>
            {
                foreach (var item in complete.Maps)
                {
                    w.WriteLine(w =>
                    {
                        w.Write("global::TestDataUSBasicLibrary.SourceGeneratorHelpers.TestGeneratorHelpersGlobal<")
                        .PopulateModelCompleteNamespace(item)
                        .Write(">.MasterContext = new ")
                        .Write(item.ClassNamespace)
                        .Write(".")
                        .Write(item.ClassName)
                        .Write("();");
                    });
                }
            });
    }
}