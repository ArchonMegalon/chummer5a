using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterInventoryQueries
{
    CharacterInventorySection ParseInventory(CharacterXmlDocument document);

    CharacterGearSection ParseGear(CharacterXmlDocument document);

    CharacterWeaponsSection ParseWeapons(CharacterXmlDocument document);

    CharacterWeaponAccessoriesSection ParseWeaponAccessories(CharacterXmlDocument document);

    CharacterArmorsSection ParseArmors(CharacterXmlDocument document);

    CharacterArmorModsSection ParseArmorMods(CharacterXmlDocument document);

    CharacterCyberwaresSection ParseCyberwares(CharacterXmlDocument document);

    CharacterVehiclesSection ParseVehicles(CharacterXmlDocument document);

    CharacterVehicleModsSection ParseVehicleMods(CharacterXmlDocument document);

    CharacterLocationsSection ParseGearLocations(CharacterXmlDocument document);

    CharacterLocationsSection ParseArmorLocations(CharacterXmlDocument document);

    CharacterLocationsSection ParseWeaponLocations(CharacterXmlDocument document);

    CharacterLocationsSection ParseVehicleLocations(CharacterXmlDocument document);

    CharacterDrugsSection ParseDrugs(CharacterXmlDocument document);
}
