using Chummer.Core.LifeModules;

namespace Chummer.Web.Endpoints;

public static class LifeModulesEndpoints
{
    public static IEndpointRouteBuilder MapLifeModulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lifemodules/stages", (ILifeModulesService lifeModulesService) =>
        {
            var stages = lifeModulesService.GetStages();
            return Results.Ok(stages);
        });

        app.MapGet("/api/lifemodules/modules", (ILifeModulesService lifeModulesService, string? stage) =>
        {
            var modules = lifeModulesService.GetModules(stage);
            return Results.Ok(new { count = modules.Count, modules });
        });

        return app;
    }
}
