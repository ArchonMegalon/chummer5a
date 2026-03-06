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
        }).AllowPublicApiKeyBypass();

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
        }).AllowPublicApiKeyBypass();

        app.MapPost("/api/rulepacks/{packId}/install-preview", (string packId, string? ruleset, RuleProfileApplyTarget target, IRulePackInstallService rulePackInstallService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RulePackInstallPreviewReceipt? preview = rulePackInstallService.Preview(ownerContextAccessor.Current, packId, target, ruleset);
            return preview is null
                ? Results.NotFound(new
                {
                    error = "rulepack_not_found",
                    packId
                })
                : Results.Ok(preview);
        });

        app.MapPost("/api/rulepacks/{packId}/install", (string packId, string? ruleset, RuleProfileApplyTarget target, IRulePackInstallService rulePackInstallService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RulePackInstallReceipt? receipt = rulePackInstallService.Apply(ownerContextAccessor.Current, packId, target, ruleset);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "rulepack_not_found",
                    packId
                })
                : Results.Ok(receipt);
        });

        return app;
    }
}
