using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Api.Endpoints;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/commands", (string? ruleset) =>
        {
            IReadOnlyList<AppCommandDefinition> commands = AppCommandCatalog.ForRuleset(RulesetDefaults.Normalize(ruleset));
            return Results.Ok(new AppCommandCatalogResponse(
                Count: commands.Count,
                Commands: commands));
        });

        return app;
    }
}
