using Chummer.Application.Content;
using Chummer.Application.Owners;
using Chummer.Contracts.Content;

namespace Chummer.Api.Endpoints;

public static class BuildKitRegistryEndpoints
{
    public static IEndpointRouteBuilder MapBuildKitRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buildkits", (string? ruleset, IBuildKitRegistryService buildKitRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            IReadOnlyList<BuildKitRegistryEntry> entries = buildKitRegistryService.List(ownerContextAccessor.Current, ruleset);
            return Results.Ok(new
            {
                count = entries.Count,
                entries
            });
        });

        app.MapGet("/api/buildkits/{buildKitId}", (string buildKitId, string? ruleset, IBuildKitRegistryService buildKitRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            BuildKitRegistryEntry? entry = buildKitRegistryService.Get(ownerContextAccessor.Current, buildKitId, ruleset);
            return entry is null
                ? Results.NotFound(new
                {
                    error = "buildkit_not_found",
                    buildKitId
                })
                : Results.Ok(entry);
        });

        return app;
    }
}
