using System.Diagnostics;
using System.Runtime;

namespace TestDataBasicGenerator.Mapping;
internal class ParserClass(IEnumerable<ClassDeclarationSyntax> list, Compilation compilation)
{
    public BasicList<CompleteInformation> GetComplete()
    {
        BasicList<CompleteInformation> output = [];
        foreach (var item in list)
        {
            CompleteInformation complete = GetComplete(item);
            output.Add(complete);
        }
        return output;
    }
    private CompleteInformation GetComplete(ClassDeclarationSyntax classDeclaration)
    {
        CompleteInformation output = new();
        output.Maps = GetResults(classDeclaration);
        output.Namespace = $"{compilation.AssemblyName}.TestGenerators.MapGlobals";
        return output;
    }
    private BasicList<ResultsModel> GetResults(ClassDeclarationSyntax classDeclaration)
    {
        ParseContext context = new(compilation, classDeclaration);
        var members = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var m in members)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(m) as IMethodSymbol;
            if (symbol is not null && symbol.Name == BaseRulesMappersContext.ConfigureName)
            {
                return ParseContext(context, m);
            }
        }
        return [];
    }
    private BasicList<ResultsModel> ParseContext(ParseContext context, MethodDeclarationSyntax syntax)
    {
        static CallInfo? GetPropertyCall(IReadOnlyList<CallInfo> calls, IPropertySymbol p, ITypeSymbol classSymbol)
        {
            foreach (var call in calls)
            {
                var ignoreIdentifier = call.Invocation.DescendantNodes()
                       .OfType<IdentifierNameSyntax>()
                       .Last();
                var cloneProp = classSymbol.GetMembers(ignoreIdentifier.Identifier.ValueText)
                .OfType<IPropertySymbol>()
                .SingleOrDefault();
                if (cloneProp.Name == p.Name && cloneProp.OriginalDefinition.ToDisplayString() == p.OriginalDefinition.ToDisplayString())
                {
                    return call;
                }
            }
            return null;
        }
        var makeCalls = ParseUtils.FindCallsOfMethodWithName(context, syntax, nameof(IRuleMapConfig.MapRulesWithPropertiesOptions));
        ResultsModel result;
        BasicList<ResultsModel> output = [];
        foreach (var make in makeCalls)
        {
            INamedTypeSymbol makeType = (INamedTypeSymbol)make.MethodSymbol.TypeArguments[0]!;
            result = GetResultsFromSymbol(makeType);
            var pList = makeType.GetAllPublicProperties();
            var seconds = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, nameof(IRuleMapPropertyConfig<object>.Ignore), optional: true, argumentIndex: 1);
            var thirds = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, nameof(IRuleMapPropertyConfig<object>.Forbid), optional: true, argumentIndex: 1);
            var fourths = ParseUtils.FindCallsOfMethodInConfigLambda(context, make, nameof(IRuleMapPropertyConfig<object>.Force), optional: true, argumentIndex: 1);
            foreach (var p in pList)
            {
                if (CanIncludeProperty(p))
                {
                    PropertyInformation fins = GetStartProperty(p);
                    result.Properties.Add(fins);
                    var call = GetPropertyCall(fourths, p, makeType);
                    if (call is not null)
                    {
                        fins.Category = "global::TestDataUSBasicLibrary.SourceGeneratorHelpers.EnumRuleCategory.Force";
                        continue;
                    }
                    call = GetPropertyCall(thirds, p, makeType);
                    if (call is not null)
                    {
                        fins.Category = "global::TestDataUSBasicLibrary.SourceGeneratorHelpers.EnumRuleCategory.Forbid";
                        continue;
                    }
                    call = GetPropertyCall(seconds, p, makeType);
                    if (call is not null)
                    {
                        fins.Category = "global::TestDataUSBasicLibrary.SourceGeneratorHelpers.EnumRuleCategory.Ignore";
                    }
                }
            }
            output.Add(result);
        }
        makeCalls = ParseUtils.FindCallsOfMethodWithName(context, syntax, nameof(IRuleMapConfig.MapRulesWithDefaults));
        foreach (var make in makeCalls)
        {
            INamedTypeSymbol makeType = (INamedTypeSymbol)make.MethodSymbol.TypeArguments[0]!;
            result = GetResultsFromSymbol(makeType);
            var pList = makeType.GetAllPublicProperties();
            foreach (var p in pList)
            {
                if (CanIncludeProperty(p))
                {
                    PropertyInformation fins = GetStartProperty(p);
                    result.Properties.Add(fins);
                }
            }
            output.Add(result);
        }
        return output;

    }
    private PropertyInformation GetStartProperty(IPropertySymbol symbol)
    {
        PropertyInformation output = new();
        output.Category = "";
        TemporaryModel temp = symbol.GetStartingPropertyInformation<TemporaryModel>();
        output.Name = temp.PropertyName;
        output.Type = GetPropertyType(symbol);
        if (output.Type.ToLower().EndsWith("basiclist"))
        {
            var mm = symbol.Type.GetSingleGenericTypeUsed();
            string firsts = output.Type;
            string nexts = GetGenericType(mm!);
            if (nexts == "")
            {
                nexts = mm!.Name.ToLower();
            }
            output.Type = $"{firsts}<{nexts}>";
        }
        return output;
    }
    private string GetGenericType(ITypeSymbol symbol)
    {
        string output;
        if (symbol.Name.ToLower() == "guid")
        {
            output = "Guid";
        }
        else if (symbol.Name.ToLower() == "int64")
        {
            output = "long";
        }
        else if (symbol.Name.ToLower() == "int32")
        {
            output = "int"; //has to be int in this case.
        }
        else
        {
            output = "";
        }
        return output;
    }
    private string GetPropertyType(IPropertySymbol symbol)
    {
        TemporaryModel temp = symbol.GetStartingPropertyInformation<TemporaryModel>();
        string output = "";
        string firsts = GetGenericType(symbol.Type);
        if (firsts != "")
        {
            return firsts; //i think this is the bug.
        }
        if (temp.VariableCustomCategory == EnumSimpleTypeCategory.StandardEnum || temp.VariableCustomCategory == EnumSimpleTypeCategory.None || temp.VariableCustomCategory == EnumSimpleTypeCategory.CustomEnum)
        {
            output = $"global::{temp.ContainingNameSpace}.{temp.UnderlyingSymbolName}";
        }
        else if (temp.VariableCustomCategory == EnumSimpleTypeCategory.DateOnly || temp.VariableCustomCategory == EnumSimpleTypeCategory.TimeOnly || temp.VariableCustomCategory == EnumSimpleTypeCategory.DateTime)
        {
            output = temp.VariableCustomCategory.ToString();
        }
        else
        {
            output = temp.VariableCustomCategory.ToString().ToLower();
        }
        return output;
    }

    private bool CanIncludeProperty(IPropertySymbol p)
    {
        if (p.IsReadOnly)
        {
            return false;
        }
        return true;
    }
    private ResultsModel GetResultsFromSymbol(INamedTypeSymbol symbol)
    {
        ResultsModel output = new();
        output.ModelName = symbol.Name;
        output.ModelNamespace = symbol.ContainingNamespace.ToDisplayString();
        if (symbol.Constructors.Count() > 1)
        {
            output.HasRequiredConstructors = true;
        }
        else if (symbol.Constructors.Single().ConstructedFrom.Parameters.Count() > 0)
        {
            output.HasRequiredConstructors = true;
        }
        else
        {
            output.HasRequiredConstructors = false;
        }
        output.ClassName = $"Generate{symbol.Name}TestMapperClass";
        output.ClassNamespace = $"{compilation.AssemblyName}.TestGenerators.Classes";
        return output;
    }
}