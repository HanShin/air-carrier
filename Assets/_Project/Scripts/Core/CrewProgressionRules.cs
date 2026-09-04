using System;
using System.Collections.Generic;

namespace AetherArk.Core
{
    public static class CrewProgressionRules
    {
        public const int MaxActiveCrew = 8;
        public const int MaxSkillLevel = 3;

        public static int ExperienceNeeded(int skillLevel)
        {
            if (skillLevel <= 1) return 4;
            if (skillLevel == 2) return 8;
            return 0;
        }

        public static bool AddExperience(CrewState crew, int amount)
        {
            if (crew == null || crew.isDead || amount <= 0 || crew.skillLevel >= MaxSkillLevel) return false;
            crew.skillLevel = Math.Max(1, crew.skillLevel);
            crew.experience += amount;
            var leveled = false;
            while (crew.skillLevel < MaxSkillLevel)
            {
                var needed = ExperienceNeeded(crew.skillLevel);
                if (crew.experience < needed) break;
                crew.experience -= needed;
                crew.skillLevel++;
                leveled = true;
            }
            if (crew.skillLevel >= MaxSkillLevel) crew.experience = 0;
            return leveled;
        }

        public static List<CrewState> AwardCombatExperience(List<CrewState> crew)
        {
            var leveled = new List<CrewState>();
            if (crew == null) return leveled;
            for (var i = 0; i < crew.Count; i++)
            {
                if (crew[i].isDead) continue;
                if (AddExperience(crew[i], 1)) leveled.Add(crew[i]);
            }
            return leveled;
        }

        public static int ActiveCrewCount(List<CrewState> crew)
        {
            if (crew == null) return 0;
            var count = 0;
            for (var i = 0; i < crew.Count; i++) if (!crew[i].isDead) count++;
            return count;
        }

        public static float SkillMultiplier(CrewState crew, float bonusPerLevel = 0.1f)
        {
            return 1f + Math.Max(0, (crew?.skillLevel ?? 1) - 1) * bonusPerLevel;
        }

        public static float RiskMultiplier(CrewState crew, float reductionPerLevel = 0.08f)
        {
            return Math.Max(0.7f, 1f - Math.Max(0, (crew?.skillLevel ?? 1) - 1) * reductionPerLevel);
        }
    }
}
