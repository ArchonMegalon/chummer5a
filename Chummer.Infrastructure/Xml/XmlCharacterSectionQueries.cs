using Chummer.Application.Characters;
using Chummer.Core.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterSectionQueries : ICharacterSectionQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterSectionQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public object ParseSection(string sectionId, string xml)
    {
        string key = (sectionId ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            "attributes" => _characterSectionService.ParseAttributes(xml),
            "attributedetails" => _characterSectionService.ParseAttributeDetails(xml),
            "inventory" => _characterSectionService.ParseInventory(xml),
            "profile" => _characterSectionService.ParseProfile(xml),
            "progress" => _characterSectionService.ParseProgress(xml),
            "rules" => _characterSectionService.ParseRules(xml),
            "build" => _characterSectionService.ParseBuild(xml),
            "movement" => _characterSectionService.ParseMovement(xml),
            "awakening" => _characterSectionService.ParseAwakening(xml),
            "gear" => _characterSectionService.ParseGear(xml),
            "weapons" => _characterSectionService.ParseWeapons(xml),
            "weaponaccessories" => _characterSectionService.ParseWeaponAccessories(xml),
            "armors" => _characterSectionService.ParseArmors(xml),
            "armormods" => _characterSectionService.ParseArmorMods(xml),
            "cyberwares" => _characterSectionService.ParseCyberwares(xml),
            "vehicles" => _characterSectionService.ParseVehicles(xml),
            "vehiclemods" => _characterSectionService.ParseVehicleMods(xml),
            "skills" => _characterSectionService.ParseSkills(xml),
            "qualities" => _characterSectionService.ParseQualities(xml),
            "contacts" => _characterSectionService.ParseContacts(xml),
            "spells" => _characterSectionService.ParseSpells(xml),
            "powers" => _characterSectionService.ParsePowers(xml),
            "complexforms" => _characterSectionService.ParseComplexForms(xml),
            "spirits" => _characterSectionService.ParseSpirits(xml),
            "foci" => _characterSectionService.ParseFoci(xml),
            "aiprograms" => _characterSectionService.ParseAiPrograms(xml),
            "martialarts" => _characterSectionService.ParseMartialArts(xml),
            "limitmodifiers" => _characterSectionService.ParseLimitModifiers(xml),
            "lifestyles" => _characterSectionService.ParseLifestyles(xml),
            "metamagics" => _characterSectionService.ParseMetamagics(xml),
            "arts" => _characterSectionService.ParseArts(xml),
            "initiationgrades" => _characterSectionService.ParseInitiationGrades(xml),
            "critterpowers" => _characterSectionService.ParseCritterPowers(xml),
            "mentorspirits" => _characterSectionService.ParseMentorSpirits(xml),
            "expenses" => _characterSectionService.ParseExpenses(xml),
            "sources" => _characterSectionService.ParseSources(xml),
            "gearlocations" => _characterSectionService.ParseGearLocations(xml),
            "armorlocations" => _characterSectionService.ParseArmorLocations(xml),
            "weaponlocations" => _characterSectionService.ParseWeaponLocations(xml),
            "vehiclelocations" => _characterSectionService.ParseVehicleLocations(xml),
            "calendar" => _characterSectionService.ParseCalendar(xml),
            "improvements" => _characterSectionService.ParseImprovements(xml),
            "customdatadirectorynames" => _characterSectionService.ParseCustomDataDirectoryNames(xml),
            "drugs" => _characterSectionService.ParseDrugs(xml),
            _ => throw new InvalidOperationException($"Unsupported section '{sectionId}'.")
        };
    }
}
