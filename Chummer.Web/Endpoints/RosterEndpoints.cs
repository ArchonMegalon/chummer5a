using Chummer.Application.Tools;
using Chummer.Contracts.Api;

namespace Chummer.Web.Endpoints;

public static class RosterEndpoints
{
    public static IEndpointRouteBuilder MapRosterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tools/roster", (IRosterStore rosterStore) =>
        {
            IReadOnlyList<RosterEntry> entries = rosterStore.Load();
            return Results.Ok(new { count = entries.Count, entries });
        });

        app.MapPost("/api/tools/roster", (RosterEntry entry, IRosterStore rosterStore) =>
        {
            IReadOnlyList<RosterEntry> entries = rosterStore.Upsert(entry);
            return Results.Ok(new { count = entries.Count, entries });
        });

        return app;
    }
}
