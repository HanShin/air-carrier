using System.Collections.Generic;

namespace AetherArk.Core
{
    public sealed class CrewIdentityDefinition
    {
        public string id;
        public string descriptionKey;
        public float repairMultiplier = 1f;
        public float fireDamageMultiplier = 1f;
        public float intruderDamageMultiplier = 1f;
        public float oxygenDamageMultiplier = 1f;
        public float weatherDamageMultiplier = 1f;
        public float boardingMultiplier = 1f;
        public float rescueWindowBonus;
        public float rescueAidTimerMultiplier = 1f;
        public float sortieTimeMultiplier = 1f;
        public float sortieLossMultiplier = 1f;
        public float overchargeInstabilityMultiplier = 1f;
        public float overchargeAccidentMultiplier = 1f;
        public float infirmaryAuraMultiplier = 1f;
    }

    public struct CrewIdentityModifiers
    {
        public float repairMultiplier;
        public float fireDamageMultiplier;
        public float intruderDamageMultiplier;
        public float oxygenDamageMultiplier;
        public float weatherDamageMultiplier;
        public float boardingMultiplier;
        public float rescueWindowBonus;
        public float rescueAidTimerMultiplier;
        public float sortieTimeMultiplier;
        public float sortieLossMultiplier;
        public float overchargeInstabilityMultiplier;
        public float overchargeAccidentMultiplier;
        public float infirmaryAuraMultiplier;

        public static CrewIdentityModifiers None => new CrewIdentityModifiers
        {
            repairMultiplier = 1f,
            fireDamageMultiplier = 1f,
            intruderDamageMultiplier = 1f,
            oxygenDamageMultiplier = 1f,
            weatherDamageMultiplier = 1f,
            boardingMultiplier = 1f,
            rescueAidTimerMultiplier = 1f,
            sortieTimeMultiplier = 1f,
            sortieLossMultiplier = 1f,
            overchargeInstabilityMultiplier = 1f,
            overchargeAccidentMultiplier = 1f,
            infirmaryAuraMultiplier = 1f
        };
    }

    /// <summary>Small, stackable rules for authored crew traits and professional backgrounds.</summary>
    public static class TraitRules
    {
        private static readonly Dictionary<string, CrewIdentityDefinition> Traits =
            new Dictionary<string, CrewIdentityDefinition>
            {
                { "trait.steadfast", Define("trait.steadfast", fire: 0.9f, intruder: 0.9f, rescueWindow: 2f) },
                { "trait.attuned", Define("trait.attuned", instability: 0.85f, accident: 0.8f) },
                { "trait.fireproof", Define("trait.fireproof", fire: 0.35f) },
                { "trait.quick_hands", Define("trait.quick_hands", repair: 1.12f) },
                { "trait.light_step", Define("trait.light_step", oxygen: 0.7f) },
                { "trait.rescuer", Define("trait.rescuer", rescueAid: 0.5f) }
            };

        private static readonly Dictionary<string, CrewIdentityDefinition> Backgrounds =
            new Dictionary<string, CrewIdentityDefinition>
            {
                { "background.exile", Define("background.exile", fire: 0.95f, intruder: 0.95f, boarding: 1.08f) },
                { "background.weather_scholar", Define("background.weather_scholar", weather: 0.75f) },
                { "background.dockwright", Define("background.dockwright", repair: 1.12f) },
                { "background.deck_runner", Define("background.deck_runner", sortieLoss: 0.9f) },
                { "background.cloud_medic", Define("background.cloud_medic", rescueAid: 0.75f, infirmaryAura: 1.25f) },
                { "background.border_guard", Define("background.border_guard", intruder: 0.9f, boarding: 1.15f) }
            };

        public static IEnumerable<CrewIdentityDefinition> AllTraits => Traits.Values;
        public static IEnumerable<CrewIdentityDefinition> AllBackgrounds => Backgrounds.Values;

        public static CrewIdentityDefinition GetTrait(string id)
        {
            return !string.IsNullOrEmpty(id) && Traits.TryGetValue(id, out var definition) ? definition : null;
        }

        public static CrewIdentityDefinition GetBackground(string id)
        {
            return !string.IsNullOrEmpty(id) && Backgrounds.TryGetValue(id, out var definition) ? definition : null;
        }

        public static CrewIdentityModifiers Modifiers(CrewState crew)
        {
            var result = CrewIdentityModifiers.None;
            if (crew == null) return result;
            Apply(ref result, GetTrait(crew.traitKey));
            Apply(ref result, GetBackground(crew.backgroundKey));
            return result;
        }

        private static CrewIdentityDefinition Define(string id, float repair = 1f, float fire = 1f, float intruder = 1f,
            float oxygen = 1f, float weather = 1f, float boarding = 1f, float rescueWindow = 0f, float rescueAid = 1f,
            float sortieTime = 1f, float sortieLoss = 1f, float instability = 1f, float accident = 1f, float infirmaryAura = 1f)
        {
            return new CrewIdentityDefinition
            {
                id = id,
                descriptionKey = id + ".desc",
                repairMultiplier = repair,
                fireDamageMultiplier = fire,
                intruderDamageMultiplier = intruder,
                oxygenDamageMultiplier = oxygen,
                weatherDamageMultiplier = weather,
                boardingMultiplier = boarding,
                rescueWindowBonus = rescueWindow,
                rescueAidTimerMultiplier = rescueAid,
                sortieTimeMultiplier = sortieTime,
                sortieLossMultiplier = sortieLoss,
                overchargeInstabilityMultiplier = instability,
                overchargeAccidentMultiplier = accident,
                infirmaryAuraMultiplier = infirmaryAura
            };
        }

        private static void Apply(ref CrewIdentityModifiers result, CrewIdentityDefinition definition)
        {
            if (definition == null) return;
            result.repairMultiplier *= definition.repairMultiplier;
            result.fireDamageMultiplier *= definition.fireDamageMultiplier;
            result.intruderDamageMultiplier *= definition.intruderDamageMultiplier;
            result.oxygenDamageMultiplier *= definition.oxygenDamageMultiplier;
            result.weatherDamageMultiplier *= definition.weatherDamageMultiplier;
            result.boardingMultiplier *= definition.boardingMultiplier;
            result.rescueWindowBonus += definition.rescueWindowBonus;
            result.rescueAidTimerMultiplier *= definition.rescueAidTimerMultiplier;
            result.sortieTimeMultiplier *= definition.sortieTimeMultiplier;
            result.sortieLossMultiplier *= definition.sortieLossMultiplier;
            result.overchargeInstabilityMultiplier *= definition.overchargeInstabilityMultiplier;
            result.overchargeAccidentMultiplier *= definition.overchargeAccidentMultiplier;
            result.infirmaryAuraMultiplier *= definition.infirmaryAuraMultiplier;
        }
    }
}
