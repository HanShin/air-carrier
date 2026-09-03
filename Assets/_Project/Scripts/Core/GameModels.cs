using System;
using System.Collections.Generic;

namespace AetherArk.Core
{
    public enum GamePhase { MainMenu, RouteMap, Encounter, Combat, Victory, Defeat, Port }
    public enum ModuleCategory { Hull, Core, Weapons, Deck, Sensors, Engineering, Bridge, Marines }
    public enum Difficulty { Story, Standard, Harsh }
    public enum Language { Korean, English }
    public enum AltitudeBand { Low, Medium, High }
    public enum WeatherType { Clear, Thunderhead, Turbulence, AetherCurrent, Icing, CloudCover }
    public enum ShipSystemType { Bridge, AetherCore, LiftArray, Engines, Ward, Weapons, FlightDeck, Sensors, Infirmary, LifeSupport }
    public enum CrewLineage { Human, Elf, Dwarf, Orc, Goblin, Avian }
    public enum CrewRole { Captain, Resonator, Engineer, Gunner, Pilot, Medic, Marine, Navigator }
    public enum SquadronType { Interceptor, Bomber, Escort, Recon, Assault }
    public enum SquadronMission { None, Intercept, Bombard, Escort, Recon, Assault, Recall }
    public enum SquadronStatus { Ready, Launching, OnMission, Recovering, Destroyed }
    public enum AlertSeverity { Info, Warning, Critical }
    public enum SupportShipType { Hospital, Workshop, Pathfinder }
    public enum EncounterType { Start, Battle, EliteBattle, Rescue, Salvage, Trade, Checkpoint, Storm, Gate }
    public enum DefeatReason { None, FlagshipDestroyed, CaptainLost, ConvoyLost, MoraleCollapsed }

    [Serializable]
    public sealed class AccessibilitySettings
    {
        public float uiScale = 1f;
        public float combatSpeed = 1f;
        public bool autoPauseOnWarning = true;
        public bool reducedMotion;
        public bool highContrast;
        public string pauseKey = "Space";
    }

    [Serializable]
    public sealed class ProfileState
    {
        public int schemaVersion = 1;
        public Language language = Language.Korean;
        public Difficulty difficulty = Difficulty.Story;
        public string captainName = "아린";
        public CrewLineage captainLineage = CrewLineage.Human;
        public SupportShipType supportShip = SupportShipType.Workshop;
        public bool tutorialSeen;
        public AccessibilitySettings accessibility = new AccessibilitySettings();
        public List<string> unlocks = new List<string> { "ship_vanguard", "squad_interceptor", "squad_bomber" };
    }

    [Serializable]
    public sealed class StrategicResources
    {
        public int aether = 16;
        public int supplies = 12;
        public int ordnance = 8;
        public int salvage = 20;
    }

    [Serializable]
    public sealed class ConvoyState
    {
        public int survivors = 1200;
        public int morale = 72;
        public SupportShipType supportShip = SupportShipType.Workshop;
        public int supportIntegrity = 100;
        public int supportCooldown;

        public bool HasCollapsed => survivors <= 0 || morale <= 0;
    }

    [Serializable]
    public sealed class ShipSystemState
    {
        public ShipSystemType type;
        public string displayKey;
        public int power;
        public int maxPower;
        public float damage;
        public float maxDamage = 100f;
        public float disabledSeconds;
        public float overchargeSeconds;

        public bool IsOperational => damage < maxDamage && disabledSeconds <= 0f;
        public int EffectivePower => IsOperational ? power + (overchargeSeconds > 0f ? 2 : 0) : 0;
        public float Integrity => Math.Max(0f, 1f - damage / Math.Max(1f, maxDamage));
    }

    [Serializable]
    public sealed class RoomState
    {
        public string id;
        public ShipSystemType system;
        public float fire;
        public float breach;
        public float oxygen = 100f;
        public int intruders;
        public float intruderProgress;
    }

