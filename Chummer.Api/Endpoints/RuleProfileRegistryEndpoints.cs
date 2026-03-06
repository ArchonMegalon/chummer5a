using Chummer.Application.Content;
using Chummer.Application.Owners;
using Chummer.Contracts.Content;

namespace Chummer.Api.Endpoints;

public static class RuleProfileRegistryEndpoints
{
    public static IEndpointRouteBuilder MapRuleProfileRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles", (string? ruleset, IRuleProfileRegistryService ruleProfileRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            IReadOnlyList<RuleProfileRegistryEntry> entries = ruleProfileRegistryService.List(ownerContextAccessor.Current, ruleset);
            return Results.Ok(new
            {
                count = entries.Count,
                entries
            });
        }).AllowPublicApiKeyBypass();

        app.MapGet("/api/profiles/{profileId}", (string profileId, string? ruleset, IRuleProfileRegistryService ruleProfileRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RuleProfileRegistryEntry? entry = ruleProfileRegistryService.Get(ownerContextAccessor.Current, profileId, ruleset);
            return entry is null
                ? Results.NotFound(new
                {
                    error = "ruleprofile_not_found",
                    profileId
                })
                : Results.Ok(entry);
        }).AllowPublicApiKeyBypass();

        app.MapPost("/api/profiles/{profileId}/preview", (string profileId, string? ruleset, RuleProfileApplyTarget target, IRuleProfileApplicationService ruleProfileApplicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RuleProfilePreviewReceipt? preview = ruleProfileApplicationService.Preview(ownerContextAccessor.Current, profileId, target, ruleset);
            return preview is null
                ? Results.NotFound(new
                {
                    error = "ruleprofile_not_found",
                    profileId
                })
                : Results.Ok(preview);
        }).AllowPublicApiKeyBypass();

        app.MapPost("/api/profiles/{profileId}/apply", (string profileId, string? ruleset, RuleProfileApplyTarget target, IRuleProfileApplicationService ruleProfileApplicationService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RuleProfileApplyReceipt? receipt = ruleProfileApplicationService.Apply(ownerContextAccessor.Current, profileId, target, ruleset);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "ruleprofile_not_found",
                    profileId
                })
                : Results.Ok(receipt);
        });

        return app;
    }
}
