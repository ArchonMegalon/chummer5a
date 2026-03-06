using Chummer.Application.Content;
using Chummer.Application.Owners;
using Chummer.Contracts.Content;

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

        app.MapPut("/api/runtime/locks/{lockId}", (string lockId, RuntimeLockSaveRequest request, IRuntimeLockRegistryService runtimeLockRegistryService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            try
            {
                RuntimeLockRegistryEntry entry = runtimeLockRegistryService.Upsert(ownerContextAccessor.Current, lockId, request);
                return Results.Ok(entry);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_runtime_lock",
                    lockId,
                    message = ex.Message
                });
            }
        });

        app.MapPost("/api/runtime/locks/{lockId}/install-preview", (string lockId, string? ruleset, RuleProfileApplyTarget target, IRuntimeLockInstallService runtimeLockInstallService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RuntimeLockInstallPreviewReceipt? preview = runtimeLockInstallService.Preview(ownerContextAccessor.Current, lockId, target, ruleset);
            return preview is null
                ? Results.NotFound(new
                {
                    error = "runtime_lock_not_found",
                    lockId
                })
                : Results.Ok(preview);
        });

        app.MapPost("/api/runtime/locks/{lockId}/install", (string lockId, string? ruleset, RuleProfileApplyTarget target, IRuntimeLockInstallService runtimeLockInstallService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            RuntimeLockInstallReceipt? receipt = runtimeLockInstallService.Apply(ownerContextAccessor.Current, lockId, target, ruleset);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "runtime_lock_not_found",
                    lockId
                })
                : Results.Ok(receipt);
        });

        return app;
    }
}
