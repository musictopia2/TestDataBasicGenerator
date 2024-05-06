namespace TestDataBasicGenerator.Parsing;
internal record ParameterModel
{
    public string VariableName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public EnumSimpleTypeCategory TypeCategory { get; set; }
    public ImmutableHashSet<EnumModel> EnumValues { get; set; } = [];
    public EnumParameterCategory ParmeterCategory { get; set; }
}