using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterInventoryQueries
{
    CharacterInventorySection ParseInventory(string xml);

    CharacterGearSection ParseGear(string xml);

    CharacterWeaponsSection ParseWeapons(string xml);

    CharacterWeaponAccessoriesSection ParseWeaponAccessories(string xml);

    CharacterArmorsSection ParseArmors(string xml);

    CharacterArmorModsSection ParseArmorMods(string xml);

    CharacterCyberwaresSection ParseCyberwares(string xml);

    CharacterVehiclesSection ParseVehicles(string xml);

    CharacterVehicleModsSection ParseVehicleMods(string xml);

    CharacterLocationsSection ParseGearLocations(string xml);

    CharacterLocationsSection ParseArmorLocations(string xml);

    CharacterLocationsSection ParseWeaponLocations(string xml);

    CharacterLocationsSection ParseVehicleLocations(string xml);

    CharacterDrugsSection ParseDrugs(string xml);
}
