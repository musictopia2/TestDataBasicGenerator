namespace TestDataBasicGenerator.Mapping;
internal record ResultsModel
{
    public string ClassName { get; set; } = "";
    public string ClassNamespace { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string ModelNamespace { get; set; } = "";
    public bool HasRequiredConstructors { get; set; }
    public BasicList<PropertyInformation> Properties { get; set; } = [];
}