using Chummer.Application.Content;
using Chummer.Application.Owners;
using Chummer.Contracts.Content;

namespace Chummer.Api.Endpoints;

public static class RulePackRegistryEndpoints
{
    public static IEndpointRouteBuilder MapRulePackRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rulepacks", (string? ruleset, IRulePackRegistryService rulePackRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            IReadOnlyList<RulePackRegistryEntry> entries = rulePackRegistryService.List(ownerContextAccessor.Current, ruleset);
            return Results.Ok(new
            {
                count = entries.Count,
                entries
            });
        });

        app.MapGet("/api/rulepacks/{packId}", (string packId, string? ruleset, IRulePackRegistryService rulePackRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RulePackRegistryEntry? entry = rulePackRegistryService.Get(ownerContextAccessor.Current, packId, ruleset);
            return entry is null
                ? Results.NotFound(new
                {
                    error = "rulepack_not_found",
                    packId
                })
                : Results.Ok(entry);
        });

        return app;
    }
}
