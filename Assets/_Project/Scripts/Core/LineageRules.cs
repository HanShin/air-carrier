using System;
using System.Collections.Generic;

namespace AetherArk.Core
{
    /// <summary>Authored racial identity shared by simulation, setup UI and balance tests.</summary>
    public sealed class LineageDefinition
    {
        public CrewLineage lineage;
        public string descriptionKey;
        public float maxHealth = 100f;
        public float repairMultiplier = 1f;
        public float hazardDamageMultiplier = 1f;
        public float oxygenDamageMultiplier = 1f;
        public float boardingMultiplier = 1f;
        public float rescueWindow = 12f;
        public float sortieTimeMultiplier = 1f;
        public float sortieLossMultiplier = 1f;
        public float overchargeInstabilityMultiplier = 1f;
        public float overchargeAccidentMultiplier = 1f;
        public int captainMorale;
        public int captainSurvivors;
        public int captainAether;
        public int captainSupplies;
        public int captainOrdnance;
        public int captainSalvage;
        public float captainHull;
        public float captainArmor;
    }

    public static class LineageRules
    {
        private static readonly Dictionary<CrewLineage, LineageDefinition> Definitions =
            new Dictionary<CrewLineage, LineageDefinition>
            {
                { CrewLineage.Human, new LineageDefinition
                    { lineage = CrewLineage.Human, descriptionKey = "lineage.human.desc", repairMultiplier = 1.08f, boardingMultiplier = 1.08f, captainMorale = 8 } },
                { CrewLineage.Elf, new LineageDefinition
                    { lineage = CrewLineage.Elf, descriptionKey = "lineage.elf.desc", maxHealth = 80f, overchargeInstabilityMultiplier = 0.65f, overchargeAccidentMultiplier = 0.55f, captainAether = 2 } },
                { CrewLineage.Dwarf, new LineageDefinition
                    { lineage = CrewLineage.Dwarf, descriptionKey = "lineage.dwarf.desc", maxHealth = 110f, repairMultiplier = 1.4f, hazardDamageMultiplier = 0.72f, captainArmor = 4f } },
                { CrewLineage.Orc, new LineageDefinition
                    { lineage = CrewLineage.Orc, descriptionKey = "lineage.orc.desc", maxHealth = 125f, boardingMultiplier = 1.55f, rescueWindow = 16f, captainHull = 4f, captainSurvivors = 25 } },
                { CrewLineage.Goblin, new LineageDefinition
                    { lineage = CrewLineage.Goblin, descriptionKey = "lineage.goblin.desc", maxHealth = 90f, repairMultiplier = 1.22f, sortieTimeMultiplier = 0.82f, captainOrdnance = 2, captainSalvage = 4 } },
                { CrewLineage.Avian, new LineageDefinition
                    { lineage = CrewLineage.Avian, descriptionKey = "lineage.avian.desc", maxHealth = 88f, oxygenDamageMultiplier = 0.35f, sortieLossMultiplier = 0.7f, captainAether = 1, captainSupplies = 2 } }
            };

        public static LineageDefinition Get(CrewLineage lineage)
        {
            return Definitions.TryGetValue(lineage, out var definition) ? definition : Definitions[CrewLineage.Human];
        }

        public static IEnumerable<LineageDefinition> All()
        {
            foreach (CrewLineage lineage in Enum.GetValues(typeof(CrewLineage))) yield return Get(lineage);
        }

        public static void ApplyCaptainDoctrine(RunState state, CrewLineage lineage)
        {
            if (state == null || state.playerShip == null) return;
            var rule = Get(lineage);
            state.convoy.morale = Math.Min(100, state.convoy.morale + rule.captainMorale);
            state.convoy.survivors += rule.captainSurvivors;
            state.resources.aether += rule.captainAether;
            state.resources.supplies += rule.captainSupplies;
            state.resources.ordnance += rule.captainOrdnance;
            state.resources.salvage += rule.captainSalvage;
            state.playerShip.maxHull += rule.captainHull;
            state.playerShip.hull += rule.captainHull;
            state.playerShip.maxArmor += rule.captainArmor;
            state.playerShip.armor += rule.captainArmor;
        }
    }
}
