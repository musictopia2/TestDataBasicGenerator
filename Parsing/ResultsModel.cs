namespace TestDataBasicGenerator.Parsing;
internal record ResultsModel : ICustomResult
{
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public ImmutableHashSet<MethodModel> Methods { get; set; } = [];
}