    [Serializable]
    public sealed class ShipState
    {
        public string id;
        public string displayName;
        public string nameKey;
        public bool boardingCapable;
        public int moduleSlots = 4;
        public float hull;
        public float maxHull;
        public float armor;
        public float maxArmor;
        public float ward;
        public float maxWard;
        public int coreOutput;
        public float instability;
        public float wardRechargeSeconds;
        public AltitudeBand altitude = AltitudeBand.Medium;
        public List<ShipSystemState> systems = new List<ShipSystemState>();
        public List<RoomState> rooms = new List<RoomState>();

        public bool IsDestroyed => hull <= 0f;

        public ShipSystemState GetSystem(ShipSystemType type)
        {
            return systems.Find(system => system.type == type);
        }

        public RoomState GetRoom(ShipSystemType type)
        {
            return rooms.Find(room => room.system == type);
        }

        public int AllocatedPower()
        {
            var total = 0;
            for (var i = 0; i < systems.Count; i++) total += systems[i].power;
            return total;
        }
    }

    [Serializable]
    public sealed class CrewState
    {
        public string id;
        public string displayName;
        public CrewLineage lineage;
        public CrewRole role;
        public string backgroundKey;
        public string traitKey;
        public ShipSystemType currentRoom;
        public float health;
        public float maxHealth;
        public float downedSeconds;
        public bool isDead;
        public bool isCaptain;
        public bool onSortie;
        public int skillLevel = 1;

        public bool IsActive => !isDead && health > 0f && !onSortie;
        public bool IsDowned => !isDead && health <= 0f;
    }

    [Serializable]
    public sealed class SquadronState
    {
        public string id;
        public string displayKey;
        public SquadronType type;
        public SquadronMission mission;
        public SquadronStatus status = SquadronStatus.Ready;
        public int strength;
        public int maxStrength;
        public int ordnanceCost;
        public float missionTimer;
        public float phaseDuration;
        public string pilotCrewId;
        public ShipSystemType targetSystem = ShipSystemType.Weapons;

        public bool CanLaunch => status == SquadronStatus.Ready && strength > 0;
    }

    [Serializable]
    public sealed class RouteNodeState
    {
        public string id;
        public string nameKey;
        public int column;
        public int lane;
        public int aetherCost = 1;
        public AltitudeBand recommendedAltitude = AltitudeBand.Medium;
        public WeatherType weather = WeatherType.Clear;
        public EncounterType encounterType;
        public string encounterId;
        public bool visited;
        public bool blocked;
        public List<string> connectedIds = new List<string>();
    }

    [Serializable]
    public sealed class EncounterChoiceDefinition
    {
        public string id;
        public string textKey;
        public string resultKey;
        public int aetherCost;
        public int suppliesCost;
        public int ordnanceCost;
        public int salvageCost;
        public int aetherDelta;
        public int suppliesDelta;
        public int ordnanceDelta;
        public int salvageDelta;
        public int survivorDelta;
        public int moraleDelta;
        public bool startsBattle;
        public string requiredTag;
        public float hullDelta;
        public float armorDelta;
        public float instabilityDelta;
        public bool refitSquadrons;
        public int battleTier = 1;
        /// <summary>1 = deterministic. Below 1 the choice is a gamble resolved on the events RNG stream.</summary>
        public float successChance = 1f;
        /// <summary>Hidden choice in the same encounter applied when the gamble fails.</summary>
        public string failureChoiceId;
        /// <summary>Never shown or selectable; exists as a failure outcome.</summary>
        public bool hidden;
    }

    [Serializable]
    public sealed class EncounterDefinition
    {
        public string id;
        public string titleKey;
        public string bodyKey;
        public EncounterType type;
        public List<EncounterChoiceDefinition> choices = new List<EncounterChoiceDefinition>();
    }

