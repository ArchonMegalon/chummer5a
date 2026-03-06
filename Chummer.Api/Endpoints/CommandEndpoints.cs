using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Api.Endpoints;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/commands", (string? ruleset, IRulesetShellCatalogResolver shellCatalogResolver) =>
        {
            IReadOnlyList<AppCommandDefinition> commands;
            try
            {
                commands = shellCatalogResolver.ResolveCommands(ruleset);
            }
            catch (InvalidOperationException) when (!string.IsNullOrWhiteSpace(ruleset))
            {
                return Results.BadRequest(new
                {
                    error = "unknown_ruleset",
                    rulesetId = ruleset.Trim()
                });
            }

            return Results.Ok(new AppCommandCatalogResponse(
                Count: commands.Count,
                Commands: commands));
        });

        return app;
    }
}
