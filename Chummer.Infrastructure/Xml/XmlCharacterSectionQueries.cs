using Chummer.Application.Characters;

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

    public object ParseSection(string sectionId, string xml)
    {
        string key = (sectionId ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            "profile" => _overviewQueries.ParseProfile(xml),
            "progress" => _overviewQueries.ParseProgress(xml),
            "rules" => _overviewQueries.ParseRules(xml),
            "build" => _overviewQueries.ParseBuild(xml),
            "movement" => _overviewQueries.ParseMovement(xml),
            "awakening" => _overviewQueries.ParseAwakening(xml),
            "skills" => _overviewQueries.ParseSkills(xml),

            "attributes" => _statsQueries.ParseAttributes(xml),
            "attributedetails" => _statsQueries.ParseAttributeDetails(xml),
            "limitmodifiers" => _statsQueries.ParseLimitModifiers(xml),

            "inventory" => _inventoryQueries.ParseInventory(xml),
            "gear" => _inventoryQueries.ParseGear(xml),
            "weapons" => _inventoryQueries.ParseWeapons(xml),
            "weaponaccessories" => _inventoryQueries.ParseWeaponAccessories(xml),
            "armors" => _inventoryQueries.ParseArmors(xml),
            "armormods" => _inventoryQueries.ParseArmorMods(xml),
            "cyberwares" => _inventoryQueries.ParseCyberwares(xml),
            "vehicles" => _inventoryQueries.ParseVehicles(xml),
            "vehiclemods" => _inventoryQueries.ParseVehicleMods(xml),
            "gearlocations" => _inventoryQueries.ParseGearLocations(xml),
            "armorlocations" => _inventoryQueries.ParseArmorLocations(xml),
            "weaponlocations" => _inventoryQueries.ParseWeaponLocations(xml),
            "vehiclelocations" => _inventoryQueries.ParseVehicleLocations(xml),
            "drugs" => _inventoryQueries.ParseDrugs(xml),

            "spells" => _magicResonanceQueries.ParseSpells(xml),
            "powers" => _magicResonanceQueries.ParsePowers(xml),
            "complexforms" => _magicResonanceQueries.ParseComplexForms(xml),
            "spirits" => _magicResonanceQueries.ParseSpirits(xml),
            "foci" => _magicResonanceQueries.ParseFoci(xml),
            "aiprograms" => _magicResonanceQueries.ParseAiPrograms(xml),
            "martialarts" => _magicResonanceQueries.ParseMartialArts(xml),
            "metamagics" => _magicResonanceQueries.ParseMetamagics(xml),
            "arts" => _magicResonanceQueries.ParseArts(xml),
            "initiationgrades" => _magicResonanceQueries.ParseInitiationGrades(xml),
            "critterpowers" => _magicResonanceQueries.ParseCritterPowers(xml),
            "mentorspirits" => _magicResonanceQueries.ParseMentorSpirits(xml),

            "qualities" => _socialNarrativeQueries.ParseQualities(xml),
            "contacts" => _socialNarrativeQueries.ParseContacts(xml),
            "lifestyles" => _socialNarrativeQueries.ParseLifestyles(xml),
            "sources" => _socialNarrativeQueries.ParseSources(xml),
            "expenses" => _socialNarrativeQueries.ParseExpenses(xml),
            "calendar" => _socialNarrativeQueries.ParseCalendar(xml),
            "improvements" => _socialNarrativeQueries.ParseImprovements(xml),
            "customdatadirectorynames" => _socialNarrativeQueries.ParseCustomDataDirectoryNames(xml),

            _ => throw new InvalidOperationException($"Unsupported section '{sectionId}'.")
        };
    }
}
