using Chummer.Application.Content;
using Chummer.Application.Owners;

namespace Chummer.Api.Endpoints;

public static class RuntimeInspectorEndpoints
{
    public static IEndpointRouteBuilder MapRuntimeInspectorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/runtime/profiles/{profileId}", (string profileId, string? ruleset, IRuntimeInspectorService runtimeInspectorService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            var projection = runtimeInspectorService.GetProfileProjection(ownerContextAccessor.Current, profileId, ruleset);
            return projection is null
                ? Results.NotFound(new
                {
                    error = "runtime_target_not_found",
                    targetKind = "profile",
                    targetId = profileId
                })
                : Results.Ok(projection);
        });

        return app;
    }
}
