using Chummer.Application.Characters;
using Chummer.Contracts.Characters;
using Chummer.Core.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterInventoryQueries : ICharacterInventoryQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterInventoryQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterInventorySection ParseInventory(string xml) => _characterSectionService.ParseInventory(xml);

    public CharacterGearSection ParseGear(string xml) => _characterSectionService.ParseGear(xml);

    public CharacterWeaponsSection ParseWeapons(string xml) => _characterSectionService.ParseWeapons(xml);

    public CharacterWeaponAccessoriesSection ParseWeaponAccessories(string xml) => _characterSectionService.ParseWeaponAccessories(xml);

    public CharacterArmorsSection ParseArmors(string xml) => _characterSectionService.ParseArmors(xml);

    public CharacterArmorModsSection ParseArmorMods(string xml) => _characterSectionService.ParseArmorMods(xml);

    public CharacterCyberwaresSection ParseCyberwares(string xml) => _characterSectionService.ParseCyberwares(xml);

    public CharacterVehiclesSection ParseVehicles(string xml) => _characterSectionService.ParseVehicles(xml);

    public CharacterVehicleModsSection ParseVehicleMods(string xml) => _characterSectionService.ParseVehicleMods(xml);

    public CharacterLocationsSection ParseGearLocations(string xml) => _characterSectionService.ParseGearLocations(xml);

    public CharacterLocationsSection ParseArmorLocations(string xml) => _characterSectionService.ParseArmorLocations(xml);

    public CharacterLocationsSection ParseWeaponLocations(string xml) => _characterSectionService.ParseWeaponLocations(xml);

    public CharacterLocationsSection ParseVehicleLocations(string xml) => _characterSectionService.ParseVehicleLocations(xml);

    public CharacterDrugsSection ParseDrugs(string xml) => _characterSectionService.ParseDrugs(xml);
}