    [Serializable]
    public sealed class ModuleDefinition
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
        public ModuleCategory category;
        public int tier = 1;
        public int cost = 8;
        // Flat bonuses applied on install.
        public float maxHull;
        public float maxArmor;
        public float maxWard;
        public int coreOutput;
        public int squadronStrength;
        public float crewHealth;
        // Multipliers (1 = no change) and bonuses consulted by the rules through ModuleRules.Modifiers.
        public float weaponDamage = 1f;
        public float weaponCooldown = 1f;
        public float accuracy;
        public bool weatherResistance;
        public float wardRegen = 1f;
        public float repairRate = 1f;
        public float healRate = 1f;
        public float autoRepair;
        public bool fireResistance;
        public bool oxygenReserve;
        public float squadronTime = 1f;
        public int interceptCharges;
        public float reconSeconds;
        public int salvageReward;
        public bool aetherDiscount;
        public float boardingDefense = 1f;
        public float assaultBonus;
        public float instabilityDecay = 1f;
    }

    [Serializable]
    public sealed class RegionDefinition
    {
        public string id;
        public string nameKey;
        public int index;
        /// <summary>Weights in WeatherType enum order (Clear, Thunderhead, Turbulence, AetherCurrent, Icing, CloudCover).</summary>
        public int[] weatherWeights;
        /// <summary>Weights for Battle, Rescue, Salvage, Trade, Checkpoint, Storm on rollable columns.</summary>
        public int[] encounterWeights;
        public float enemyStatMultiplier = 1f;
        public float extraAetherCostChance = 0.2f;
    }

    [Serializable]
    public sealed class WeatherProfile
    {
        public WeatherType type;
        public string nameKey;
        public float accuracyModifier;
        public float wardRegenModifier = 1f;
        public float squadronTimeModifier = 1f;
        public float breachDamageModifier = 1f;
        public float hazardInterval = 9f;
    }

    [Serializable]
    public sealed class CombatLogEntry
    {
        public string key;
        public string argument;

        public CombatLogEntry() { }

        public CombatLogEntry(string keyValue, string argumentValue = "")
        {
            key = keyValue;
            argument = argumentValue;
        }
    }

    [Serializable]
    public sealed class RandomStreamsState
    {
        public uint route;
        public uint combat;
        public uint events;
    }

    [Serializable]
    public sealed class RunState
    {
        public int schemaVersion = 1;
        public int seed;
        public bool isFirstExpedition;
        public GamePhase phase = GamePhase.RouteMap;
        public Difficulty difficulty;
        public int regionIndex = 1;
        /// <summary>Regions in this campaign: 1 for the locked first expedition, otherwise the full roster.</summary>
        public int regionCount = 1;
        public int travelCount;
        public int totalTravelCount;
        public int stormColumn = -1;
        public string currentNodeId = "n0_1";
        public string activeEncounterId;
        public List<string> installedModules = new List<string>();
        public StrategicResources resources = new StrategicResources();
        public ConvoyState convoy = new ConvoyState();
        public ShipState playerShip;
        public ShipState enemyShip;
        public List<CrewState> crew = new List<CrewState>();
        public List<SquadronState> squadrons = new List<SquadronState>();
        public List<RouteNodeState> routeNodes = new List<RouteNodeState>();
        public RandomStreamsState random = new RandomStreamsState();
        public WeatherType currentWeather = WeatherType.Clear;
        public bool isPaused;
        public bool autoPauseOnWarning = true;
        public float combatElapsed;
        public float playerWeaponCooldown;
        public float enemyWeaponCooldown;
        public float enemySquadronCooldown;
        public float altitudeCooldown;
        public float weatherHazardTimer;
        public int interceptCharges;
        public float reconBonusSeconds;
        public ShipSystemType selectedEnemySystem = ShipSystemType.Weapons;
        public DefeatReason defeatReason;
        public bool isFinalBattle;
        public bool hasChangedPower;
        public bool hasFiredWeapon;
        public bool hasLaunchedSquadron;
        public bool hasMovedCrew;
        public string combatAlertKey;
        public string combatAlertArgument;
        public AlertSeverity combatAlertSeverity;
        public float combatAlertSeconds;
        public bool combatAlertPausedBattle;
        public List<CombatLogEntry> combatLog = new List<CombatLogEntry>();
    }
}
