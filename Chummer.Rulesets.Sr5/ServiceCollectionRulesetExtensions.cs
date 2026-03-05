using Chummer.Contracts.Rulesets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chummer.Rulesets.Sr5;

public static class ServiceCollectionRulesetExtensions
{
    public static IServiceCollection AddChummerRulesets(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRulesetPlugin, Chummer.Rulesets.Sr5.Sr5RulesetPlugin>());
        services.TryAddSingleton<IRulesetPluginRegistry, RulesetPluginRegistry>();
        services.TryAddSingleton<IRulesetShellCatalogResolver, RulesetShellCatalogResolverService>();
        return services;
    }
}
