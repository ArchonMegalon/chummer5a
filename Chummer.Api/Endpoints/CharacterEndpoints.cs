using Chummer.Application.Characters;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;

namespace Chummer.Api.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/characters/summary", (ICharacterFileQueries characterFileQueries, CharacterXmlRequest request) =>
        {
            var summary = characterFileQueries.ParseSummary(new CharacterXmlDocument(request.Xml));
            return Results.Ok(summary);
        });

        app.MapPost("/api/characters/validate", (ICharacterFileQueries characterFileQueries, CharacterXmlRequest request) =>
        {
            var validation = characterFileQueries.Validate(new CharacterXmlDocument(request.Xml));
            return Results.Ok(validation);
        });

        app.MapPost("/api/characters/metadata", (ICharacterMetadataCommands characterMetadataCommands, CharacterMetadataRequest request) =>
        {
            UpdateCharacterMetadataCommand command = new(
                Xml: request.Xml,
                Name: request.Name,
                Alias: request.Alias,
                Notes: request.Notes);
            UpdateCharacterMetadataResult result = characterMetadataCommands.UpdateMetadata(command);
            return Results.Ok(new { updatedXml = result.UpdatedXml, summary = result.Summary });
        });

        MapSection(app, "attributes");
        MapSection(app, "attributedetails");
        MapSection(app, "inventory");
        MapSection(app, "profile");
        MapSection(app, "progress");
        MapSection(app, "rules");
        MapSection(app, "build");
        MapSection(app, "movement");
        MapSection(app, "awakening");
        MapSection(app, "gear");
        MapSection(app, "weapons");
        MapSection(app, "weaponaccessories");
        MapSection(app, "armors");
        MapSection(app, "armormods");
        MapSection(app, "cyberwares");
        MapSection(app, "vehicles");
        MapSection(app, "vehiclemods");
        MapSection(app, "skills");
        MapSection(app, "qualities");
        MapSection(app, "contacts");
        MapSection(app, "spells");
        MapSection(app, "powers");
        MapSection(app, "complexforms");
        MapSection(app, "spirits");
        MapSection(app, "foci");
        MapSection(app, "aiprograms");
        MapSection(app, "martialarts");
        MapSection(app, "limitmodifiers");
        MapSection(app, "lifestyles");
        MapSection(app, "metamagics");
        MapSection(app, "arts");
        MapSection(app, "initiationgrades");
        MapSection(app, "critterpowers");
        MapSection(app, "mentorspirits");
        MapSection(app, "expenses");
        MapSection(app, "sources");
        MapSection(app, "gearlocations");
        MapSection(app, "armorlocations");
        MapSection(app, "weaponlocations");
        MapSection(app, "vehiclelocations");
        MapSection(app, "calendar");
        MapSection(app, "improvements");
        MapSection(app, "customdatadirectorynames");
        MapSection(app, "drugs");

        return app;
    }

    private static void MapSection(
        IEndpointRouteBuilder app,
        string route)
    {
        app.MapPost($"/api/characters/sections/{route}", (ICharacterSectionQueries characterSectionQueries, CharacterXmlRequest request) =>
        {
            object section = characterSectionQueries.ParseSection(route, request.Xml);
            return Results.Ok(section);
        });
    }
}
