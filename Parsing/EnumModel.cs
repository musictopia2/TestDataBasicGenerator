namespace TestDataBasicGenerator.Parsing;
internal record EnumModel
{
    public string FullName { get; set; } = "";
    public string FullStringValue { get; set; } = "";
    public string PartialStringValue { get; set; } = "";
    public string IntValue { get; set; } = "";
    public string DisplayValue { get; set; } = "";
}