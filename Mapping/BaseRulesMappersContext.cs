global using TestDataBasicGenerator.Mapping;
namespace TestDataBasicGenerator.Mapping;
[IncludeCode]
internal interface IRuleMapPropertyConfig<T>
{
    IRuleMapPropertyConfig<T> Ignore<P>(Func<T, P> propertySelector);
    IRuleMapPropertyConfig<T> Force<P>(Func<T, P> propertySelector);
    IRuleMapPropertyConfig<T> Forbid<P>(Func<T, P> propertySelector);
}
internal interface IRuleMapConfig
{
    IRuleMapConfig MapRulesWithDefaults<D>();
    IRuleMapConfig MapRulesWithPropertiesOptions<T>(Action<IRuleMapPropertyConfig<T>> action);
}

internal abstract class BaseRulesMappersContext
{
    public const string ConfigureName = nameof(Configure);
    protected abstract void Configure(IRuleMapConfig config);
}