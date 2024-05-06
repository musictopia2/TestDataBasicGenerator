namespace TestDataBasicGenerator.Mapping;
internal record CompleteInformation
{
    public string Namespace { get; set; } = "";
    public BasicList<ResultsModel> Maps { get; set; } = [];
}