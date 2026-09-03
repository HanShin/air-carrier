using System.Collections.Generic;
using AetherArk.Content;

namespace AetherArk.Core
{
    /// <summary>Aggregated effect of every installed module. Multipliers multiply, flats add.</summary>
    public struct ModuleModifiers
    {
        public float weaponDamage;
        public float weaponCooldown;
        public float accuracy;
        public bool weatherResistance;
        public float wardRegen;
        public float repairRate;
        public float healRate;
        public float autoRepair;
        public bool fireResistance;
        public bool oxygenReserve;
        public float squadronTime;
        public int interceptCharges;
        public float reconSeconds;
        public int salvageReward;
        public bool aetherDiscount;
        public float boardingDefense;
        public float assaultBonus;
        public float instabilityDecay;

        public static ModuleModifiers None => new ModuleModifiers
        {
            weaponDamage = 1f, weaponCooldown = 1f, wardRegen = 1f, repairRate = 1f, healRate = 1f,
            squadronTime = 1f, boardingDefense = 1f, instabilityDecay = 1f
        };
    }

    public static class ModuleRules
    {
        public static ModuleModifiers Modifiers(RunState state)
        {
            var result = ModuleModifiers.None;
            if (state?.installedModules == null) return result;
            for (var i = 0; i < state.installedModules.Count; i++)
            {
                var module = ContentCatalog.GetModule(state.installedModules[i]);
                if (module == null) continue;
                result.weaponDamage *= module.weaponDamage;
                result.weaponCooldown *= module.weaponCooldown;
                result.accuracy += module.accuracy;
                result.weatherResistance |= module.weatherResistance;
                result.wardRegen *= module.wardRegen;
                result.repairRate *= module.repairRate;
                result.healRate *= module.healRate;
                result.autoRepair += module.autoRepair;
                result.fireResistance |= module.fireResistance;
                result.oxygenReserve |= module.oxygenReserve;
                result.squadronTime *= module.squadronTime;
                result.interceptCharges += module.interceptCharges;
                result.reconSeconds += module.reconSeconds;
                result.salvageReward += module.salvageReward;
                result.aetherDiscount |= module.aetherDiscount;
                result.boardingDefense *= module.boardingDefense;
                result.assaultBonus += module.assaultBonus;
                result.instabilityDecay *= module.instabilityDecay;
            }
            return result;
        }

        public static bool HasAnyEffect(ModuleDefinition module)
        {
            if (module == null) return false;
            return module.maxHull != 0f || module.maxArmor != 0f || module.maxWard != 0f || module.coreOutput != 0 || module.squadronStrength != 0
                   || module.crewHealth != 0f || module.weaponDamage != 1f || module.weaponCooldown != 1f || module.accuracy != 0f || module.weatherResistance
                   || module.wardRegen != 1f || module.repairRate != 1f || module.healRate != 1f || module.autoRepair != 0f || module.fireResistance
                   || module.oxygenReserve || module.squadronTime != 1f || module.interceptCharges != 0 || module.reconSeconds != 0f
                   || module.salvageReward != 0 || module.aetherDiscount || module.boardingDefense != 1f || module.assaultBonus != 0f || module.instabilityDecay != 1f;
        }

        /// <summary>Applies a module's flat bonuses to the run (called once on install) and refills the grown capacity.</summary>
        public static void ApplyFlatBonuses(RunState state, ModuleDefinition module)
        {
            var ship = state.playerShip;
            ship.maxHull += module.maxHull; ship.hull += module.maxHull;
            ship.maxArmor += module.maxArmor; ship.armor += module.maxArmor;
            ship.maxWard += module.maxWard; ship.ward += module.maxWard;
            ship.coreOutput += module.coreOutput;
            for (var i = 0; i < state.squadrons.Count; i++)
            {
                state.squadrons[i].maxStrength += module.squadronStrength;
                state.squadrons[i].strength = state.squadrons[i].maxStrength;
            }
            for (var i = 0; i < state.crew.Count; i++)
            {
                if (state.crew[i].isDead) continue;
                state.crew[i].maxHealth += module.crewHealth;
                state.crew[i].health += module.crewHealth;
            }
        }
    }
}
