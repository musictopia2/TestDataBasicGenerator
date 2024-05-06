namespace TestDataBasicGenerator.Parsing;
internal record MethodModel
{
    public string Name { get; set; } = "";
    public ImmutableArray<ParameterModel> Parameters { get; set; } = [];
    public int TotalParameters { get; set; }
    public int OptionalParameters { get; set; }
}