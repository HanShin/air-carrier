using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    /// <summary>Authored events beyond the five baseline encounters. Generated from tools/gen_events.py data.</summary>
    public static class EncounterLibrary
    {
        public static void AddAll(Dictionary<string, EncounterDefinition> result)
        {
            Event(result, "burning_ferry", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "teams", suppliesCost = 1, survivorDelta = 40, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "dash", survivorDelta = 70, moraleDelta = 6, successChance = 0.6f, failureChoiceId = "dash_fail" },
                new EncounterChoiceDefinition { id = "dash_fail", hidden = true, hullDelta = -4, survivorDelta = 20, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -5 });
            Event(result, "ice_locked_lifeboats", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "thaw", requiredTag = "lineage.dwarf", suppliesCost = 1, survivorDelta = 30, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "blast", aetherCost = 1, survivorDelta = 28, salvageDelta = 3, moraleDelta = 1 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -4 });
            Event(result, "mutiny_transport", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "intimidate", requiredTag = "lineage.orc", survivorDelta = 55, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "negotiate", suppliesCost = 2, survivorDelta = 55, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -3 });
            Event(result, "plague_barge", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "quarantine", requiredTag = "support.hospital", survivorDelta = 60, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "take", survivorDelta = 60, moraleDelta = 4, successChance = 0.5f, failureChoiceId = "take_fail" },
                new EncounterChoiceDefinition { id = "take_fail", hidden = true, survivorDelta = -30, moraleDelta = -8 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -4 });
            Event(result, "child_choir", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "take", suppliesCost = 1, survivorDelta = 18, moraleDelta = 10 },
                new EncounterChoiceDefinition { id = "fuel", aetherCost = 1, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -6 });
            Event(result, "imperial_deserters", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "accept", survivorDelta = 25, ordnanceDelta = 2, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "interrogate", requiredTag = "lineage.human", survivorDelta = 25, aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -1 });
            Event(result, "stranded_engineers", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "take", survivorDelta = 12, hullDelta = 6, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "pay", salvageCost = 3, survivorDelta = 12, hullDelta = 10, armorDelta = 4, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -3 });
            Event(result, "sky_whale_calf", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "calm", requiredTag = "lineage.avian", moraleDelta = 9, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "free", suppliesCost = 1, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "harvest", salvageDelta = 8, moraleDelta = -9 });
            Event(result, "wreck_signal_trap", EncounterType.Rescue,
                new EncounterChoiceDefinition { id = "detect", requiredTag = "support.pathfinder", salvageDelta = 6, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "approach", survivorDelta = 50, moraleDelta = 5, successChance = 0.55f, failureChoiceId = "ambush" },
                new EncounterChoiceDefinition { id = "ambush", hidden = true, startsBattle = true, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "ignore", moraleDelta = -2 });
            Event(result, "derelict_cruiser", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "strip", ordnanceDelta = 4, salvageDelta = 4 },
                new EncounterChoiceDefinition { id = "core", requiredTag = "lineage.dwarf", aetherDelta = 3, salvageDelta = 3, successChance = 0.7f, failureChoiceId = "core_fail" },
                new EncounterChoiceDefinition { id = "core_fail", hidden = true, hullDelta = -5, instabilityDelta = 15 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "aether_geyser", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "harvest", aetherDelta = 4, instabilityDelta = 20 },
                new EncounterChoiceDefinition { id = "careful", aetherDelta = 2, instabilityDelta = 5 },
                new EncounterChoiceDefinition { id = "pass" });
            Event(result, "floating_monastery", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "relics", salvageDelta = 10, moraleDelta = -6 },
                new EncounterChoiceDefinition { id = "prayers", moraleDelta = 7 },
                new EncounterChoiceDefinition { id = "resonate", requiredTag = "lineage.elf", aetherDelta = 2, moraleDelta = 4 });
            Event(result, "mine_field", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "chart", requiredTag = "support.pathfinder", ordnanceDelta = 3, salvageDelta = 5 },
                new EncounterChoiceDefinition { id = "thread", salvageDelta = 9, successChance = 0.6f, failureChoiceId = "thread_fail" },
                new EncounterChoiceDefinition { id = "thread_fail", hidden = true, hullDelta = -6, armorDelta = -4, salvageDelta = 3 },
                new EncounterChoiceDefinition { id = "avoid", aetherCost = 1 });
            Event(result, "crashed_courier", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "decode", aetherDelta = 1, suppliesDelta = 2, moraleDelta = 1 },
                new EncounterChoiceDefinition { id = "sell", salvageDelta = 7 },
                new EncounterChoiceDefinition { id = "burn", moraleDelta = 3, ordnanceDelta = 1 });
            Event(result, "cloud_farm", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "harvest", suppliesDelta = 6 },
                new EncounterChoiceDefinition { id = "settle", survivorDelta = 20, suppliesDelta = 3, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "ordnance_cache", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "careful", ordnanceDelta = 3 },
                new EncounterChoiceDefinition { id = "blast", ordnanceDelta = 6, salvageDelta = 2, successChance = 0.65f, failureChoiceId = "blast_fail" },
                new EncounterChoiceDefinition { id = "blast_fail", hidden = true, ordnanceDelta = 1, hullDelta = -3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "sky_kelp_forest", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "gather", suppliesDelta = 5 },
                new EncounterChoiceDefinition { id = "scavenge", requiredTag = "lineage.goblin", salvageDelta = 6, suppliesDelta = 2 },
                new EncounterChoiceDefinition { id = "push", aetherCost = 1, moraleDelta = 1 });
            Event(result, "gate_shard", EncounterType.Salvage,
                new EncounterChoiceDefinition { id = "study", aetherDelta = 2, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "sell", salvageDelta = 12 },
                new EncounterChoiceDefinition { id = "attune", requiredTag = "lineage.elf", aetherDelta = 4, instabilityDelta = 10 });
            Event(result, "smuggler_flotilla", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "ordnance", salvageCost = 5, ordnanceDelta = 4 },
                new EncounterChoiceDefinition { id = "aether", salvageCost = 7, aetherDelta = 4 },
                new EncounterChoiceDefinition { id = "sell", suppliesCost = 2, salvageDelta = 9 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "guild_caravan", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "escort", ordnanceCost = 1, salvageDelta = 8, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "buy", salvageCost = 5, suppliesDelta = 6 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "refit_yard", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "refit", salvageCost = 6, refitSquadrons = true },
                new EncounterChoiceDefinition { id = "plating", salvageCost = 9, armorDelta = 8 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "black_market", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "cheap", salvageCost = 4, aetherDelta = 3, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "haggle", requiredTag = "lineage.goblin", salvageCost = 3, aetherDelta = 3, suppliesDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "pilgrim_bazaar", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "comforts", suppliesCost = 1, moraleDelta = 8 },
                new EncounterChoiceDefinition { id = "sell", salvageDelta = 5, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "mercenary_wing", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "hire", salvageCost = 10, ordnanceDelta = 3, refitSquadrons = true },
                new EncounterChoiceDefinition { id = "ordnance", salvageCost = 4, ordnanceDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "fuel_barge", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "small", suppliesCost = 3, aetherDelta = 3 },
                new EncounterChoiceDefinition { id = "big", salvageCost = 8, aetherDelta = 6 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "quartermaster", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "bribe", salvageCost = 7, ordnanceDelta = 5, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "pose", requiredTag = "lineage.human", suppliesCost = 1, ordnanceDelta = 4, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "shipwright", EncounterType.Trade,
                new EncounterChoiceDefinition { id = "repair", salvageCost = 7 },
                new EncounterChoiceDefinition { id = "reinforce", salvageCost = 5, hullDelta = 5, armorDelta = 3 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "customs_inspection", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "submit", suppliesCost = 2, moraleDelta = -1 },
                new EncounterChoiceDefinition { id = "hide", moraleDelta = 4, successChance = 0.6f, failureChoiceId = "hide_fail" },
                new EncounterChoiceDefinition { id = "hide_fail", hidden = true, startsBattle = true, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "loyalty_oath", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "swear", aetherDelta = 2, moraleDelta = -8 },
                new EncounterChoiceDefinition { id = "recite", requiredTag = "lineage.human", moraleDelta = 2, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "refuse", startsBattle = true, moraleDelta = 3 });
            Event(result, "bounty_hunters", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "pay", salvageCost = 8 },
                new EncounterChoiceDefinition { id = "intimidate", requiredTag = "lineage.orc", moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "blockade_toll", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "pay", aetherCost = 1, salvageCost = 4 },
                new EncounterChoiceDefinition { id = "run", moraleDelta = 5, successChance = 0.5f, failureChoiceId = "run_fail" },
                new EncounterChoiceDefinition { id = "run_fail", hidden = true, startsBattle = true, battleTier = 2 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true, battleTier = 2 });
            Event(result, "propaganda_broadcast", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "jam", ordnanceCost = 1, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "counter", moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "ignore", moraleDelta = -6 });
            Event(result, "reformist_courier", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "help", suppliesCost = 1, aetherDelta = 2, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -2 });
            Event(result, "hostage_exchange", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "trade", ordnanceCost = 2, survivorDelta = 40, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "assault", survivorDelta = 40, moraleDelta = 8, successChance = 0.5f, failureChoiceId = "assault_fail" },
                new EncounterChoiceDefinition { id = "assault_fail", hidden = true, startsBattle = true, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -4 });
            Event(result, "spy_aboard", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "search", suppliesCost = 1, moraleDelta = -2, ordnanceDelta = 1 },
                new EncounterChoiceDefinition { id = "spot", requiredTag = "lineage.avian", moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "ignore", moraleDelta = 1, successChance = 0.5f, failureChoiceId = "sabotage" },
                new EncounterChoiceDefinition { id = "sabotage", hidden = true, aetherDelta = -2, instabilityDelta = 10 });
            Event(result, "pilgrim_blockade", EncounterType.Checkpoint,
                new EncounterChoiceDefinition { id = "escort", aetherCost = 1, survivorDelta = 30, moraleDelta = 7 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -2 });
            Event(result, "ion_squall", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "calm", aetherCost = 1, instabilityDelta = -15 },
                new EncounterChoiceDefinition { id = "push", aetherDelta = 1, successChance = 0.6f, failureChoiceId = "push_fail" },
                new EncounterChoiceDefinition { id = "push_fail", hidden = true, hullDelta = -4, instabilityDelta = 15 });
            Event(result, "static_fog", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "chart", requiredTag = "support.pathfinder", moraleDelta = 2, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "slow", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "fast", moraleDelta = 2, successChance = 0.5f, failureChoiceId = "fast_fail" },
                new EncounterChoiceDefinition { id = "fast_fail", hidden = true, armorDelta = -6 });
            Event(result, "hail_front", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "brace", requiredTag = "lineage.dwarf", armorDelta = -1, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "climb", aetherCost = 2 },
                new EncounterChoiceDefinition { id = "endure", armorDelta = -5, hullDelta = -2 });
            Event(result, "aether_bloom", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "harvest", aetherDelta = 4, instabilityDelta = 20 },
                new EncounterChoiceDefinition { id = "skirt", aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "avoid" });
            Event(result, "lightning_choir", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "conduct", requiredTag = "lineage.elf", aetherDelta = 3, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "ground", ordnanceCost = 1 },
                new EncounterChoiceDefinition { id = "endure", moraleDelta = 1, successChance = 0.5f, failureChoiceId = "strike" },
                new EncounterChoiceDefinition { id = "strike", hidden = true, hullDelta = -5, moraleDelta = -2 });
            Event(result, "updraft_chasm", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "high", aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "avoid", aetherCost = 1 });
            Event(result, "cloud_reef", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "climb", requiredTag = "lineage.goblin", salvageDelta = 5 },
                new EncounterChoiceDefinition { id = "around", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "risk", salvageDelta = 8, successChance = 0.55f, failureChoiceId = "scrape" },
                new EncounterChoiceDefinition { id = "scrape", hidden = true, hullDelta = -6, salvageDelta = 2 });
            Event(result, "whiteout", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "anchor", suppliesCost = 1, moraleDelta = 1 },
                new EncounterChoiceDefinition { id = "press", moraleDelta = 2, successChance = 0.6f, failureChoiceId = "lost" },
                new EncounterChoiceDefinition { id = "lost", hidden = true, survivorDelta = -20, moraleDelta = -5 });
            Event(result, "storm_leviathan", EncounterType.Storm,
                new EncounterChoiceDefinition { id = "flee", aetherCost = 2 },
                new EncounterChoiceDefinition { id = "sing", requiredTag = "lineage.avian", moraleDelta = 8, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "harpoon", salvageDelta = 15, moraleDelta = 5, successChance = 0.4f, failureChoiceId = "harpoon_fail" },
                new EncounterChoiceDefinition { id = "harpoon_fail", hidden = true, hullDelta = -8, moraleDelta = -4 });
        }

        private static void Event(Dictionary<string, EncounterDefinition> result, string id, EncounterType type, params EncounterChoiceDefinition[] choices)
        {
            var encounter = new EncounterDefinition { id = id, type = type, titleKey = "enc." + id + ".title", bodyKey = "enc." + id + ".body" };
            for (var i = 0; i < choices.Length; i++)
            {
                choices[i].textKey = "enc." + id + "." + choices[i].id;
                choices[i].resultKey = "enc." + id + "." + choices[i].id + ".r";
                encounter.choices.Add(choices[i]);
            }
            result[id] = encounter;
        }
    }
}
