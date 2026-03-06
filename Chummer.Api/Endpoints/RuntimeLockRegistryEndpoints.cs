using Chummer.Application.Content;
using Chummer.Application.Owners;

namespace Chummer.Api.Endpoints;

public static class RuntimeLockRegistryEndpoints
{
    public static IEndpointRouteBuilder MapRuntimeLockRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/runtime/locks", (string? ruleset, IRuntimeLockRegistryService runtimeLockRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            var page = runtimeLockRegistryService.List(ownerContextAccessor.Current, ruleset);
            return Results.Ok(new
            {
                count = page.TotalCount,
                entries = page.Entries
            });
        }).AllowPublicApiKeyBypass();

        app.MapGet("/api/runtime/locks/{lockId}", (string lockId, string? ruleset, IRuntimeLockRegistryService runtimeLockRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            var entry = runtimeLockRegistryService.Get(ownerContextAccessor.Current, lockId, ruleset);
            return entry is null
                ? Results.NotFound(new
                {
                    error = "runtime_lock_not_found",
                    lockId
                })
                : Results.Ok(entry);
        }).AllowPublicApiKeyBypass();

        return app;
    }
}
