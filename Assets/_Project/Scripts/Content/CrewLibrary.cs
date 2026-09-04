using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    public sealed class CrewRecruitDefinition
    {
        public string id;
        public string displayName;
        public string nameKey;
        public CrewLineage lineage;
        public CrewRole role;
        public ShipSystemType startingRoom;
        public string traitKey;
        public string backgroundKey;
        public int cost;
    }

    public static class CrewLibrary
    {
        private static readonly List<CrewRecruitDefinition> Definitions = new List<CrewRecruitDefinition>
        {
            Recruit("recruit_aela", "Aela", CrewLineage.Avian, CrewRole.Navigator, ShipSystemType.Sensors, "trait.light_step", "background.weather_scholar", 11),
            Recruit("recruit_durgan", "Durgan", CrewLineage.Dwarf, CrewRole.Gunner, ShipSystemType.Weapons, "trait.steadfast", "background.dockwright", 13),
            Recruit("recruit_kael", "Kael", CrewLineage.Human, CrewRole.Marine, ShipSystemType.Weapons, "trait.rescuer", "background.border_guard", 12),
            Recruit("recruit_nym", "Nym", CrewLineage.Elf, CrewRole.Medic, ShipSystemType.Infirmary, "trait.attuned", "background.cloud_medic", 13),
            Recruit("recruit_rix", "Rix", CrewLineage.Goblin, CrewRole.Engineer, ShipSystemType.Engines, "trait.quick_hands", "background.deck_runner", 11),
            Recruit("recruit_varka", "Varka", CrewLineage.Orc, CrewRole.Pilot, ShipSystemType.FlightDeck, "trait.fireproof", "background.exile", 14)
        };

        public static IEnumerable<CrewRecruitDefinition> All => Definitions;

        public static CrewRecruitDefinition Get(string id)
        {
            return Definitions.Find(definition => definition.id == id);
        }

        public static CrewRecruitDefinition Offer(int seed, int regionIndex, List<CrewState> crew)
        {
            var random = SeededRandom.Seed(unchecked(seed + regionIndex * 65537), 0xC2E7u);
            var start = SeededRandom.Range(ref random, 0, Definitions.Count);
            for (var offset = 0; offset < Definitions.Count; offset++)
            {
                var candidate = Definitions[(start + offset) % Definitions.Count];
                if (crew == null || !crew.Exists(member => member.id == candidate.id)) return candidate;
            }
            return null;
        }

        public static CrewState Create(string id)
        {
            var definition = Get(id);
            if (definition == null) return null;
            var health = LineageRules.Get(definition.lineage).maxHealth;
            return new CrewState
            {
                id = definition.id,
                displayName = definition.displayName,
                lineage = definition.lineage,
                role = definition.role,
                currentRoom = definition.startingRoom,
                health = health,
                maxHealth = health,
                traitKey = definition.traitKey,
                backgroundKey = definition.backgroundKey,
                skillLevel = 1
            };
        }

        private static CrewRecruitDefinition Recruit(string id, string displayName, CrewLineage lineage, CrewRole role, ShipSystemType room,
            string trait, string background, int cost)
        {
            return new CrewRecruitDefinition
            {
                id = id,
                displayName = displayName,
                nameKey = "crew." + id,
                lineage = lineage,
                role = role,
                startingRoom = room,
                traitKey = trait,
                backgroundKey = background,
                cost = cost
            };
        }
    }
}
