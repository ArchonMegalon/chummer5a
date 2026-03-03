using Chummer.Contracts.Presentation;

namespace Chummer.Api.Endpoints;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/commands", () =>
        {
            IReadOnlyList<AppCommandDefinition> commands = AppCommandCatalog.All;
            return Results.Ok(new AppCommandCatalogResponse(
                Count: commands.Count,
                Commands: commands));
        });

        return app;
    }
}
