using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
using Chummer.Core.Characters;

namespace Chummer.Web.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/characters/summary", (ICharacterFileService characterFileService, CharacterXmlRequest request) =>
        {
            var summary = characterFileService.ParseSummaryFromXml(request.Xml);
            return Results.Ok(summary);
        });

        app.MapPost("/api/characters/validate", (ICharacterFileService characterFileService, CharacterXmlRequest request) =>
        {
            var validation = characterFileService.ValidateXml(request.Xml);
            return Results.Ok(validation);
        });

        app.MapPost("/api/characters/metadata", (ICharacterFileService characterFileService, CharacterMetadataRequest request) =>
        {
            CharacterMetadataUpdate update = new(
                Name: request.Name,
                Alias: request.Alias,
                Notes: request.Notes);
            string updatedXml = characterFileService.ApplyMetadataUpdate(request.Xml, update);
            var summary = characterFileService.ParseSummaryFromXml(updatedXml);
            return Results.Ok(new { updatedXml, summary });
        });

        MapSection(app, "attributes", static (service, xml) => service.ParseAttributes(xml));
        MapSection(app, "attributedetails", static (service, xml) => service.ParseAttributeDetails(xml));
        MapSection(app, "inventory", static (service, xml) => service.ParseInventory(xml));
        MapSection(app, "profile", static (service, xml) => service.ParseProfile(xml));
        MapSection(app, "progress", static (service, xml) => service.ParseProgress(xml));
        MapSection(app, "rules", static (service, xml) => service.ParseRules(xml));
        MapSection(app, "build", static (service, xml) => service.ParseBuild(xml));
        MapSection(app, "movement", static (service, xml) => service.ParseMovement(xml));
        MapSection(app, "awakening", static (service, xml) => service.ParseAwakening(xml));
        MapSection(app, "gear", static (service, xml) => service.ParseGear(xml));
        MapSection(app, "weapons", static (service, xml) => service.ParseWeapons(xml));
        MapSection(app, "weaponaccessories", static (service, xml) => service.ParseWeaponAccessories(xml));
        MapSection(app, "armors", static (service, xml) => service.ParseArmors(xml));
        MapSection(app, "armormods", static (service, xml) => service.ParseArmorMods(xml));
        MapSection(app, "cyberwares", static (service, xml) => service.ParseCyberwares(xml));
        MapSection(app, "vehicles", static (service, xml) => service.ParseVehicles(xml));
        MapSection(app, "vehiclemods", static (service, xml) => service.ParseVehicleMods(xml));
        MapSection(app, "skills", static (service, xml) => service.ParseSkills(xml));
        MapSection(app, "qualities", static (service, xml) => service.ParseQualities(xml));
        MapSection(app, "contacts", static (service, xml) => service.ParseContacts(xml));
        MapSection(app, "spells", static (service, xml) => service.ParseSpells(xml));
        MapSection(app, "powers", static (service, xml) => service.ParsePowers(xml));
        MapSection(app, "complexforms", static (service, xml) => service.ParseComplexForms(xml));
        MapSection(app, "spirits", static (service, xml) => service.ParseSpirits(xml));
        MapSection(app, "foci", static (service, xml) => service.ParseFoci(xml));
        MapSection(app, "aiprograms", static (service, xml) => service.ParseAiPrograms(xml));
        MapSection(app, "martialarts", static (service, xml) => service.ParseMartialArts(xml));
        MapSection(app, "limitmodifiers", static (service, xml) => service.ParseLimitModifiers(xml));
        MapSection(app, "lifestyles", static (service, xml) => service.ParseLifestyles(xml));
        MapSection(app, "metamagics", static (service, xml) => service.ParseMetamagics(xml));
        MapSection(app, "arts", static (service, xml) => service.ParseArts(xml));
        MapSection(app, "initiationgrades", static (service, xml) => service.ParseInitiationGrades(xml));
        MapSection(app, "critterpowers", static (service, xml) => service.ParseCritterPowers(xml));
        MapSection(app, "mentorspirits", static (service, xml) => service.ParseMentorSpirits(xml));
        MapSection(app, "expenses", static (service, xml) => service.ParseExpenses(xml));
        MapSection(app, "sources", static (service, xml) => service.ParseSources(xml));
        MapSection(app, "gearlocations", static (service, xml) => service.ParseGearLocations(xml));
        MapSection(app, "armorlocations", static (service, xml) => service.ParseArmorLocations(xml));
        MapSection(app, "weaponlocations", static (service, xml) => service.ParseWeaponLocations(xml));
        MapSection(app, "vehiclelocations", static (service, xml) => service.ParseVehicleLocations(xml));
        MapSection(app, "calendar", static (service, xml) => service.ParseCalendar(xml));
        MapSection(app, "improvements", static (service, xml) => service.ParseImprovements(xml));
        MapSection(app, "customdatadirectorynames", static (service, xml) => service.ParseCustomDataDirectoryNames(xml));
        MapSection(app, "drugs", static (service, xml) => service.ParseDrugs(xml));

        return app;
    }

    private static void MapSection<TSection>(
        IEndpointRouteBuilder app,
        string route,
        Func<ICharacterSectionService, string, TSection> parser)
    {
        app.MapPost($"/api/characters/sections/{route}", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
        {
            TSection section = parser(characterSectionService, request.Xml);
            return Results.Ok(section);
        });
    }
}
