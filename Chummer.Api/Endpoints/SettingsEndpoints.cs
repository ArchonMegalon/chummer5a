using System.Text.Json.Nodes;
using Chummer.Application.Owners;
using Chummer.Application.Tools;
using Chummer.Contracts.Owners;

namespace Chummer.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tools/settings/{scope}", (string scope, ISettingsStore settingsStore, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (!TryNormalizeScope(scope, out string normalizedScope))
                return Results.BadRequest(new { error = "scope must be 'global' or 'character'." });

            OwnerScope owner = ownerContextAccessor.Current;
            JsonObject settings = settingsStore.Load(owner, normalizedScope);
            return Results.Ok(new { scope = normalizedScope, settings });
        });

        app.MapPost("/api/tools/settings/{scope}", (string scope, JsonObject? settings, ISettingsStore settingsStore, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (!TryNormalizeScope(scope, out string normalizedScope))
                return Results.BadRequest(new { error = "scope must be 'global' or 'character'." });

            OwnerScope owner = ownerContextAccessor.Current;
            settingsStore.Save(owner, normalizedScope, settings ?? new JsonObject());
            return Results.Ok(new { scope = normalizedScope, saved = true });
        });

        return app;
    }

    private static bool TryNormalizeScope(string scope, out string normalizedScope)
    {
        normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedScope is "global" or "character";
    }
}
