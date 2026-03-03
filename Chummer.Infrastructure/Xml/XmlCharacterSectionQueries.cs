using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterSectionQueries : ICharacterSectionQueries
{
    private readonly ICharacterOverviewQueries _overviewQueries;
    private readonly ICharacterStatsQueries _statsQueries;
    private readonly ICharacterInventoryQueries _inventoryQueries;
    private readonly ICharacterMagicResonanceQueries _magicResonanceQueries;
    private readonly ICharacterSocialNarrativeQueries _socialNarrativeQueries;

    public XmlCharacterSectionQueries(ICharacterSectionService characterSectionService)
        : this(
            new XmlCharacterOverviewQueries(characterSectionService),
            new XmlCharacterStatsQueries(characterSectionService),
            new XmlCharacterInventoryQueries(characterSectionService),
            new XmlCharacterMagicResonanceQueries(characterSectionService),
            new XmlCharacterSocialNarrativeQueries(characterSectionService))
    {
    }

    public XmlCharacterSectionQueries(
        ICharacterOverviewQueries overviewQueries,
        ICharacterStatsQueries statsQueries,
        ICharacterInventoryQueries inventoryQueries,
        ICharacterMagicResonanceQueries magicResonanceQueries,
        ICharacterSocialNarrativeQueries socialNarrativeQueries)
    {
        _overviewQueries = overviewQueries;
        _statsQueries = statsQueries;
        _inventoryQueries = inventoryQueries;
        _magicResonanceQueries = magicResonanceQueries;
        _socialNarrativeQueries = socialNarrativeQueries;
    }

    public object ParseSection(string sectionId, CharacterXmlDocument document)
    {
        string key = (sectionId ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            "profile" => _overviewQueries.ParseProfile(document),
            "progress" => _overviewQueries.ParseProgress(document),
            "rules" => _overviewQueries.ParseRules(document),
            "build" => _overviewQueries.ParseBuild(document),
            "movement" => _overviewQueries.ParseMovement(document),
            "awakening" => _overviewQueries.ParseAwakening(document),
            "skills" => _overviewQueries.ParseSkills(document),

            "attributes" => _statsQueries.ParseAttributes(document),
            "attributedetails" => _statsQueries.ParseAttributeDetails(document),
            "limitmodifiers" => _statsQueries.ParseLimitModifiers(document),

            "inventory" => _inventoryQueries.ParseInventory(document.Xml),
            "gear" => _inventoryQueries.ParseGear(document.Xml),
            "weapons" => _inventoryQueries.ParseWeapons(document.Xml),
            "weaponaccessories" => _inventoryQueries.ParseWeaponAccessories(document.Xml),
            "armors" => _inventoryQueries.ParseArmors(document.Xml),
            "armormods" => _inventoryQueries.ParseArmorMods(document.Xml),
            "cyberwares" => _inventoryQueries.ParseCyberwares(document.Xml),
            "vehicles" => _inventoryQueries.ParseVehicles(document.Xml),
            "vehiclemods" => _inventoryQueries.ParseVehicleMods(document.Xml),
            "gearlocations" => _inventoryQueries.ParseGearLocations(document.Xml),
            "armorlocations" => _inventoryQueries.ParseArmorLocations(document.Xml),
            "weaponlocations" => _inventoryQueries.ParseWeaponLocations(document.Xml),
            "vehiclelocations" => _inventoryQueries.ParseVehicleLocations(document.Xml),
            "drugs" => _inventoryQueries.ParseDrugs(document.Xml),

            "spells" => _magicResonanceQueries.ParseSpells(document.Xml),
            "powers" => _magicResonanceQueries.ParsePowers(document.Xml),
            "complexforms" => _magicResonanceQueries.ParseComplexForms(document.Xml),
            "spirits" => _magicResonanceQueries.ParseSpirits(document.Xml),
            "foci" => _magicResonanceQueries.ParseFoci(document.Xml),
            "aiprograms" => _magicResonanceQueries.ParseAiPrograms(document.Xml),
            "martialarts" => _magicResonanceQueries.ParseMartialArts(document.Xml),
            "metamagics" => _magicResonanceQueries.ParseMetamagics(document.Xml),
            "arts" => _magicResonanceQueries.ParseArts(document.Xml),
            "initiationgrades" => _magicResonanceQueries.ParseInitiationGrades(document.Xml),
            "critterpowers" => _magicResonanceQueries.ParseCritterPowers(document.Xml),
            "mentorspirits" => _magicResonanceQueries.ParseMentorSpirits(document.Xml),

            "qualities" => _socialNarrativeQueries.ParseQualities(document.Xml),
            "contacts" => _socialNarrativeQueries.ParseContacts(document.Xml),
            "lifestyles" => _socialNarrativeQueries.ParseLifestyles(document.Xml),
            "sources" => _socialNarrativeQueries.ParseSources(document.Xml),
            "expenses" => _socialNarrativeQueries.ParseExpenses(document.Xml),
            "calendar" => _socialNarrativeQueries.ParseCalendar(document.Xml),
            "improvements" => _socialNarrativeQueries.ParseImprovements(document.Xml),
            "customdatadirectorynames" => _socialNarrativeQueries.ParseCustomDataDirectoryNames(document.Xml),

            _ => throw new InvalidOperationException($"Unsupported section '{sectionId}'.")
        };
    }
}
