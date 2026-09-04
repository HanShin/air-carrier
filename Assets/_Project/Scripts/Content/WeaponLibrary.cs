using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    /// <summary>Mounted weapons. Generated from tools/gen_weapons.py; edit the table, not this file.</summary>
    public static class WeaponLibrary
    {
        public static void AddAll(Dictionary<string, WeaponDefinition> result)
        {
            result["aether_cannon"] = new WeaponDefinition { id = "aether_cannon", nameKey = "weapon.aether_cannon", descriptionKey = "weapon.aether_cannon.desc", family = WeaponFamily.Cannon, tier = 1, cost = 10, powerCost = 2, damage = 4.6f, cooldown = 4.0f };
            result["heavy_cannon"] = new WeaponDefinition { id = "heavy_cannon", nameKey = "weapon.heavy_cannon", descriptionKey = "weapon.heavy_cannon.desc", family = WeaponFamily.Cannon, tier = 2, cost = 18, powerCost = 2, damage = 6.4f, cooldown = 5.6f };
            result["siege_cannon"] = new WeaponDefinition { id = "siege_cannon", nameKey = "weapon.siege_cannon", descriptionKey = "weapon.siege_cannon.desc", family = WeaponFamily.Cannon, tier = 3, cost = 28, powerCost = 3, damage = 9.5f, cooldown = 6.4f, systemDamageMultiplier = 1.5f };
            result["ward_lance"] = new WeaponDefinition { id = "ward_lance", nameKey = "weapon.ward_lance", descriptionKey = "weapon.ward_lance.desc", family = WeaponFamily.Lance, tier = 1, cost = 10, powerCost = 1, damage = 3.0f, cooldown = 3.4f, wardMultiplier = 2.0f };
            result["resonance_lance"] = new WeaponDefinition { id = "resonance_lance", nameKey = "weapon.resonance_lance", descriptionKey = "weapon.resonance_lance.desc", family = WeaponFamily.Lance, tier = 2, cost = 18, powerCost = 2, damage = 4.4f, cooldown = 3.6f, wardMultiplier = 2.0f, accuracyBonus = 0.06f };
            result["sky_lance"] = new WeaponDefinition { id = "sky_lance", nameKey = "weapon.sky_lance", descriptionKey = "weapon.sky_lance.desc", family = WeaponFamily.Lance, tier = 3, cost = 28, powerCost = 2, damage = 5.8f, cooldown = 3.8f, wardMultiplier = 2.5f };
            result["bolt_thrower"] = new WeaponDefinition { id = "bolt_thrower", nameKey = "weapon.bolt_thrower", descriptionKey = "weapon.bolt_thrower.desc", family = WeaponFamily.Piercer, tier = 1, cost = 10, powerCost = 1, damage = 3.4f, cooldown = 4.2f, armorPiercing = 0.6f, wardMultiplier = 0.5f };
            result["rail_harpoon"] = new WeaponDefinition { id = "rail_harpoon", nameKey = "weapon.rail_harpoon", descriptionKey = "weapon.rail_harpoon.desc", family = WeaponFamily.Piercer, tier = 2, cost = 18, powerCost = 2, damage = 5.2f, cooldown = 4.6f, armorPiercing = 0.7f, wardMultiplier = 0.5f };
            result["gate_piercer"] = new WeaponDefinition { id = "gate_piercer", nameKey = "weapon.gate_piercer", descriptionKey = "weapon.gate_piercer.desc", family = WeaponFamily.Piercer, tier = 3, cost = 28, powerCost = 3, damage = 7.4f, cooldown = 5.0f, armorPiercing = 0.8f, wardMultiplier = 0.5f, breachChance = 0.3f };
            result["rocket_pod"] = new WeaponDefinition { id = "rocket_pod", nameKey = "weapon.rocket_pod", descriptionKey = "weapon.rocket_pod.desc", family = WeaponFamily.Missile, tier = 1, cost = 10, powerCost = 1, damage = 6.0f, cooldown = 6.0f, ignoresWard = true, ordnancePerShot = 1, fireChance = 0.3f };
            result["storm_missiles"] = new WeaponDefinition { id = "storm_missiles", nameKey = "weapon.storm_missiles", descriptionKey = "weapon.storm_missiles.desc", family = WeaponFamily.Missile, tier = 2, cost = 18, powerCost = 1, damage = 8.5f, cooldown = 6.5f, ignoresWard = true, ordnancePerShot = 1, fireChance = 0.45f };
            result["ruin_missiles"] = new WeaponDefinition { id = "ruin_missiles", nameKey = "weapon.ruin_missiles", descriptionKey = "weapon.ruin_missiles.desc", family = WeaponFamily.Missile, tier = 3, cost = 28, powerCost = 2, damage = 12.0f, cooldown = 7.5f, ignoresWard = true, ordnancePerShot = 2, fireChance = 0.4f, systemDamageMultiplier = 1.5f };
            result["flak_battery"] = new WeaponDefinition { id = "flak_battery", nameKey = "weapon.flak_battery", descriptionKey = "weapon.flak_battery.desc", family = WeaponFamily.Flak, tier = 1, cost = 10, powerCost = 1, damage = 1.6f, cooldown = 5.0f, interceptCharge = 1 };
            result["flak_curtain"] = new WeaponDefinition { id = "flak_curtain", nameKey = "weapon.flak_curtain", descriptionKey = "weapon.flak_curtain.desc", family = WeaponFamily.Flak, tier = 2, cost = 18, powerCost = 2, damage = 2.6f, cooldown = 4.5f, interceptCharge = 1, accuracyBonus = 0.08f };
            result["ember_mortar"] = new WeaponDefinition { id = "ember_mortar", nameKey = "weapon.ember_mortar", descriptionKey = "weapon.ember_mortar.desc", family = WeaponFamily.Incendiary, tier = 1, cost = 10, powerCost = 1, damage = 2.4f, cooldown = 4.4f, fireChance = 0.8f, systemDamageMultiplier = 1.3f };
            result["hellfire_mortar"] = new WeaponDefinition { id = "hellfire_mortar", nameKey = "weapon.hellfire_mortar", descriptionKey = "weapon.hellfire_mortar.desc", family = WeaponFamily.Incendiary, tier = 3, cost = 28, powerCost = 2, damage = 4.2f, cooldown = 4.8f, fireChance = 1.0f, systemDamageMultiplier = 1.6f };
            result["breacher_charges"] = new WeaponDefinition { id = "breacher_charges", nameKey = "weapon.breacher_charges", descriptionKey = "weapon.breacher_charges.desc", family = WeaponFamily.Breacher, tier = 1, cost = 10, powerCost = 1, damage = 2.8f, cooldown = 4.6f, breachChance = 0.7f };
            result["hull_ripper"] = new WeaponDefinition { id = "hull_ripper", nameKey = "weapon.hull_ripper", descriptionKey = "weapon.hull_ripper.desc", family = WeaponFamily.Breacher, tier = 2, cost = 18, powerCost = 2, damage = 4.6f, cooldown = 5.2f, breachChance = 0.8f, armorPiercing = 0.4f };
        }
    }
}
