using System;

namespace AetherArk.Core
{
    /// <summary>Pure helpers shared by the route map view and its tests.</summary>
    public static class RouteRules
    {
        /// <summary>The storm column after one more jump; mirrors the closure rule in TravelTo.</summary>
        public static int NextStormColumn(RunState state)
        {
            if (state == null) return -1;
            return Math.Max(-1, state.travelCount + 1 - 3);
        }

        public static string Glyph(EncounterType type)
        {
            switch (type)
            {
                case EncounterType.Battle: return "▲";
                case EncounterType.EliteBattle: return "▲";
                case EncounterType.Rescue: return "+";
                case EncounterType.Salvage: return "◆";
                case EncounterType.Trade: return "$";
                case EncounterType.Checkpoint: return "▼";
                case EncounterType.Storm: return "◇";
                case EncounterType.Gate: return "★";
                default: return "●";
            }
        }

        public static string NameKey(EncounterType type)
        {
            return type == EncounterType.Start ? "node.departure" : "node." + type.ToString().ToLowerInvariant();
        }

        public static bool IsHostile(EncounterType type)
        {
            return type == EncounterType.Battle || type == EncounterType.EliteBattle || type == EncounterType.Gate;
        }
    }
}
