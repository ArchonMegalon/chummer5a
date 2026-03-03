using Chummer.Contracts.Presentation;

namespace Chummer.Web.Endpoints;

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/commands", () =>
        {
            IReadOnlyList<AppCommandDefinition> commands = AppCommandCatalog.All;
            return Results.Ok(new { count = commands.Count, commands });
        });

        return app;
    }
}
