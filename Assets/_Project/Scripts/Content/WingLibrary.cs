using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    /// <summary>Air wings. Generated from tools/gen_wings.py; edit the table, not this file.</summary>
    public static class WingLibrary
    {
        public static void AddAll(Dictionary<string, WingDefinition> result)
        {
            result["kestrel_interceptors"] = new WingDefinition { id = "kestrel_interceptors", nameKey = "wing.kestrel_interceptors", descriptionKey = "wing.kestrel_interceptors.desc", type = SquadronType.Interceptor, tier = 1, cost = 12, strength = 4, ordnanceCost = 1, interceptCharges = 2 };
            result["ember_bombers"] = new WingDefinition { id = "ember_bombers", nameKey = "wing.ember_bombers", descriptionKey = "wing.ember_bombers.desc", type = SquadronType.Bomber, tier = 1, cost = 12, strength = 3, ordnanceCost = 2 };
            result["gale_lancers"] = new WingDefinition { id = "gale_lancers", nameKey = "wing.gale_lancers", descriptionKey = "wing.gale_lancers.desc", type = SquadronType.Interceptor, tier = 2, cost = 20, strength = 4, ordnanceCost = 1, interceptCharges = 3, lossResistance = 0.8f };
            result["ghost_kites"] = new WingDefinition { id = "ghost_kites", nameKey = "wing.ghost_kites", descriptionKey = "wing.ghost_kites.desc", type = SquadronType.Interceptor, tier = 3, cost = 30, strength = 5, ordnanceCost = 1, interceptCharges = 2, missionTime = 0.7f, lossResistance = 0.7f };
            result["thunder_bombers"] = new WingDefinition { id = "thunder_bombers", nameKey = "wing.thunder_bombers", descriptionKey = "wing.thunder_bombers.desc", type = SquadronType.Bomber, tier = 2, cost = 20, strength = 3, ordnanceCost = 3, bombardDamage = 1.5f, bombardFire = 20 };
            result["sky_wardens"] = new WingDefinition { id = "sky_wardens", nameKey = "wing.sky_wardens", descriptionKey = "wing.sky_wardens.desc", type = SquadronType.Escort, tier = 2, cost = 20, strength = 3, ordnanceCost = 1, escortWard = 8, escortCharges = 2 };
            result["far_eyes"] = new WingDefinition { id = "far_eyes", nameKey = "wing.far_eyes", descriptionKey = "wing.far_eyes.desc", type = SquadronType.Recon, tier = 1, cost = 12, strength = 2, ordnanceCost = 0, reconSeconds = 25, lossResistance = 0.5f };
            result["storm_marines"] = new WingDefinition { id = "storm_marines", nameKey = "wing.storm_marines", descriptionKey = "wing.storm_marines.desc", type = SquadronType.Assault, tier = 2, cost = 20, strength = 3, ordnanceCost = 2, assaultSabotage = 48, assaultHull = 3 };
            result["ruin_dropships"] = new WingDefinition { id = "ruin_dropships", nameKey = "wing.ruin_dropships", descriptionKey = "wing.ruin_dropships.desc", type = SquadronType.Assault, tier = 3, cost = 30, strength = 4, ordnanceCost = 3, assaultSabotage = 64, assaultHull = 5 };
        }
    }
}
