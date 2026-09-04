using System.Collections.Generic;
using AetherArk.Content;

namespace AetherArk.Core
{
    /// <summary>Progress-based unlocks. Pure functions over the profile so the rules are testable.</summary>
    public static class UnlockRules
    {
        public const string DefaultFlagship = "ship_vanguard";

        public static bool IsFlagshipUnlocked(ProfileState profile, string flagshipId)
        {
            if (profile == null || ContentCatalog.GetFlagship(flagshipId) == null) return false;
            switch (flagshipId)
            {
                case "ship_vanguard": return true;
                case "ship_bastion": return profile.tutorialSeen;
                case "ship_zephyr": return profile.campaignVictories >= 1;
                default: return false;
            }
        }

        public static List<string> UnlockedFlagships(ProfileState profile)
        {
            var result = new List<string>();
            foreach (var id in ContentCatalog.FlagshipIds())
                if (IsFlagshipUnlocked(profile, id)) result.Add(id);
            return result;
        }

        /// <summary>The flagship a new run should actually use: the chosen one if unlocked, otherwise the vanguard; tutorials always sail the vanguard.</summary>
        public static string ResolveFlagship(ProfileState profile)
        {
            if (profile == null || !profile.tutorialSeen) return DefaultFlagship;
            return IsFlagshipUnlocked(profile, profile.flagshipId) ? profile.flagshipId : DefaultFlagship;
        }

        /// <summary>Records a finished victorious run on the profile: the tutorial completion and full-campaign victories.</summary>
        public static void RecordVictory(ProfileState profile, RunState state)
        {
            if (profile == null || state == null || state.phase != GamePhase.Victory) return;
            if (state.isFirstExpedition) profile.tutorialSeen = true;
            if (state.regionCount > 1 && state.regionIndex >= state.regionCount) profile.campaignVictories++;
        }
    }
}
