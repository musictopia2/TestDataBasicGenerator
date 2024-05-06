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
            .WriteLine($"method.Invoke = Invoke{method.Name}{method.OverloadPart}Results;")
            .WriteLine("dataSet.Methods.Add(method);");
        return w;
    }
    public static ICodeBlock PopulatePrivateMethod(this ICodeBlock w, MethodModel method, ResultsModel result)
    {
        w.WriteLine($"private string Invoke{method.Name}{method.OverloadPart}Results(object payLoad, global::CommonBasicLibraries.CollectionClasses.BasicList<string> arguments)")
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
                        throw new global::CommonBasicLibraries.BasicDataSettingsAndProcesses.CustomBasicException("Invalid cast when invoking to get the data");
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
    private static ICodeBlock PopulateEnumLaterArgument(this ICodeBlock w, MethodModel method)
    {
        var parameter = method.Parameters.First(x => x.TypeCategory == EnumSimpleTypeCategory.CustomEnum || x.TypeCategory == EnumSimpleTypeCategory.StandardEnum);
        int upTo = method.Parameters.IndexOf(parameter);
        w.PopulateEnumArgument(upTo, parameter);
        return w;
    }
    private static ICodeBlock PopulateEnumArgument(this ICodeBlock w, int upTo, ParameterModel p)
    {
        w.WriteLine($"{p.FullName} enumToUse = default;")
            .WriteLine($"string argument = arguments[{upTo}];");
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
        string variable = method.Parameters.Single().VariableName;
        if (p.TypeCategory == EnumSimpleTypeCategory.CustomEnum || p.TypeCategory == EnumSimpleTypeCategory.StandardEnum)
        {
            w.PopulateEnumArgument(0, p);
            w.WriteLine($"return data.{method.Name}({variable}: enumToUse).ToString();");
            return w;
        }
        if (p.TypeCategory != EnumSimpleTypeCategory.String)
        {
            string toUse = p.TypeCategory.GetParseMethodName();
            w.WriteLine($"return data.{method.Name}({variable}: {toUse}.Parse(arguments[0])).ToString();");
        }
        else
        {
            w.WriteLine($"return data.{method.Name}({variable}: (arguments[0]).ToString());");
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
        if (parameters.Any(x => x.TypeCategory == EnumSimpleTypeCategory.CustomEnum || x.TypeCategory == EnumSimpleTypeCategory.StandardEnum))
        {
            w.PopulateEnumLaterArgument(method);
        }
        w.WriteLine(w =>
        {
            w.Write($"return data.{method.Name}(");
            int upTo = 0;
            StrCat cats = new();
            string variable;
            foreach (var item in parameters)
            {
                variable = $"{item.VariableName}:";
                if (item.TypeCategory == EnumSimpleTypeCategory.CustomEnum || item.TypeCategory == EnumSimpleTypeCategory.StandardEnum)
                {
                    cats.AddToString($"{variable} enumToUse", ", ");
                }
                else if (item.TypeCategory != EnumSimpleTypeCategory.String)
                {
                    string toUse = item.TypeCategory.GetParseMethodName();
                    cats.AddToString($"{variable} {toUse}.Parse(arguments[{upTo}])", ", ");
                }
                else
                {
                    cats.AddToString($"{variable} arguments[{upTo}]", ", ");
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