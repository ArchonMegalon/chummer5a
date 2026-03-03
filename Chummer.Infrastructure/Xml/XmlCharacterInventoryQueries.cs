using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterInventoryQueries : ICharacterInventoryQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterInventoryQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterInventorySection ParseInventory(CharacterXmlDocument document) => _characterSectionService.ParseInventory(document.Xml);

    public CharacterGearSection ParseGear(CharacterXmlDocument document) => _characterSectionService.ParseGear(document.Xml);

    public CharacterWeaponsSection ParseWeapons(CharacterXmlDocument document) => _characterSectionService.ParseWeapons(document.Xml);

    public CharacterWeaponAccessoriesSection ParseWeaponAccessories(CharacterXmlDocument document) => _characterSectionService.ParseWeaponAccessories(document.Xml);

    public CharacterArmorsSection ParseArmors(CharacterXmlDocument document) => _characterSectionService.ParseArmors(document.Xml);

    public CharacterArmorModsSection ParseArmorMods(CharacterXmlDocument document) => _characterSectionService.ParseArmorMods(document.Xml);

    public CharacterCyberwaresSection ParseCyberwares(CharacterXmlDocument document) => _characterSectionService.ParseCyberwares(document.Xml);

    public CharacterVehiclesSection ParseVehicles(CharacterXmlDocument document) => _characterSectionService.ParseVehicles(document.Xml);

    public CharacterVehicleModsSection ParseVehicleMods(CharacterXmlDocument document) => _characterSectionService.ParseVehicleMods(document.Xml);

    public CharacterLocationsSection ParseGearLocations(CharacterXmlDocument document) => _characterSectionService.ParseGearLocations(document.Xml);

    public CharacterLocationsSection ParseArmorLocations(CharacterXmlDocument document) => _characterSectionService.ParseArmorLocations(document.Xml);

    public CharacterLocationsSection ParseWeaponLocations(CharacterXmlDocument document) => _characterSectionService.ParseWeaponLocations(document.Xml);

    public CharacterLocationsSection ParseVehicleLocations(CharacterXmlDocument document) => _characterSectionService.ParseVehicleLocations(document.Xml);

    public CharacterDrugsSection ParseDrugs(CharacterXmlDocument document) => _characterSectionService.ParseDrugs(document.Xml);
}
