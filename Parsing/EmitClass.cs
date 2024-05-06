namespace TestDataBasicGenerator.Parsing;
internal class EmitClass(CompleteInformation complete, SourceProductionContext context)
{
    public void Emit()
    {
        WriteClass();
        WriteGlobal();
    }
    private void WriteClass()
    {
        SourceCodeStringBuilder builder = new();
        //this is for printing.
        builder.WriteLine("#nullable enable")
               .WriteLine(w =>
               {
                   w.Write("namespace ")
                   .Write(complete.Namespace)
                   .Write(";");
               })
           .WriteLine(w =>
           {
               w.Write("public class TestDataParsingClass : global::TestDataUSBasicLibrary.SourceGeneratorHelpers.ICaptureDatasets");
           })
           .WriteCodeBlock(w =>
           {
               w.WriteLine("global::CommonBasicLibraries.CollectionClasses.BasicList<global::TestDataUSBasicLibrary.SourceGeneratorHelpers.DataSetClassModel> global::TestDataUSBasicLibrary.SourceGeneratorHelpers.ICaptureDatasets.DataSets()")
               .WriteCodeBlock(w =>
               {
                   w.WriteLine("global::CommonBasicLibraries.CollectionClasses.BasicList<global::TestDataUSBasicLibrary.SourceGeneratorHelpers.DataSetClassModel> output = [];")
                   .WriteLine("global::TestDataUSBasicLibrary.SourceGeneratorHelpers.DataSetClassModel dataSet;")
                   .WriteLine("global::TestDataUSBasicLibrary.SourceGeneratorHelpers.DataSetMethodModel method;");
                   FinishMethod(w);
                   w.WriteLine("return output;");
               });
               PopulatePrivateMethods(w);
           });
        context.AddSource("TestFrameworkParsingCapture.g.cs", builder.ToString());
    }
    private void PopulatePrivateMethods(ICodeBlock w)
    {
        //will do the hardest part.
        foreach (var item in complete.Results)
        {
            foreach (var m in item.Methods)
            {
                w.PopulatePrivateMethod(m, item);
            }
        }
    }
    private void FinishMethod(ICodeBlock w)
    {
        //here is where i figure out what else i need.
        foreach (var item in complete.Results)
        {
            //dataSet.Name = "PretendData";
            w.WriteLine("dataSet = new();")
                .WriteLine($"""
                dataSet.Name = "{item.ClassName}";
                """)
                .WriteLine($"dataSet.DeclaringType = typeof({item.Namespace}.{item.ClassName});")
                .WriteLine("output.Add(dataSet);");
            foreach (var m in item.Methods)
            {
                w.PopulateMainMethod(m);
            }
        }
    }
    private void WriteGlobal()
    {
        SourceCodeStringBuilder builder = new();
        //this is for printing.
        builder.WriteLine("#nullable enable")
               .WriteLine(w =>
               {
                   w.Write("namespace ")
                   .Write(complete.Namespace)
                   .Write(";");
               })
           .WriteLine(w =>
           {
               w.Write("internal static class RegisterTestParsing");
           })
           .WriteCodeBlock(w =>
           {
               w.WriteLine("public static void RegisterDataSets()")
               .WriteCodeBlock(w =>
               {
                   w.WriteLine("global::TestDataUSBasicLibrary.SourceGeneratorHelpers.ICaptureDatasets parses = new TestDataParsingClass();");
                   w.WriteLine("global::TestDataUSBasicLibrary.Tokenizer.DataSets.AddRange(parses.DataSets());");
               });
           });
        context.AddSource("TestFrameworkParsingRegistrations.g.cs", builder.ToString());
    }
}