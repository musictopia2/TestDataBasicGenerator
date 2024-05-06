namespace TestDataBasicGenerator.Mapping;
[Generator] //this is important so it knows this class is a generator which will generate code for a class using it.
public class MappingMySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c =>
        {
            c.CreateCustomSource().BuildSourceCode();
        });
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
        var declares4 = declares3.Collect(); //if you need compilation, then look at past samples.  try to do without compilation at the end if possible
        context.RegisterSourceOutput(declares4, Execute);
    }
    private bool IsSyntaxTarget(SyntaxNode syntax)
    {
        bool rets = syntax is ClassDeclarationSyntax ctx &&
            ctx.BaseList is not null &&
            ctx.ToString().Contains(nameof(BaseRulesMappersContext));
        return rets;
    }
    private ClassDeclarationSyntax? GetTarget(GeneratorSyntaxContext context)
    {
        var ourClass = context.GetClassNode(); //can use the sematic model at this stage
        return ourClass; //for this one, return the class always in this case.
    }
    private static ImmutableHashSet<CompleteInformation> GetResults(
        ImmutableHashSet<ClassDeclarationSyntax> classes,
        Compilation compilation
        )
    {
        ParserClass parses = new(classes, compilation);
        BasicList<CompleteInformation> output = parses.GetComplete();
        return [.. output];
    }
    private void Execute(SourceProductionContext context, ImmutableArray<CompleteInformation> list)
    {
        if (list.Count() == 0)
        {
            return; //because there was none.
        }
        EmitClass emit = new(list, context);
        emit.Emit();
    }
}