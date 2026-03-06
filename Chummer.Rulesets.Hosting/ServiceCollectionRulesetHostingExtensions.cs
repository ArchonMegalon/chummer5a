using Chummer.Application.Workspaces;
using Chummer.Contracts.Rulesets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chummer.Rulesets.Hosting;

public static class ServiceCollectionRulesetHostingExtensions
{
    public static IServiceCollection AddRulesetInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IRulesetPluginRegistry, RulesetPluginRegistry>();
        services.TryAddSingleton<IRulesetShellCatalogResolver, RulesetShellCatalogResolverService>();
        services.TryAddSingleton<IRulesetWorkspaceCodecResolver, RulesetWorkspaceCodecResolver>();
        return services;
    }
}
