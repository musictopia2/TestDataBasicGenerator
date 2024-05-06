namespace TestDataBasicGenerator.Parsing;
internal class ParserClass(IEnumerable<ClassDeclarationSyntax> list, Compilation compilation)
{
    public BasicList<ResultsModel> GetResults()
    {
        BasicList<ResultsModel> output = [];
        foreach (var item in list)
        {
            ResultsModel results = GetResult(item);
            if (output.Any(x => x.ClassName == results.ClassName) == false && results.Methods.Count > 0)
            {
                output.Add(results);
            }
        }
        return output;
    }
    private ResultsModel GetResult(ClassDeclarationSyntax classDeclaration)
    {
        ResultsModel output;
        INamedTypeSymbol symbol = compilation.GetClassSymbol(classDeclaration)!;
        output = symbol.GetStartingResults<ResultsModel>();
        output.Methods = GetMethods(symbol);
        return output;
    }
    private bool CanUse(INamedTypeSymbol originalSymbol, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ReceiverType!.Name != originalSymbol.Name)
        {
            return false;
        }
        EnumSimpleTypeCategory category;
        category = methodSymbol.ReturnType.GetVariableCategory();
        if (category == EnumSimpleTypeCategory.None)
        {
            return false;
        }
        return true;
    }
    private ImmutableHashSet<MethodModel> GetMethods(INamedTypeSymbol classSymbol)
    {
        BasicList<MethodModel> output = [];
        var list = classSymbol.GetAllPublicMethods();
        foreach (var item in list)
        {
            if (CanUse(classSymbol, item))
            {
                MethodModel method = new();
                method.Name = item.Name;
                method.Parameters = GetParameters(item);
                method.TotalParameters = method.Parameters.Count(x => x.ParmeterCategory != EnumParameterCategory.NotAllowed);
                method.OptionalParameters = method.Parameters.Count(x => x.ParmeterCategory == EnumParameterCategory.Optional);
                if (method.Parameters.Any(x => x.ParmeterCategory == EnumParameterCategory.Disabled) == false)
                {
                    output.Add(method);
                }
            }
        }
        var nexts = output.GroupBy(x => x.Name).ToBasicList();
        foreach (var item in nexts)
        {
            if (item.Count() > 1)
            {
                int upTo = 0;
                foreach (var item2 in item)
                {
                    upTo++;
                    item2.OverloadPart = upTo.ToString();
                }
            }
        }
        return [.. output];
    }
    private ImmutableArray<ParameterModel> GetParameters(IMethodSymbol symbol)
    {
        BasicList<ParameterModel> output = [];
        var list = symbol.Parameters;
        foreach (var item in list)
        {
            ParameterModel p = new();
            p.TypeCategory = item.Type.GetVariableCategory();
            if (p.TypeCategory == EnumSimpleTypeCategory.None)
            {
                p.ParmeterCategory = EnumParameterCategory.NotAllowed;
            }
            else if (item.IsOptional)
            {
                p.ParmeterCategory = EnumParameterCategory.Optional;
            }
            else
            {
                p.ParmeterCategory = EnumParameterCategory.None;
            }
            if (item.IsOptional == false && p.TypeCategory == EnumSimpleTypeCategory.None)
            {
                p.ParmeterCategory = EnumParameterCategory.Disabled;
            }
            if (p.TypeCategory == EnumSimpleTypeCategory.StandardEnum)
            {
                INamedTypeSymbol fins = item.Type.GetUnderlyingSymbol();
                if (fins.DeclaringSyntaxReferences.Any())
                {
                    p.FullName = $"{fins.ContainingNamespace}.{fins.Name}";
                    p.TypeName = fins.Name;
                    p.EnumValues = GetStandardEnums(fins);
                }
                else
                {
                    p.ParmeterCategory = EnumParameterCategory.NotAllowed;
                }
            }
            else if (p.TypeCategory == EnumSimpleTypeCategory.CustomEnum)
            {
                INamedTypeSymbol fins = item.Type.GetUnderlyingSymbol();
                if (fins.DeclaringSyntaxReferences.Any())
                {
                    p.FullName = $"{fins.ContainingNamespace}.{fins.Name}";
                    p.TypeName = fins.Name;
                    p.EnumValues = GetCustomEnums(fins);
                }
                else
                {
                    p.ParmeterCategory = EnumParameterCategory.NotAllowed;
                }
            }
            output.Add(p);
        }
        if (output.Any(x => x.ParmeterCategory == EnumParameterCategory.NotAllowed))
        {
            if (output.All(x => x.ParmeterCategory == EnumParameterCategory.NotAllowed) == false)
            {
                foreach (var item in output)
                {
                    item.ParmeterCategory = EnumParameterCategory.Disabled;
                }
            }
        }
        return [.. output];
    }
    private ImmutableHashSet<EnumModel> GetCustomEnums(ITypeSymbol symbol)
    {
        BasicList<EnumModel> output = [];
        var test = symbol.DeclaringSyntaxReferences;
        var part1 = symbol.DeclaringSyntaxReferences.Single().SyntaxTree.GetAllMembers().Single();
        var list = part1.DescendantNodes().OfType<EnumDeclarationSyntax>().ToBasicList();
        if (list.Count != 1)
        {
            return [.. output];
        }
        var nexts = list.Single().DescendantNodes().OfType<EnumMemberDeclarationSyntax>().ToBasicList();
        int oldValue = 0;
        foreach (var next in nexts)
        {
            var fins = next.DescendantNodes().OfType<EqualsValueClauseSyntax>().SingleOrDefault();
            if (fins is not null)
            {
                var aa = fins.Value.ToString();
                oldValue = int.Parse(aa);
            }
            EnumModel model = new();
            model.FullName = $"{symbol.ContainingNamespace}.{symbol.Name}";
            model.IntValue = oldValue.ToString();
            string value = next.Identifier.ValueText;
            model.DisplayValue = value;
            model.PartialStringValue = value.ToLower();
            model.FullStringValue = $"{symbol.Name}.{value}";
            model.FullStringValue = model.FullStringValue.ToLower();
            output.Add(model);
            oldValue++;
        }
        return [.. output];
    }
    private ImmutableHashSet<EnumModel> GetStandardEnums(ITypeSymbol symbol)
    {
        BasicList<EnumModel> output = [];
        var firstEnums = symbol.DeclaringSyntaxReferences.Single().SyntaxTree.GetAllMembers().OfType<EnumDeclarationSyntax>().ToBasicList();
        foreach (var item in firstEnums)
        {
            string name = item.Identifier.ValueText;
            if (name == symbol.Name)
            {
                var nexts = item.DescendantNodes().OfType<EnumMemberDeclarationSyntax>().ToBasicList();
                int oldValue = 0;
                foreach (var next in nexts)
                {
                    var fins = next.DescendantNodes().OfType<EqualsValueClauseSyntax>().SingleOrDefault();
                    if (fins is not null)
                    {
                        var aa = fins.Value.ToString();
                        oldValue = int.Parse(aa);
                    }
                    EnumModel model = new();
                    model.FullName = $"{symbol.ContainingNamespace}.{symbol.Name}";
                    model.IntValue = oldValue.ToString();
                    string value = next.Identifier.ValueText;
                    model.DisplayValue = value;
                    model.PartialStringValue = value.ToLower();
                    model.FullStringValue = $"{symbol.Name}.{value}";
                    model.FullStringValue = model.FullStringValue.ToLower();
                    output.Add(model);
                    oldValue++;
                }
            }
        }
        return [.. output];
    }
}