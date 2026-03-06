using Chummer.Application.Owners;
using Chummer.Application.Tools;
using Chummer.Contracts.Api;
using Chummer.Contracts.Owners;

namespace Chummer.Api.Endpoints;

public static class RosterEndpoints
{
    public static IEndpointRouteBuilder MapRosterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tools/roster", (IRosterStore rosterStore, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            IReadOnlyList<RosterEntry> entries = rosterStore.Load(owner);
            return Results.Ok(new { count = entries.Count, entries });
        });

        app.MapPost("/api/tools/roster", (RosterEntry entry, IRosterStore rosterStore, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            IReadOnlyList<RosterEntry> entries = rosterStore.Upsert(owner, entry);
            return Results.Ok(new { count = entries.Count, entries });
        });

        return app;
    }
}
