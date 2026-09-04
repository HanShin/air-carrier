using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    /// <summary>Authored events beyond the five baseline encounters. Generated from tools/gen_events.py data.</summary>
    public static class EncounterLibrary
    {
        public static void AddAll(Dictionary<string, EncounterDefinition> result)
        {
            Event(result, "burning_ferry", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "teams", suppliesCost = 1, survivorDelta = 40, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "dash", survivorDelta = 70, moraleDelta = 6, successChance = 0.6f, failureChoiceId = "dash_fail" },
                new EncounterChoiceDefinition { id = "dash_fail", hidden = true, hullDelta = -4, survivorDelta = 20, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -5 });
            Event(result, "ice_locked_lifeboats", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "thaw", requiredTag = "lineage.dwarf", suppliesCost = 1, survivorDelta = 30, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "blast", aetherCost = 1, survivorDelta = 28, salvageDelta = 3, moraleDelta = 1 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -4 });
            Event(result, "mutiny_transport", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "intimidate", requiredTag = "lineage.orc", survivorDelta = 55, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "negotiate", suppliesCost = 2, survivorDelta = 55, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -3 });
            Event(result, "plague_barge", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "quarantine", requiredTag = "support.hospital", survivorDelta = 60, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "take", survivorDelta = 60, moraleDelta = 4, successChance = 0.5f, failureChoiceId = "take_fail" },
                new EncounterChoiceDefinition { id = "take_fail", hidden = true, survivorDelta = -30, moraleDelta = -8 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -4 });
            Event(result, "child_choir", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "take", suppliesCost = 1, survivorDelta = 18, moraleDelta = 10 },
                new EncounterChoiceDefinition { id = "fuel", aetherCost = 1, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -6 });
            Event(result, "imperial_deserters", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "accept", survivorDelta = 25, ordnanceDelta = 2, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "interrogate", requiredTag = "lineage.human", survivorDelta = 25, aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -1 });
            Event(result, "stranded_engineers", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "take", survivorDelta = 12, hullDelta = 6, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "pay", salvageCost = 3, survivorDelta = 12, hullDelta = 10, armorDelta = 4, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -3 });
            Event(result, "sky_whale_calf", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "calm", requiredTag = "lineage.avian", moraleDelta = 9, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "free", suppliesCost = 1, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "harvest", salvageDelta = 8, moraleDelta = -9 });
            Event(result, "wreck_signal_trap", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "detect", requiredTag = "support.pathfinder", salvageDelta = 6, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "approach", survivorDelta = 50, moraleDelta = 5, successChance = 0.55f, failureChoiceId = "ambush" },
                new EncounterChoiceDefinition { id = "ambush", hidden = true, startsBattle = true, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "ignore", moraleDelta = -2 });
            Event(result, "derelict_cruiser", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "strip", ordnanceDelta = 4, salvageDelta = 4 },
                new EncounterChoiceDefinition { id = "core", requiredTag = "lineage.dwarf", aetherDelta = 3, salvageDelta = 3, successChance = 0.7f, failureChoiceId = "core_fail" },
                new EncounterChoiceDefinition { id = "core_fail", hidden = true, hullDelta = -5, instabilityDelta = 15 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "aether_geyser", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "harvest", aetherDelta = 4, instabilityDelta = 20 },
                new EncounterChoiceDefinition { id = "careful", aetherDelta = 2, instabilityDelta = 5 },
                new EncounterChoiceDefinition { id = "pass" });
            Event(result, "floating_monastery", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "relics", salvageDelta = 10, moraleDelta = -6 },
                new EncounterChoiceDefinition { id = "prayers", moraleDelta = 7 },
                new EncounterChoiceDefinition { id = "resonate", requiredTag = "lineage.elf", aetherDelta = 2, moraleDelta = 4 });
            Event(result, "mine_field", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "chart", requiredTag = "support.pathfinder", ordnanceDelta = 3, salvageDelta = 5 },
                new EncounterChoiceDefinition { id = "thread", salvageDelta = 9, successChance = 0.6f, failureChoiceId = "thread_fail" },
                new EncounterChoiceDefinition { id = "thread_fail", hidden = true, hullDelta = -6, armorDelta = -4, salvageDelta = 3 },
                new EncounterChoiceDefinition { id = "avoid", aetherCost = 1 });
            Event(result, "crashed_courier", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "decode", aetherDelta = 1, suppliesDelta = 2, moraleDelta = 1 },
                new EncounterChoiceDefinition { id = "sell", salvageDelta = 7 },
                new EncounterChoiceDefinition { id = "burn", moraleDelta = 3, ordnanceDelta = 1 });
            Event(result, "cloud_farm", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "harvest", suppliesDelta = 6 },
                new EncounterChoiceDefinition { id = "settle", survivorDelta = 20, suppliesDelta = 3, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "ordnance_cache", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "careful", ordnanceDelta = 3 },
                new EncounterChoiceDefinition { id = "blast", ordnanceDelta = 6, salvageDelta = 2, successChance = 0.65f, failureChoiceId = "blast_fail" },
                new EncounterChoiceDefinition { id = "blast_fail", hidden = true, ordnanceDelta = 1, hullDelta = -3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "sky_kelp_forest", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "gather", suppliesDelta = 5 },
                new EncounterChoiceDefinition { id = "scavenge", requiredTag = "lineage.goblin", salvageDelta = 6, suppliesDelta = 2 },
                new EncounterChoiceDefinition { id = "push", aetherCost = 1, moraleDelta = 1 });
            Event(result, "gate_shard", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "study", aetherDelta = 2, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "sell", salvageDelta = 12 },
                new EncounterChoiceDefinition { id = "attune", requiredTag = "lineage.elf", aetherDelta = 4, instabilityDelta = 10 });
            Event(result, "smuggler_flotilla", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "ordnance", salvageCost = 5, ordnanceDelta = 4 },
                new EncounterChoiceDefinition { id = "aether", salvageCost = 7, aetherDelta = 4 },
                new EncounterChoiceDefinition { id = "sell", suppliesCost = 2, salvageDelta = 9 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "guild_caravan", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "escort", ordnanceCost = 1, salvageDelta = 8, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "buy", salvageCost = 5, suppliesDelta = 6 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "refit_yard", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "refit", salvageCost = 6, refitSquadrons = true },
                new EncounterChoiceDefinition { id = "plating", salvageCost = 9, armorDelta = 8 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "black_market", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "cheap", salvageCost = 4, aetherDelta = 3, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "haggle", requiredTag = "lineage.goblin", salvageCost = 3, aetherDelta = 3, suppliesDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "pilgrim_bazaar", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "comforts", suppliesCost = 1, moraleDelta = 8 },
                new EncounterChoiceDefinition { id = "sell", salvageDelta = 5, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "mercenary_wing", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "hire", salvageCost = 10, ordnanceDelta = 3, refitSquadrons = true },
                new EncounterChoiceDefinition { id = "ordnance", salvageCost = 4, ordnanceDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "fuel_barge", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "small", suppliesCost = 3, aetherDelta = 3 },
                new EncounterChoiceDefinition { id = "big", salvageCost = 8, aetherDelta = 6 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "quartermaster", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "bribe", salvageCost = 7, ordnanceDelta = 5, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "pose", requiredTag = "lineage.human", suppliesCost = 1, ordnanceDelta = 4, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "shipwright", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "repair", salvageCost = 7 },
                new EncounterChoiceDefinition { id = "reinforce", salvageCost = 5, hullDelta = 5, armorDelta = 3 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "customs_inspection", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "submit", suppliesCost = 2, moraleDelta = -1 },
                new EncounterChoiceDefinition { id = "hide", moraleDelta = 4, successChance = 0.6f, failureChoiceId = "hide_fail" },
                new EncounterChoiceDefinition { id = "hide_fail", hidden = true, startsBattle = true, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "loyalty_oath", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "swear", aetherDelta = 2, moraleDelta = -8 },
                new EncounterChoiceDefinition { id = "recite", requiredTag = "lineage.human", moraleDelta = 2, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "refuse", startsBattle = true, moraleDelta = 3 });
            Event(result, "bounty_hunters", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "pay", salvageCost = 8 },
                new EncounterChoiceDefinition { id = "intimidate", requiredTag = "lineage.orc", moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "blockade_toll", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "pay", aetherCost = 1, salvageCost = 4 },
                new EncounterChoiceDefinition { id = "run", moraleDelta = 5, successChance = 0.5f, failureChoiceId = "run_fail" },
                new EncounterChoiceDefinition { id = "run_fail", hidden = true, startsBattle = true, battleTier = 2 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true, battleTier = 2 });
            Event(result, "propaganda_broadcast", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "jam", ordnanceCost = 1, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "counter", moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "ignore", moraleDelta = -6 });
            Event(result, "reformist_courier", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "help", suppliesCost = 1, aetherDelta = 2, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -2 });
            Event(result, "hostage_exchange", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "trade", ordnanceCost = 2, survivorDelta = 40, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "assault", survivorDelta = 40, moraleDelta = 8, successChance = 0.5f, failureChoiceId = "assault_fail" },
                new EncounterChoiceDefinition { id = "assault_fail", hidden = true, startsBattle = true, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = -4 });
            Event(result, "spy_aboard", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "search", suppliesCost = 1, moraleDelta = -2, ordnanceDelta = 1 },
                new EncounterChoiceDefinition { id = "spot", requiredTag = "lineage.avian", moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "ignore", moraleDelta = 1, successChance = 0.5f, failureChoiceId = "sabotage" },
                new EncounterChoiceDefinition { id = "sabotage", hidden = true, aetherDelta = -2, instabilityDelta = 10 });
            Event(result, "pilgrim_blockade", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "escort", aetherCost = 1, survivorDelta = 30, moraleDelta = 7 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -2 });
            Event(result, "ion_squall", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "calm", aetherCost = 1, instabilityDelta = -15 },
                new EncounterChoiceDefinition { id = "push", aetherDelta = 1, successChance = 0.6f, failureChoiceId = "push_fail" },
                new EncounterChoiceDefinition { id = "push_fail", hidden = true, hullDelta = -4, instabilityDelta = 15 });
            Event(result, "static_fog", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "chart", requiredTag = "support.pathfinder", moraleDelta = 2, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "slow", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "fast", moraleDelta = 2, successChance = 0.5f, failureChoiceId = "fast_fail" },
                new EncounterChoiceDefinition { id = "fast_fail", hidden = true, armorDelta = -6 });
            Event(result, "hail_front", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "brace", requiredTag = "lineage.dwarf", armorDelta = -1, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "climb", aetherCost = 2 },
                new EncounterChoiceDefinition { id = "endure", armorDelta = -5, hullDelta = -2 });
            Event(result, "aether_bloom", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "harvest", aetherDelta = 4, instabilityDelta = 20 },
                new EncounterChoiceDefinition { id = "skirt", aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "avoid" });
            Event(result, "lightning_choir", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "conduct", requiredTag = "lineage.elf", aetherDelta = 3, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "ground", ordnanceCost = 1 },
                new EncounterChoiceDefinition { id = "endure", moraleDelta = 1, successChance = 0.5f, failureChoiceId = "strike" },
                new EncounterChoiceDefinition { id = "strike", hidden = true, hullDelta = -5, moraleDelta = -2 });
            Event(result, "updraft_chasm", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "high", aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "avoid", aetherCost = 1 });
            Event(result, "cloud_reef", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "climb", requiredTag = "lineage.goblin", salvageDelta = 5 },
                new EncounterChoiceDefinition { id = "around", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "risk", salvageDelta = 8, successChance = 0.55f, failureChoiceId = "scrape" },
                new EncounterChoiceDefinition { id = "scrape", hidden = true, hullDelta = -6, salvageDelta = 2 });
            Event(result, "whiteout", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "anchor", suppliesCost = 1, moraleDelta = 1 },
                new EncounterChoiceDefinition { id = "press", moraleDelta = 2, successChance = 0.6f, failureChoiceId = "lost" },
                new EncounterChoiceDefinition { id = "lost", hidden = true, survivorDelta = -20, moraleDelta = -5 });
            Event(result, "storm_leviathan", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "flee", aetherCost = 2 },
                new EncounterChoiceDefinition { id = "sing", requiredTag = "lineage.avian", moraleDelta = 8, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "harpoon", salvageDelta = 15, moraleDelta = 5, successChance = 0.4f, failureChoiceId = "harpoon_fail" },
                new EncounterChoiceDefinition { id = "harpoon_fail", hidden = true, hullDelta = -8, moraleDelta = -4 });
            Event(result, "frozen_survey_team", EncounterType.Rescue, new int[] { 3 },
                new EncounterChoiceDefinition { id = "fetch", aetherCost = 1, survivorDelta = 14, moraleDelta = 4, salvageDelta = 4 },
                new EncounterChoiceDefinition { id = "dwarf", requiredTag = "lineage.dwarf", moraleDelta = 6, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -3 });
            Event(result, "abyss_diving_bell", EncounterType.Rescue, new int[] { 5 },
                new EncounterChoiceDefinition { id = "haul", survivorDelta = 22, salvageDelta = 8, instabilityDelta = 10 },
                new EncounterChoiceDefinition { id = "cut", moraleDelta = -8 },
                new EncounterChoiceDefinition { id = "orc", requiredTag = "lineage.orc", survivorDelta = 22, salvageDelta = 8, moraleDelta = 4 });
            Event(result, "cordon_escapees", EncounterType.Rescue, new int[] { 4, 6 },
                new EncounterChoiceDefinition { id = "cover", survivorDelta = 36, moraleDelta = 5, startsBattle = true },
                new EncounterChoiceDefinition { id = "decoy", ordnanceCost = 1, survivorDelta = 36, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -4 });
            Event(result, "drifting_nursery", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "adopt", survivorDelta = 30, moraleDelta = 9, suppliesCost = 1 },
                new EncounterChoiceDefinition { id = "navigator", requiredTag = "lineage.avian", moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -7 });
            Event(result, "wrecked_gunship", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "both", suppliesCost = 1, survivorDelta = 16, ordnanceDelta = 3, salvageDelta = 4 },
                new EncounterChoiceDefinition { id = "guns", ordnanceDelta = 4, salvageDelta = 6, moraleDelta = -5 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "storm_shrine_hermits", EncounterType.Rescue, new int[] { 2, 5 },
                new EncounterChoiceDefinition { id = "children", survivorDelta = 12, moraleDelta = 6, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "persuade", requiredTag = "lineage.elf", survivorDelta = 28, moraleDelta = 8 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = 1 });
            Event(result, "quarantined_liner", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "inspect", requiredTag = "support.hospital", survivorDelta = 70, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "risk", survivorDelta = 70, moraleDelta = 4, successChance = 0.6f, failureChoiceId = "risk_fail" },
                new EncounterChoiceDefinition { id = "risk_fail", hidden = true, survivorDelta = -25, moraleDelta = -6 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -3 });
            Event(result, "lost_patrol", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "fuel", aetherCost = 1, moraleDelta = 3, salvageDelta = 3 },
                new EncounterChoiceDefinition { id = "recruit", survivorDelta = 18, moraleDelta = -2, ordnanceDelta = 1 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "collapsing_spire", EncounterType.Rescue, new int[] {  },
                new EncounterChoiceDefinition { id = "bridge", survivorDelta = 60, moraleDelta = 8, successChance = 0.7f, failureChoiceId = "bridge_fail" },
                new EncounterChoiceDefinition { id = "bridge_fail", hidden = true, hullDelta = -6, survivorDelta = 20, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "wings", ordnanceCost = 1, survivorDelta = 40, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -6 });
            Event(result, "throne_pilgrims_stranded", EncounterType.Rescue, new int[] { 6 },
                new EncounterChoiceDefinition { id = "share", aetherCost = 2, survivorDelta = 25, moraleDelta = 10 },
                new EncounterChoiceDefinition { id = "take", survivorDelta = 25, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "leave", moraleDelta = -4 });
            Event(result, "ice_locked_freighter", EncounterType.Salvage, new int[] { 3 },
                new EncounterChoiceDefinition { id = "thaw", suppliesDelta = 5, salvageDelta = 6 },
                new EncounterChoiceDefinition { id = "blast", aetherCost = 1, suppliesDelta = 6, salvageDelta = 9, ordnanceDelta = 1 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "abyss_relic_field", EncounterType.Salvage, new int[] { 5 },
                new EncounterChoiceDefinition { id = "gather", salvageDelta = 14, aetherDelta = 2, instabilityDelta = 25 },
                new EncounterChoiceDefinition { id = "careful", requiredTag = "lineage.elf", salvageDelta = 12, aetherDelta = 3 },
                new EncounterChoiceDefinition { id = "skirt", salvageDelta = 5 });
            Event(result, "cordon_supply_dump", EncounterType.Salvage, new int[] { 4, 6 },
                new EncounterChoiceDefinition { id = "raid", suppliesDelta = 6, ordnanceDelta = 4, salvageDelta = 8, successChance = 0.6f, failureChoiceId = "raid_fail" },
                new EncounterChoiceDefinition { id = "raid_fail", hidden = true, suppliesDelta = 3, ordnanceDelta = 2, startsBattle = true },
                new EncounterChoiceDefinition { id = "scout", requiredTag = "support.pathfinder", suppliesDelta = 6, ordnanceDelta = 4, salvageDelta = 8 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "whale_bone_reef", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "mine", aetherDelta = 3, salvageDelta = 3 },
                new EncounterChoiceDefinition { id = "respect", requiredTag = "lineage.avian", aetherDelta = 3, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "sunken_arsenal", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "dive", ordnanceDelta = 6, salvageDelta = 5, successChance = 0.55f, failureChoiceId = "dive_fail" },
                new EncounterChoiceDefinition { id = "dive_fail", hidden = true, hullDelta = -5, ordnanceDelta = 2 },
                new EncounterChoiceDefinition { id = "wings", ordnanceCost = 1, ordnanceDelta = 4, salvageDelta = 3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "derelict_hospital_ship", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "medicine", moraleDelta = 6, suppliesDelta = 3 },
                new EncounterChoiceDefinition { id = "equipment", salvageDelta = 8, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "storm_wrecked_convoy", EncounterType.Salvage, new int[] { 2, 5 },
                new EncounterChoiceDefinition { id = "salvage", suppliesDelta = 6, salvageDelta = 8, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "honour", suppliesDelta = 3, salvageDelta = 3, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "throne_watchtower", EncounterType.Salvage, new int[] { 6 },
                new EncounterChoiceDefinition { id = "records", moraleDelta = 10, aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "strip", salvageDelta = 14 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "pirate_cache", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "goblin", requiredTag = "lineage.goblin", salvageDelta = 12, ordnanceDelta = 2 },
                new EncounterChoiceDefinition { id = "open", salvageDelta = 10, ordnanceDelta = 2, successChance = 0.65f, failureChoiceId = "open_fail" },
                new EncounterChoiceDefinition { id = "open_fail", hidden = true, hullDelta = -4, salvageDelta = 3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "crystal_garden", EncounterType.Salvage, new int[] {  },
                new EncounterChoiceDefinition { id = "harvest", aetherDelta = 4, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "tend", aetherDelta = 2, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "ice_trading_post", EncounterType.Trade, new int[] { 3 },
                new EncounterChoiceDefinition { id = "stores", salvageCost = 5, suppliesDelta = 6 },
                new EncounterChoiceDefinition { id = "fuel", salvageCost = 6, aetherDelta = 4 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "abyss_salvagers", EncounterType.Trade, new int[] { 5 },
                new EncounterChoiceDefinition { id = "ordnance", salvageCost = 8, ordnanceDelta = 6 },
                new EncounterChoiceDefinition { id = "plating", salvageCost = 10, armorDelta = 8 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "cordon_smuggler_tunnel", EncounterType.Trade, new int[] { 4, 6 },
                new EncounterChoiceDefinition { id = "route", salvageCost = 6, aetherDelta = 3 },
                new EncounterChoiceDefinition { id = "stores", salvageCost = 7, suppliesDelta = 5, ordnanceDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "throne_court_merchant", EncounterType.Trade, new int[] { 6 },
                new EncounterChoiceDefinition { id = "refit", salvageCost = 12, hullDelta = 8, armorDelta = 6 },
                new EncounterChoiceDefinition { id = "wings", salvageCost = 10, refitSquadrons = true },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "wandering_alchemist", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "stabilise", salvageCost = 5, instabilityDelta = -30 },
                new EncounterChoiceDefinition { id = "tonic", suppliesCost = 1, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "refugee_market", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "swap", ordnanceCost = 2, suppliesDelta = 6 },
                new EncounterChoiceDefinition { id = "swap2", suppliesCost = 3, aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "gunsmith_barge", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "ordnance", salvageCost = 5, ordnanceDelta = 4 },
                new EncounterChoiceDefinition { id = "tune", salvageCost = 6, hullDelta = 3 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "monastery_kitchens", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "donate", salvageCost = 4, suppliesDelta = 5, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "free", suppliesDelta = 3, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "captains_exchange", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "intel", salvageCost = 4, aetherDelta = 2, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "human", requiredTag = "lineage.human", aetherDelta = 2, ordnanceDelta = 2 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "scrap_auction", EncounterType.Trade, new int[] {  },
                new EncounterChoiceDefinition { id = "bid", salvageCost = 5, hullDelta = 6, armorDelta = 4, successChance = 0.6f, failureChoiceId = "bid_fail" },
                new EncounterChoiceDefinition { id = "bid_fail", hidden = true },
                new EncounterChoiceDefinition { id = "sell", salvageCost = 6, suppliesDelta = 4 },
                new EncounterChoiceDefinition { id = "depart" });
            Event(result, "ice_garrison", EncounterType.Checkpoint, new int[] { 3 },
                new EncounterChoiceDefinition { id = "bribe", suppliesCost = 2 },
                new EncounterChoiceDefinition { id = "dwarf", requiredTag = "lineage.dwarf", moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "abyss_toll_keepers", EncounterType.Checkpoint, new int[] { 5 },
                new EncounterChoiceDefinition { id = "pay", salvageCost = 6 },
                new EncounterChoiceDefinition { id = "goblin", requiredTag = "lineage.goblin", salvageCost = 3 },
                new EncounterChoiceDefinition { id = "run", moraleDelta = 3, successChance = 0.55f, failureChoiceId = "run_fail" },
                new EncounterChoiceDefinition { id = "run_fail", hidden = true, hullDelta = -5, armorDelta = -3 });
            Event(result, "cordon_inquisitor", EncounterType.Checkpoint, new int[] { 4, 6 },
                new EncounterChoiceDefinition { id = "submit", moraleDelta = -5 },
                new EncounterChoiceDefinition { id = "human", requiredTag = "lineage.human", moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true, battleTier = 2 });
            Event(result, "throne_gatekeepers", EncounterType.Checkpoint, new int[] { 6 },
                new EncounterChoiceDefinition { id = "trial", moraleDelta = 10, aetherDelta = 2, successChance = 0.6f, failureChoiceId = "trial_fail" },
                new EncounterChoiceDefinition { id = "trial_fail", hidden = true, startsBattle = true, battleTier = 2 },
                new EncounterChoiceDefinition { id = "elf", requiredTag = "lineage.elf", moraleDelta = 12, aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true, battleTier = 2 });
            Event(result, "conscription_sweep", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "refuse", startsBattle = true, moraleDelta = 4 },
                new EncounterChoiceDefinition { id = "bribe", salvageCost = 8, moraleDelta = -2 },
                new EncounterChoiceDefinition { id = "orc", requiredTag = "lineage.orc", moraleDelta = 5 });
            Event(result, "tariff_station", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "pay", suppliesCost = 1, salvageCost = 3 },
                new EncounterChoiceDefinition { id = "forge", moraleDelta = 2, successChance = 0.5f, failureChoiceId = "forge_fail" },
                new EncounterChoiceDefinition { id = "forge_fail", hidden = true, salvageDelta = -8, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "deserter_hunt", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "allow", survivorDelta = -10, moraleDelta = -6 },
                new EncounterChoiceDefinition { id = "hide", moraleDelta = 5, successChance = 0.6f, failureChoiceId = "hide_fail" },
                new EncounterChoiceDefinition { id = "hide_fail", hidden = true, startsBattle = true },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true });
            Event(result, "weather_station_seizure", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "buy", salvageCost = 5, aetherDelta = 2 },
                new EncounterChoiceDefinition { id = "avian", requiredTag = "lineage.avian", aetherDelta = 1, moraleDelta = 3 },
                new EncounterChoiceDefinition { id = "leave" });
            Event(result, "refugee_registration", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "register", moraleDelta = -4, suppliesDelta = 2 },
                new EncounterChoiceDefinition { id = "partial", successChance = 0.65f, failureChoiceId = "partial_fail" },
                new EncounterChoiceDefinition { id = "partial_fail", hidden = true, startsBattle = true },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true, moraleDelta = 3 });
            Event(result, "admiral_parley", EncounterType.Checkpoint, new int[] {  },
                new EncounterChoiceDefinition { id = "refuse", moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "stall", aetherDelta = 1, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "fight", startsBattle = true, battleTier = 2 });
            Event(result, "ice_storm", EncounterType.Storm, new int[] { 3 },
                new EncounterChoiceDefinition { id = "heat", aetherCost = 1, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "endure", armorDelta = -4, instabilityDelta = 5 },
                new EncounterChoiceDefinition { id = "dwarf", requiredTag = "lineage.dwarf", moraleDelta = 4 });
            Event(result, "abyss_undertow", EncounterType.Storm, new int[] { 5 },
                new EncounterChoiceDefinition { id = "climb", aetherCost = 2 },
                new EncounterChoiceDefinition { id = "ride", aetherDelta = 3, salvageDelta = 4, successChance = 0.55f, failureChoiceId = "ride_fail" },
                new EncounterChoiceDefinition { id = "ride_fail", hidden = true, hullDelta = -7 },
                new EncounterChoiceDefinition { id = "anchor", suppliesCost = 1 });
            Event(result, "cordon_searchlights", EncounterType.Storm, new int[] { 4, 6 },
                new EncounterChoiceDefinition { id = "dark", instabilityDelta = 5 },
                new EncounterChoiceDefinition { id = "fast", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "caught", successChance = 0.5f, failureChoiceId = "caught_fail" },
                new EncounterChoiceDefinition { id = "caught_fail", hidden = true, startsBattle = true });
            Event(result, "throne_aurora", EncounterType.Storm, new int[] { 6 },
                new EncounterChoiceDefinition { id = "bathe", aetherDelta = 5, instabilityDelta = 20, moraleDelta = 5 },
                new EncounterChoiceDefinition { id = "elf", requiredTag = "lineage.elf", aetherDelta = 5, moraleDelta = 6 },
                new EncounterChoiceDefinition { id = "skirt", aetherDelta = 2 });
            Event(result, "ash_cloud", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "slow", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "fast", hullDelta = -3, instabilityDelta = 10 },
                new EncounterChoiceDefinition { id = "around", aetherCost = 2, moraleDelta = 1 });
            Event(result, "ghost_lights", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "ignore", aetherCost = 1 },
                new EncounterChoiceDefinition { id = "follow", aetherDelta = 3, successChance = 0.5f, failureChoiceId = "follow_fail" },
                new EncounterChoiceDefinition { id = "follow_fail", hidden = true, aetherDelta = -2, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "avian", requiredTag = "lineage.avian", moraleDelta = 3 });
            Event(result, "crystal_rain", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "open", aetherDelta = 3, armorDelta = -3 },
                new EncounterChoiceDefinition { id = "closed" },
                new EncounterChoiceDefinition { id = "goblin", requiredTag = "lineage.goblin", aetherDelta = 3, salvageDelta = 3 });
            Event(result, "pressure_front", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "medic", suppliesCost = 1, moraleDelta = 2 },
                new EncounterChoiceDefinition { id = "endure", survivorDelta = -8, moraleDelta = -3 },
                new EncounterChoiceDefinition { id = "climb", aetherCost = 1 });
            Event(result, "thunder_leviathan_pod", EncounterType.Storm, new int[] { 2, 5 },
                new EncounterChoiceDefinition { id = "hide", aetherDelta = 1, moraleDelta = 5, successChance = 0.6f, failureChoiceId = "hide_fail" },
                new EncounterChoiceDefinition { id = "hide_fail", hidden = true, hullDelta = -6 },
                new EncounterChoiceDefinition { id = "avian", requiredTag = "lineage.avian", moraleDelta = 8, aetherDelta = 1 },
                new EncounterChoiceDefinition { id = "avoid", aetherCost = 1 });
            Event(result, "eye_of_silence", EncounterType.Storm, new int[] {  },
                new EncounterChoiceDefinition { id = "rest", suppliesCost = 1, moraleDelta = 6, instabilityDelta = -15 },
                new EncounterChoiceDefinition { id = "repair", salvageCost = 4, hullDelta = 6 },
                new EncounterChoiceDefinition { id = "move" });
        }

        private static void Event(Dictionary<string, EncounterDefinition> result, string id, EncounterType type, int[] regions, params EncounterChoiceDefinition[] choices)
        {
            var encounter = new EncounterDefinition { id = id, type = type, regions = regions, titleKey = "enc." + id + ".title", bodyKey = "enc." + id + ".body" };
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
