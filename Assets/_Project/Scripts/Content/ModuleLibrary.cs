using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    /// <summary>Flagship modules. Generated from tools/gen_modules.py; edit the table, not this file.</summary>
    public static class ModuleLibrary
    {
        public static void AddAll(Dictionary<string, ModuleDefinition> result)
        {
            result["reinforced_ribs"] = new ModuleDefinition { id = "reinforced_ribs", nameKey = "module.reinforced_ribs", descriptionKey = "module.reinforced_ribs.desc", category = ModuleCategory.Hull, tier = 1, cost = 8, maxHull = 6 };
            result["ablative_plating"] = new ModuleDefinition { id = "ablative_plating", nameKey = "module.ablative_plating", descriptionKey = "module.ablative_plating.desc", category = ModuleCategory.Hull, tier = 2, cost = 14, maxArmor = 8 };
            result["ward_lattice"] = new ModuleDefinition { id = "ward_lattice", nameKey = "module.ward_lattice", descriptionKey = "module.ward_lattice.desc", category = ModuleCategory.Hull, tier = 2, cost = 14, maxWard = 6 };
            result["storm_keel"] = new ModuleDefinition { id = "storm_keel", nameKey = "module.storm_keel", descriptionKey = "module.storm_keel.desc", category = ModuleCategory.Hull, tier = 3, cost = 22, maxHull = 8, maxArmor = 6 };
            result["resonance_dampers"] = new ModuleDefinition { id = "resonance_dampers", nameKey = "module.resonance_dampers", descriptionKey = "module.resonance_dampers.desc", category = ModuleCategory.Core, tier = 1, cost = 8, instabilityDecay = 1.5f };
            result["aether_capacitor"] = new ModuleDefinition { id = "aether_capacitor", nameKey = "module.aether_capacitor", descriptionKey = "module.aether_capacitor.desc", category = ModuleCategory.Core, tier = 2, cost = 14, coreOutput = 1 };
            result["twin_core_bypass"] = new ModuleDefinition { id = "twin_core_bypass", nameKey = "module.twin_core_bypass", descriptionKey = "module.twin_core_bypass.desc", category = ModuleCategory.Core, tier = 3, cost = 22, coreOutput = 2 };
            result["ward_harmonizer"] = new ModuleDefinition { id = "ward_harmonizer", nameKey = "module.ward_harmonizer", descriptionKey = "module.ward_harmonizer.desc", category = ModuleCategory.Core, tier = 2, cost = 14, wardRegen = 1.35f };
            result["rifled_barrels"] = new ModuleDefinition { id = "rifled_barrels", nameKey = "module.rifled_barrels", descriptionKey = "module.rifled_barrels.desc", category = ModuleCategory.Weapons, tier = 1, cost = 8, weaponDamage = 1.12f };
            result["autoloader"] = new ModuleDefinition { id = "autoloader", nameKey = "module.autoloader", descriptionKey = "module.autoloader.desc", category = ModuleCategory.Weapons, tier = 2, cost = 14, weaponCooldown = 0.85f };
            result["aether_shells"] = new ModuleDefinition { id = "aether_shells", nameKey = "module.aether_shells", descriptionKey = "module.aether_shells.desc", category = ModuleCategory.Weapons, tier = 3, cost = 22, weaponDamage = 1.25f };
            result["gunnery_computer"] = new ModuleDefinition { id = "gunnery_computer", nameKey = "module.gunnery_computer", descriptionKey = "module.gunnery_computer.desc", category = ModuleCategory.Weapons, tier = 2, cost = 14, accuracy = 0.06f };
            result["extended_hangar"] = new ModuleDefinition { id = "extended_hangar", nameKey = "module.extended_hangar", descriptionKey = "module.extended_hangar.desc", category = ModuleCategory.Deck, tier = 1, cost = 8, squadronStrength = 1 };
            result["rapid_catapult"] = new ModuleDefinition { id = "rapid_catapult", nameKey = "module.rapid_catapult", descriptionKey = "module.rapid_catapult.desc", category = ModuleCategory.Deck, tier = 2, cost = 14, squadronTime = 0.8f };
            result["escort_doctrine"] = new ModuleDefinition { id = "escort_doctrine", nameKey = "module.escort_doctrine", descriptionKey = "module.escort_doctrine.desc", category = ModuleCategory.Deck, tier = 2, cost = 14, interceptCharges = 1 };
            result["veteran_pilots"] = new ModuleDefinition { id = "veteran_pilots", nameKey = "module.veteran_pilots", descriptionKey = "module.veteran_pilots.desc", category = ModuleCategory.Deck, tier = 3, cost = 22, squadronStrength = 1, squadronTime = 0.9f };
            result["long_range_array"] = new ModuleDefinition { id = "long_range_array", nameKey = "module.long_range_array", descriptionKey = "module.long_range_array.desc", category = ModuleCategory.Sensors, tier = 1, cost = 8, accuracy = 0.04f };
            result["storm_eyes"] = new ModuleDefinition { id = "storm_eyes", nameKey = "module.storm_eyes", descriptionKey = "module.storm_eyes.desc", category = ModuleCategory.Sensors, tier = 2, cost = 14, weatherResistance = true };
            result["recon_uplink"] = new ModuleDefinition { id = "recon_uplink", nameKey = "module.recon_uplink", descriptionKey = "module.recon_uplink.desc", category = ModuleCategory.Sensors, tier = 3, cost = 22, reconSeconds = 10 };
            result["damage_control_teams"] = new ModuleDefinition { id = "damage_control_teams", nameKey = "module.damage_control_teams", descriptionKey = "module.damage_control_teams.desc", category = ModuleCategory.Engineering, tier = 1, cost = 8, repairRate = 1.3f };
            result["fire_suppression"] = new ModuleDefinition { id = "fire_suppression", nameKey = "module.fire_suppression", descriptionKey = "module.fire_suppression.desc", category = ModuleCategory.Engineering, tier = 2, cost = 14, fireResistance = true };
            result["oxygen_reserves"] = new ModuleDefinition { id = "oxygen_reserves", nameKey = "module.oxygen_reserves", descriptionKey = "module.oxygen_reserves.desc", category = ModuleCategory.Engineering, tier = 1, cost = 8, oxygenReserve = true };
            result["auto_repair_drones"] = new ModuleDefinition { id = "auto_repair_drones", nameKey = "module.auto_repair_drones", descriptionKey = "module.auto_repair_drones.desc", category = ModuleCategory.Engineering, tier = 3, cost = 22, autoRepair = 0.5f };
            result["medical_bay_upgrade"] = new ModuleDefinition { id = "medical_bay_upgrade", nameKey = "module.medical_bay_upgrade", descriptionKey = "module.medical_bay_upgrade.desc", category = ModuleCategory.Engineering, tier = 2, cost = 14, healRate = 1.5f };
            result["navigator_charts"] = new ModuleDefinition { id = "navigator_charts", nameKey = "module.navigator_charts", descriptionKey = "module.navigator_charts.desc", category = ModuleCategory.Bridge, tier = 1, cost = 8, aetherDiscount = true };
            result["salvage_cranes"] = new ModuleDefinition { id = "salvage_cranes", nameKey = "module.salvage_cranes", descriptionKey = "module.salvage_cranes.desc", category = ModuleCategory.Bridge, tier = 2, cost = 14, salvageReward = 3 };
            result["salvage_refinery"] = new ModuleDefinition { id = "salvage_refinery", nameKey = "module.salvage_refinery", descriptionKey = "module.salvage_refinery.desc", category = ModuleCategory.Bridge, tier = 3, cost = 22, salvageReward = 6 };
            result["boarding_armory"] = new ModuleDefinition { id = "boarding_armory", nameKey = "module.boarding_armory", descriptionKey = "module.boarding_armory.desc", category = ModuleCategory.Marines, tier = 1, cost = 8, boardingDefense = 1.6f };
            result["marine_barracks"] = new ModuleDefinition { id = "marine_barracks", nameKey = "module.marine_barracks", descriptionKey = "module.marine_barracks.desc", category = ModuleCategory.Marines, tier = 2, cost = 14, boardingDefense = 1.3f, crewHealth = 10 };
            result["shock_troops"] = new ModuleDefinition { id = "shock_troops", nameKey = "module.shock_troops", descriptionKey = "module.shock_troops.desc", category = ModuleCategory.Marines, tier = 3, cost = 22, assaultBonus = 16 };
        }
    }
}
