namespace TestDataBasicGenerator.Parsing;
internal static class CodeBlockExtensions
{
    public static ICodeBlock PopulateMainMethod(this ICodeBlock w, MethodModel method)
    {
        w.WriteLine("method = new ();")
            .WriteLine($"""
            method.Name = "{method.Name}";
            """)
            .WriteLine($"method.TotalArgumentsCount = {method.TotalParameters};")
            .WriteLine($"method.OptionalArgumentsCount = {method.OptionalParameters};")
            .WriteLine($"method.Invoke = Invoke{method.Name}Results;")
            .WriteLine("dataSet.Methods.Add(method);");
        return w;
    }
    public static ICodeBlock PopulatePrivateMethod(this ICodeBlock w, MethodModel method, ResultsModel result)
    {
        w.WriteLine($"private string Invoke{method.Name}Results(object payLoad, global::CommonBasicLibraries.CollectionClasses.BasicList<string> arguments)")
            .WriteCodeBlock(w =>
            {
                w.WriteLine($"if (payLoad is {result.Namespace}.{result.ClassName} data)")
                .WriteCodeBlock(w =>
                {
                    w.PopulateArguments(method);
                })
                .WriteLine("else")
                .WriteCodeBlock(w =>
                {
                    w.WriteLine("""
                        throw new CustomBasicException("Invalid cast when invoking to get the data");
                        """);
                });
            });
        return w;
    }
    private static ICodeBlock PopulateResultOfEnum(this ICodeBlock w, ParameterModel p, EnumModel e)
    {
        w.WriteLine($"enumToUse = {p.FullName}.{e.DisplayValue};");
        return w;
    }
    private static ICodeBlock PopulateEnumSingleArgument(this ICodeBlock w, MethodModel method, ParameterModel p)
    {
        w.WriteLine($"{p.FullName} enumToUse = default;")
            .WriteLine("string argument = arguments.Single();");
        foreach (var item in p.EnumValues)
        {
            w.WriteLine($"""
                if (argument == "{item.IntValue}")
                """)
                .WriteCodeBlock(w =>
                {
                    w.PopulateResultOfEnum(p, item);
                });
            w.WriteLine($"""
                if (argument.Equals("{item.PartialStringValue}", StringComparison.CurrentCultureIgnoreCase))
                """)
                .WriteCodeBlock(w => w.PopulateResultOfEnum(p, item));
            w.WriteLine($"""
                if (argument == "{item.FullStringValue}")
                """)
                .WriteCodeBlock(w => w.PopulateResultOfEnum(p, item));
        }
        w.WriteLine($"return data.{method.Name}(enumToUse).ToString();");
        return w;
    }
    private static ICodeBlock PopulateSimpleReturn(this ICodeBlock w, MethodModel method)
    {
        w.WriteLine($"return data.{method.Name}().ToString();");
        return w;
    }
    private static ICodeBlock PopulateSingleArgumentInformation(this ICodeBlock w, MethodModel method)
    {
        ParameterModel p = method.Parameters.Single(x => x.ParmeterCategory != EnumParameterCategory.NotAllowed);
        if (p.TypeCategory == EnumSimpleTypeCategory.CustomEnum || p.TypeCategory == EnumSimpleTypeCategory.StandardEnum)
        {
            w.PopulateEnumSingleArgument(method, p);
            return w;
        }
        if (p.TypeCategory != EnumSimpleTypeCategory.String)
        {
            string toUse = p.TypeCategory.GetParseMethodName();
            w.WriteLine($"return data.{method.Name}({toUse}.Parse(arguments[0])).ToString();");
        }
        else
        {
            w.WriteLine($"return data.{method.Name}((arguments[0]).ToString();");
        }
        return w;
    }
    private static ICodeBlock PopulateArguments(this ICodeBlock w, MethodModel method)
    {
        if (method.TotalParameters == 0)
        {
            w.PopulateSimpleReturn(method);
            return w;
        }
        if (method.TotalParameters == 1)
        {
            if (method.OptionalParameters == 1)
            {
                w.WriteLine("if (arguments.Count == 1)")
                    .WriteCodeBlock(w =>
                    {
                        w.PopulateSingleArgumentInformation(method);
                    })
                    .WriteLine("else")
                    .WriteCodeBlock(w =>
                    {
                        w.PopulateSimpleReturn(method);
                    });
            }
            else
            {
                w.PopulateSingleArgumentInformation(method);
            }
            return w;
        }
        if (method.OptionalParameters == 0)
        {
            w.ProcessWithSpecificArguments(method, method.Parameters.ToBasicList());
            return w;
        }
        w.ProcessSeveralArguments(method);
        return w;
    }
    private static ICodeBlock ProcessWithSpecificArguments(this ICodeBlock w, MethodModel method, BasicList<ParameterModel> parameters)
    {
        w.WriteLine(w =>
        {
            w.Write($"return data.{method.Name}(");
            int upTo = 0;
            StrCat cats = new();
            foreach (var item in parameters)
            {
                if (item.TypeCategory != EnumSimpleTypeCategory.String)
                {
                    string toUse = item.TypeCategory.GetParseMethodName();
                    cats.AddToString($"{toUse}.Parse(arguments[{upTo}])", ", ");
                }
                else
                {
                    cats.AddToString($"arguments[{upTo}]", ", ");
                }
                upTo++;
            }
            w.Write(cats.GetInfo());
            w.Write(").ToString();");
        });
        return w;
    }
    private static ICodeBlock ProcessIfWithArguments(this ICodeBlock w, int upTo, MethodModel method, BasicList<ParameterModel> parameters)
    {
        w.WriteLine($"if (arguments.Count == {upTo})")
            .WriteCodeBlock(w =>
            {
                if (upTo > 0)
                {
                    w.ProcessWithSpecificArguments(method, parameters);
                }
                else
                {
                    w.PopulateSimpleReturn(method);
                }
            });
        return w;
    }
    private static ICodeBlock ProcessSeveralArguments(this ICodeBlock w, MethodModel method)
    {
        BasicList<ParameterModel> filteredList;
        int requested;
        int upTo;
        if (method.TotalParameters == method.OptionalParameters)
        {
            requested = method.TotalParameters;
            upTo = 0;
            requested.Times(() =>
            {
                filteredList = method.Parameters.Take(upTo).ToBasicList();
                w.ProcessIfWithArguments(upTo, method, filteredList);
                upTo++;
            });
            w.ProcessWithSpecificArguments(method, method.Parameters.ToBasicList());
            return w;
        }
        requested = method.TotalParameters;
        upTo = method.TotalParameters - method.OptionalParameters;
        requested.Times(() =>
        {
            if (upTo < method.TotalParameters)
            {
                filteredList = method.Parameters.Take(upTo).ToBasicList();
                w.ProcessIfWithArguments(upTo, method, filteredList);
            }
            upTo++;
        });
        w.ProcessWithSpecificArguments(method, method.Parameters.ToBasicList());
        return w;
    }
}