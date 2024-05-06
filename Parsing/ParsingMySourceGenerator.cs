using System.Diagnostics;

namespace TestDataBasicGenerator.Parsing;
[Generator]
public class ParsingMySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> declares1 = context.SyntaxProvider.CreateSyntaxProvider(
            (s, _) => IsSyntaxTarget(s),
            (t, _) => GetTarget(t))
            .Where(m => m != null)!;
        var declares2 = context.CompilationProvider.Combine(declares1.Collect());
        var declares3 = declares2.SelectMany(static (x, _) =>
        {
            ImmutableHashSet<ClassDeclarationSyntax> start = [.. x.Right];

            return GetResults(start, x.Left);
        });
        var declares4 = declares3.Collect();
        var declares5 = declares4.Combine(context.CompilationProvider).Select(static (item, _) =>
        {
            CompleteInformation output;
            output = new($"{item.Right.AssemblyName}.TestGenerators", item.Left);
            return output;
        });
        context.RegisterSourceOutput(declares5, Execute);
    }
    private bool IsSyntaxTarget(SyntaxNode syntax)
    {
        return syntax is ClassDeclarationSyntax ctx &&
           ctx.IsPublic();
    }
    private ClassDeclarationSyntax? GetTarget(GeneratorSyntaxContext context)
    {
        var ourClass = context.GetClassNode();
        var symbol = context.GetClassSymbol(ourClass);
        bool rets = symbol.Implements("IMustashable");
        if (rets)
        {
            return ourClass;
        }
        return null;

    }
    private static ImmutableHashSet<ResultsModel> GetResults(
        ImmutableHashSet<ClassDeclarationSyntax> classes,
        Compilation compilation
        )
    {

        ParserClass parses = new(classes, compilation);
        BasicList<ResultsModel> output = parses.GetResults();

        return [.. output];
    }
    private void Execute(SourceProductionContext context, CompleteInformation complete)
    {
        if (complete.Results.Count() == 0)
        {
            return; //there was none found.  nothing should be generated from this generator this time.
        }
        EmitClass emit = new(complete, context);
        emit.Emit();
    }
}