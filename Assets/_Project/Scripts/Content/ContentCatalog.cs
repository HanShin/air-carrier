using System;
using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    public static class ContentCatalog
    {
        private static readonly Dictionary<string, EncounterDefinition> Encounters = BuildEncounters();

        public static ShipState CreateVanguard()
        {
            var ship = new ShipState
            {
                id = "ship_vanguard",
                displayName = "EAS Dawn Refuge",
                hull = 32f,
                maxHull = 32f,
                armor = 18f,
                maxArmor = 18f,
                ward = 12f,
                maxWard = 12f,
                coreOutput = 12,
                altitude = AltitudeBand.Medium
            };

            AddSystem(ship, ShipSystemType.Bridge, "system.bridge", 1, 2);
            AddSystem(ship, ShipSystemType.AetherCore, "system.core", 0, 0);
            AddSystem(ship, ShipSystemType.LiftArray, "system.lift", 1, 3);
            AddSystem(ship, ShipSystemType.Engines, "system.engines", 2, 4);
            AddSystem(ship, ShipSystemType.Ward, "system.ward", 2, 4);
            AddSystem(ship, ShipSystemType.Weapons, "system.weapons", 2, 4);
            AddSystem(ship, ShipSystemType.FlightDeck, "system.deck", 1, 3);
            AddSystem(ship, ShipSystemType.Sensors, "system.sensors", 1, 3);
            AddSystem(ship, ShipSystemType.Infirmary, "system.infirmary", 0, 2);
            AddSystem(ship, ShipSystemType.LifeSupport, "system.life", 1, 2);
            return ship;
        }

        private sealed class EnemyDefinition
        {
            public string id;
            public string displayName;
            public int tier;
            public int weight;
            public float hull, armor, ward;
            public int coreOutput;
            public bool boarding;
            public string[] weapons;
            // Power per system in enum order: Bridge, AetherCore, LiftArray, Engines, Ward, Weapons, FlightDeck, Sensors, Infirmary, LifeSupport.
            public int[] power;
            public int[] maxPower;
        }

        private static readonly EnemyDefinition[] EnemyRoster =
        {
            new EnemyDefinition
            {
                id = "enemy_cutter", weapons = new[] { "aether_cannon" }, displayName = "Imperial Pursuit Cutter", tier = 1, weight = 40,
                hull = 24f, armor = 10f, ward = 8f, coreOutput = 9,
                power = new[] { 1, 0, 1, 2, 1, 3, 0, 0, 0, 1 }, maxPower = new[] { 2, 0, 3, 3, 3, 4, 2, 2, 1, 2 }
            },
            new EnemyDefinition
            {
                id = "enemy_carrier", weapons = new[] { "flak_battery" }, displayName = "Imperial Strike Carrier", tier = 1, weight = 20,
                hull = 28f, armor = 12f, ward = 10f, coreOutput = 10,
                power = new[] { 1, 0, 1, 1, 1, 1, 3, 1, 0, 1 }, maxPower = new[] { 2, 0, 3, 3, 3, 3, 4, 2, 1, 2 }
            },
            new EnemyDefinition
            {
                id = "enemy_scout", weapons = new[] { "ward_lance" }, displayName = "Imperial Scout Frigate", tier = 1, weight = 20,
                hull = 20f, armor = 8f, ward = 8f, coreOutput = 11,
                power = new[] { 1, 0, 1, 3, 1, 2, 0, 2, 0, 1 }, maxPower = new[] { 2, 0, 3, 4, 3, 3, 1, 3, 1, 2 }
            },
            new EnemyDefinition
            {
                id = "enemy_boarder", weapons = new[] { "ember_mortar" }, displayName = "Imperial Boarding Barge", tier = 1, weight = 20, boarding = true,
                hull = 26f, armor = 12f, ward = 6f, coreOutput = 9,
                power = new[] { 1, 0, 1, 1, 1, 1, 2, 0, 1, 1 }, maxPower = new[] { 2, 0, 3, 3, 3, 3, 3, 2, 1, 2 }
            },
            new EnemyDefinition
            {
                id = "enemy_cruiser", weapons = new[] { "heavy_cannon" }, displayName = "Imperial Storm Cruiser", tier = 2, weight = 60,
                hull = 34f, armor = 18f, ward = 12f, coreOutput = 11,
                power = new[] { 1, 0, 1, 2, 2, 3, 1, 0, 0, 1 }, maxPower = new[] { 2, 0, 3, 3, 3, 4, 2, 2, 1, 2 }
            },
            new EnemyDefinition
            {
                id = "enemy_monitor", weapons = new[] { "bolt_thrower" }, displayName = "Imperial Bulwark Monitor", tier = 2, weight = 40,
                hull = 30f, armor = 22f, ward = 16f, coreOutput = 11,
                power = new[] { 1, 0, 1, 1, 3, 2, 0, 2, 0, 1 }, maxPower = new[] { 2, 0, 3, 3, 4, 4, 1, 3, 1, 2 }
            }
        };

        private static readonly ShipSystemType[] SystemOrder =
        {
            ShipSystemType.Bridge, ShipSystemType.AetherCore, ShipSystemType.LiftArray, ShipSystemType.Engines, ShipSystemType.Ward,
            ShipSystemType.Weapons, ShipSystemType.FlightDeck, ShipSystemType.Sensors, ShipSystemType.Infirmary, ShipSystemType.LifeSupport
        };

        private static readonly string[] SystemKeys =
        {
            "system.bridge", "system.core", "system.lift", "system.engines", "system.ward",
            "system.weapons", "system.deck", "system.sensors", "system.infirmary", "system.life"
        };

        /// <summary>
        /// Picks an enemy for the battle tier. Without variants only the baseline cutter/cruiser can appear,
        /// which keeps the locked first expedition byte-for-byte reproducible.
        /// </summary>
        public static ShipState CreateEnemy(int tier, bool allowVariants, ref uint random)
        {
            var effectiveTier = tier >= 2 ? 2 : 1;
            var chosen = effectiveTier == 2 ? EnemyRoster[4] : EnemyRoster[0];
            if (allowVariants)
            {
                var total = 0;
                for (var i = 0; i < EnemyRoster.Length; i++) if (EnemyRoster[i].tier == effectiveTier) total += EnemyRoster[i].weight;
                var roll = SeededRandom.Range(ref random, 0, total);
                for (var i = 0; i < EnemyRoster.Length; i++)
                {
                    if (EnemyRoster[i].tier != effectiveTier) continue;
                    if (roll < EnemyRoster[i].weight) { chosen = EnemyRoster[i]; break; }
                    roll -= EnemyRoster[i].weight;
                }
            }
            return Build(chosen, ref random);
        }

        public static ShipState CreateEnemyById(string id, ref uint random)
        {
            for (var i = 0; i < EnemyRoster.Length; i++)
                if (EnemyRoster[i].id == id) return Build(EnemyRoster[i], ref random);
            return null;
        }

        public static IEnumerable<string> EnemyIds()
        {
            for (var i = 0; i < EnemyRoster.Length; i++) yield return EnemyRoster[i].id;
        }

        private static ShipState Build(EnemyDefinition definition, ref uint random)
        {
            var ship = new ShipState
            {
                id = definition.id,
                displayName = definition.displayName,
                nameKey = "ship." + definition.id,
                hull = definition.hull,
                maxHull = definition.hull,
                armor = definition.armor,
                maxArmor = definition.armor,
                ward = definition.ward,
                maxWard = definition.ward,
                coreOutput = definition.coreOutput,
                boardingCapable = definition.boarding,
                altitude = (AltitudeBand)SeededRandom.Range(ref random, 0, 3)
            };
            for (var i = 0; i < SystemOrder.Length; i++)
                AddSystem(ship, SystemOrder[i], SystemKeys[i], definition.power[i], definition.maxPower[i]);
            if (definition.weapons != null)
                for (var i = 0; i < definition.weapons.Length; i++) ship.weaponSlots.Add(new WeaponSlotState { weaponId = definition.weapons[i] });
            ship.weaponHardpoints = Math.Max(2, ship.weaponSlots.Count);
            return ship;
        }

        private static readonly Dictionary<string, WeaponDefinition> Weapons = BuildWeapons();

        private static Dictionary<string, WeaponDefinition> BuildWeapons()
        {
            var result = new Dictionary<string, WeaponDefinition>();
            WeaponLibrary.AddAll(result);
            return result;
        }

        public static WeaponDefinition GetWeapon(string id)
        {
            return !string.IsNullOrEmpty(id) && Weapons.TryGetValue(id, out var weapon) ? weapon : null;
        }

        public static List<string> WeaponIds()
        {
            return new List<string>(Weapons.Keys);
        }

        public const string StartingWeapon = "aether_cannon";
        public const string StartingSecondary = "ward_lance";

        /// <summary>Two distinct weapons for a port, deterministic per seed and region, tiers at most regionIndex + 1.</summary>
        public static List<string> OfferWeapons(int seed, int regionIndex, List<WeaponSlotState> mounted)
        {
            var random = SeededRandom.Seed(unchecked(seed + regionIndex * 7727), 0x5EA9u);
            var pool = new List<string>();
            var weights = new List<int>();
            foreach (var pair in Weapons)
            {
                var alreadyMounted = false;
                if (mounted != null) for (var i = 0; i < mounted.Count; i++) if (mounted[i].weaponId == pair.Key) alreadyMounted = true;
                if (alreadyMounted || pair.Value.tier > regionIndex + 1) continue;
                pool.Add(pair.Key);
                weights.Add(pair.Value.tier <= regionIndex ? 3 : 1);
            }
            var offers = new List<string>();
            while (offers.Count < 2 && pool.Count > 0)
            {
                var total = 0;
                for (var i = 0; i < weights.Count; i++) total += weights[i];
                var roll = SeededRandom.Range(ref random, 0, total);
                var index = 0;
                for (; index < weights.Count; index++)
                {
                    if (roll < weights[index]) break;
                    roll -= weights[index];
                }
                if (index >= pool.Count) index = pool.Count - 1;
                offers.Add(pool[index]);
                pool.RemoveAt(index);
                weights.RemoveAt(index);
            }
            return offers;
        }

        private static readonly Dictionary<string, ModuleDefinition> Modules = BuildModules();

        private static Dictionary<string, ModuleDefinition> BuildModules()
        {
            var result = new Dictionary<string, ModuleDefinition>();
            ModuleLibrary.AddAll(result);
            return result;
        }

        public static ModuleDefinition GetModule(string id)
        {
            return !string.IsNullOrEmpty(id) && Modules.TryGetValue(id, out var module) ? module : null;
        }

        public static List<string> ModuleIds()
        {
            return new List<string>(Modules.Keys);
        }

        /// <summary>
        /// Three distinct uninstalled modules for a port, deterministic per seed and region.
        /// Tiers above regionIndex + 1 are excluded, and higher tiers are weighted down while still possible.
        /// </summary>
        public static List<string> OfferModules(int seed, int regionIndex, List<string> installed)
        {
            var random = SeededRandom.Seed(unchecked(seed + regionIndex * 31337), 0x0FF37u);
            var pool = new List<string>();
            var weights = new List<int>();
            foreach (var pair in Modules)
            {
                if (installed != null && installed.Contains(pair.Key)) continue;
                if (pair.Value.tier > regionIndex + 1) continue;
                pool.Add(pair.Key);
                weights.Add(pair.Value.tier <= regionIndex ? 3 : 1);
            }
            var offers = new List<string>();
            while (offers.Count < 3 && pool.Count > 0)
            {
                var total = 0;
                for (var i = 0; i < weights.Count; i++) total += weights[i];
                var roll = SeededRandom.Range(ref random, 0, total);
                var index = 0;
                for (; index < weights.Count; index++)
                {
                    if (roll < weights[index]) break;
                    roll -= weights[index];
                }
                if (index >= pool.Count) index = pool.Count - 1;
                offers.Add(pool[index]);
                pool.RemoveAt(index);
                weights.RemoveAt(index);
            }
            return offers;
        }

        private static readonly Dictionary<string, DeckPlan> DeckPlans = BuildDeckPlans();

        public static DeckPlan GetDeckPlan(string shipId)
        {
            return shipId != null && DeckPlans.TryGetValue(shipId, out var plan) ? plan : null;
        }

        private static Dictionary<string, DeckPlan> BuildDeckPlans()
        {
            var plans = new Dictionary<string, DeckPlan>();
            // Bow faces right (higher column). Row 0 is the top row.
            plans["ship_vanguard"] = Plan("ship_vanguard", 6, 3,
                Tile(ShipSystemType.Engines, 0, 0, 1, 2),
                Tile(ShipSystemType.LiftArray, 1, 0),
                Tile(ShipSystemType.AetherCore, 1, 1),
                Tile(ShipSystemType.FlightDeck, 2, 0, 2, 2),
                Tile(ShipSystemType.Sensors, 4, 0),
                Tile(ShipSystemType.Weapons, 4, 1, 1, 2),
                Tile(ShipSystemType.Bridge, 5, 0, 1, 2),
                Tile(ShipSystemType.LifeSupport, 0, 2),
                Tile(ShipSystemType.Infirmary, 1, 2),
                Tile(ShipSystemType.Ward, 2, 2, 2, 1));

            plans["enemy_cutter"] = Plan("enemy_cutter", 5, 2,
                Tile(ShipSystemType.Engines, 0, 0),
                Tile(ShipSystemType.AetherCore, 1, 0),
                Tile(ShipSystemType.FlightDeck, 2, 0),
                Tile(ShipSystemType.Sensors, 3, 0),
                Tile(ShipSystemType.Bridge, 4, 0),
                Tile(ShipSystemType.LifeSupport, 0, 1),
                Tile(ShipSystemType.LiftArray, 1, 1),
                Tile(ShipSystemType.Ward, 2, 1),
                Tile(ShipSystemType.Weapons, 3, 1),
                Tile(ShipSystemType.Infirmary, 4, 1));

            plans["enemy_cruiser"] = Plan("enemy_cruiser", 6, 3,
                Tile(ShipSystemType.Engines, 0, 0, 1, 2),
                Tile(ShipSystemType.LiftArray, 1, 0),
                Tile(ShipSystemType.AetherCore, 1, 1),
                Tile(ShipSystemType.Ward, 2, 0, 2, 1),
                Tile(ShipSystemType.FlightDeck, 2, 1, 1, 2),
                Tile(ShipSystemType.Weapons, 3, 1, 2, 2),
                Tile(ShipSystemType.Sensors, 4, 0),
                Tile(ShipSystemType.Bridge, 5, 0, 1, 2),
                Tile(ShipSystemType.LifeSupport, 0, 2),
                Tile(ShipSystemType.Infirmary, 1, 2));

            plans["enemy_carrier"] = Plan("enemy_carrier", 7, 3,
                Tile(ShipSystemType.Engines, 0, 0, 1, 2),
                Tile(ShipSystemType.LiftArray, 1, 0),
                Tile(ShipSystemType.AetherCore, 1, 1),
                Tile(ShipSystemType.FlightDeck, 2, 0, 3, 2),
                Tile(ShipSystemType.Sensors, 5, 0),
                Tile(ShipSystemType.Weapons, 5, 1),
                Tile(ShipSystemType.Bridge, 6, 0, 1, 2),
                Tile(ShipSystemType.LifeSupport, 0, 2),
                Tile(ShipSystemType.Infirmary, 1, 2),
                Tile(ShipSystemType.Ward, 2, 2, 2, 1));
            plans["enemy_scout"] = Plan("enemy_scout", 6, 2,
                Tile(ShipSystemType.Engines, 0, 0, 1, 2),
                Tile(ShipSystemType.AetherCore, 1, 0),
                Tile(ShipSystemType.LiftArray, 1, 1),
                Tile(ShipSystemType.Sensors, 2, 0, 2, 1),
                Tile(ShipSystemType.Ward, 2, 1),
                Tile(ShipSystemType.Weapons, 3, 1),
                Tile(ShipSystemType.Bridge, 4, 0),
                Tile(ShipSystemType.FlightDeck, 4, 1),
                Tile(ShipSystemType.LifeSupport, 5, 0),
                Tile(ShipSystemType.Infirmary, 5, 1));

            plans["enemy_boarder"] = Plan("enemy_boarder", 7, 2,
                Tile(ShipSystemType.Engines, 0, 0),
                Tile(ShipSystemType.LifeSupport, 0, 1),
                Tile(ShipSystemType.AetherCore, 1, 0),
                Tile(ShipSystemType.LiftArray, 1, 1),
                Tile(ShipSystemType.FlightDeck, 2, 0, 2, 2),
                Tile(ShipSystemType.Infirmary, 4, 0),
                Tile(ShipSystemType.Weapons, 4, 1),
                Tile(ShipSystemType.Bridge, 5, 0),
                Tile(ShipSystemType.Sensors, 5, 1),
                Tile(ShipSystemType.Ward, 6, 0, 1, 2));

            plans["enemy_monitor"] = Plan("enemy_monitor", 6, 3,
                Tile(ShipSystemType.Engines, 0, 0, 1, 2),
                Tile(ShipSystemType.LifeSupport, 0, 2),
                Tile(ShipSystemType.LiftArray, 1, 0),
                Tile(ShipSystemType.AetherCore, 1, 1),
                Tile(ShipSystemType.Infirmary, 1, 2),
                Tile(ShipSystemType.Ward, 2, 0, 2, 2),
                Tile(ShipSystemType.FlightDeck, 2, 2),
                Tile(ShipSystemType.Sensors, 3, 2),
                Tile(ShipSystemType.Weapons, 4, 0, 1, 3),
                Tile(ShipSystemType.Bridge, 5, 0, 1, 2));
            return plans;
        }

        private static DeckPlan Plan(string shipId, int columns, int rows, params DeckTile[] tiles)
        {
            var plan = new DeckPlan { shipId = shipId, columns = columns, rows = rows };
            plan.tiles.AddRange(tiles);
            return plan;
        }

        private static DeckTile Tile(ShipSystemType system, int column, int row, int width = 1, int height = 1)
        {
            return new DeckTile { system = system, column = column, row = row, width = width, height = height };
        }

        private static void AddSystem(ShipState ship, ShipSystemType type, string key, int power, int maxPower)
        {
            ship.systems.Add(new ShipSystemState
            {
                type = type,
                displayKey = key,
                power = power,
                maxPower = maxPower,
                maxDamage = 100f
            });
            ship.rooms.Add(new RoomState { id = "room_" + type.ToString().ToLowerInvariant(), system = type });
        }

        public static List<CrewState> CreateCrew(ProfileState profile)
        {
            var crew = new List<CrewState>
            {
                MakeCrew("crew_captain", profile.captainName, profile.captainLineage, CrewRole.Captain, ShipSystemType.Bridge, true, "background.exile", "trait.steadfast"),
                MakeCrew("crew_resonator", "Liora", CrewLineage.Elf, CrewRole.Resonator, ShipSystemType.AetherCore, false, "background.weather_scholar", "trait.attuned"),
                MakeCrew("crew_engineer", "Brom", CrewLineage.Dwarf, CrewRole.Engineer, ShipSystemType.Engines, false, "background.dockwright", "trait.fireproof"),
                MakeCrew("crew_pilot", "Pip", CrewLineage.Goblin, CrewRole.Pilot, ShipSystemType.FlightDeck, false, "background.deck_runner", "trait.quick_hands"),
                MakeCrew("crew_medic", "Sera", CrewLineage.Avian, CrewRole.Medic, ShipSystemType.Infirmary, false, "background.cloud_medic", "trait.light_step"),
                MakeCrew("crew_marine", "Rokan", CrewLineage.Orc, CrewRole.Marine, ShipSystemType.Weapons, false, "background.border_guard", "trait.rescuer")
            };
            return crew;
        }

        private static CrewState MakeCrew(string id, string name, CrewLineage lineage, CrewRole role,
            ShipSystemType room, bool captain, string background, string trait)
        {
            var health = MaxHealth(lineage);
            return new CrewState
            {
                id = id,
                displayName = name,
                lineage = lineage,
                role = role,
                currentRoom = room,
                isCaptain = captain,
                health = health,
                maxHealth = health,
                backgroundKey = background,
                traitKey = trait
            };
        }

        public static float MaxHealth(CrewLineage lineage)
        {
            switch (lineage)
            {
                case CrewLineage.Elf: return 80f;
                case CrewLineage.Orc: return 125f;
                case CrewLineage.Dwarf: return 110f;
                case CrewLineage.Goblin: return 90f;
                case CrewLineage.Avian: return 88f;
                default: return 100f;
            }
        }

        public static List<SquadronState> CreateSquadrons()
        {
            return new List<SquadronState>
            {
                new SquadronState
                {
                    id = "squad_kestrel", displayKey = "squadron.kestrel", type = SquadronType.Interceptor,
                    strength = 4, maxStrength = 4, ordnanceCost = 1, pilotCrewId = "crew_pilot"
                },
                new SquadronState
                {
                    id = "squad_ember", displayKey = "squadron.ember", type = SquadronType.Bomber,
                    strength = 3, maxStrength = 3, ordnanceCost = 2, pilotCrewId = "crew_marine"
                }
            };
        }

        private static readonly RegionDefinition[] Regions =
        {
            // Region 1 reproduces the legacy rolls exactly: weather total 6 (uniform), encounters 38/15/15/11/11/10.
            new RegionDefinition { id = "dawn_archipelago", nameKey = "region.dawn_archipelago", index = 1,
                weatherWeights = new[] { 1, 1, 1, 1, 1, 1 }, encounterWeights = new[] { 38, 15, 15, 11, 11, 10 }, enemyStatMultiplier = 1f, extraAetherCostChance = 0.2f },
            new RegionDefinition { id = "storm_corridor", nameKey = "region.storm_corridor", index = 2,
                weatherWeights = new[] { 1, 4, 4, 1, 1, 1 }, encounterWeights = new[] { 36, 12, 13, 6, 9, 24 }, enemyStatMultiplier = 1.12f, extraAetherCostChance = 0.25f },
            new RegionDefinition { id = "icefield_heights", nameKey = "region.icefield_heights", index = 3,
                weatherWeights = new[] { 1, 1, 1, 1, 4, 4 }, encounterWeights = new[] { 34, 22, 22, 8, 6, 8 }, enemyStatMultiplier = 1.38f, extraAetherCostChance = 0.3f },
            new RegionDefinition { id = "imperial_cordon", nameKey = "region.imperial_cordon", index = 4,
                weatherWeights = new[] { 4, 1, 1, 4, 1, 1 }, encounterWeights = new[] { 42, 8, 10, 8, 26, 6 }, enemyStatMultiplier = 1.6f, extraAetherCostChance = 0.35f }
        };

        public static int RegionCount => Regions.Length;

        public static RegionDefinition GetRegion(int index)
        {
            if (index < 1) index = 1;
            if (index > Regions.Length) index = Regions.Length;
            return Regions[index - 1];
        }

        private static int WeightedIndex(ref uint random, int[] weights)
        {
            var total = 0;
            for (var i = 0; i < weights.Length; i++) total += weights[i];
            var roll = SeededRandom.Range(ref random, 0, total);
            for (var i = 0; i < weights.Length; i++)
            {
                if (roll < weights[i]) return i;
                roll -= weights[i];
            }
            return weights.Length - 1;
        }

        public static List<RouteNodeState> CreateRoute(int seed)
        {
            return CreateRoute(seed, 1);
        }

        public static List<RouteNodeState> CreateRoute(int seed, int regionIndex)
        {
            var region = GetRegion(regionIndex);
            // Region 1 keeps the legacy stream so the locked first expedition is unchanged.
            var random = regionIndex <= 1
                ? SeededRandom.Seed(seed, 0xA11CEu)
                : SeededRandom.Seed(unchecked(seed + regionIndex * 7919), 0xA11CEu ^ (uint)(regionIndex * 0x9E37u));
            var nodes = new List<RouteNodeState>();
            nodes.Add(new RouteNodeState
            {
                id = "n0_1", nameKey = "node.departure", column = 0, lane = 1,
                encounterType = EncounterType.Start, encounterId = "departure", visited = true
            });

            for (var column = 1; column <= 6; column++)
            {
                for (var lane = 0; lane < 3; lane++)
                {
                    var encounter = EncounterFor(column, lane, region, ref random);
                    nodes.Add(new RouteNodeState
                    {
                        id = $"n{column}_{lane}",
                        nameKey = "node." + encounter.ToString().ToLowerInvariant(),
                        column = column,
                        lane = lane,
                        aetherCost = 1 + (SeededRandom.Chance(ref random, region.extraAetherCostChance) ? 1 : 0),
                        recommendedAltitude = (AltitudeBand)SeededRandom.Range(ref random, 0, 3),
                        weather = (WeatherType)WeightedIndex(ref random, region.weatherWeights),
                        encounterType = encounter,
                        encounterId = EncounterIdFor(encounter)
                    });
                }
            }

            nodes.Add(new RouteNodeState
            {
                id = "n7_1", nameKey = "node.gate", column = 7, lane = 1, aetherCost = 2,
                recommendedAltitude = AltitudeBand.High, weather = WeatherType.AetherCurrent,
                encounterType = EncounterType.Gate, encounterId = "gate_finale"
            });

            for (var i = 0; i < nodes.Count; i++)
            {
                var source = nodes[i];
                if (source.column >= 7) continue;
                for (var j = 0; j < nodes.Count; j++)
                {
                    var target = nodes[j];
                    if (target.column != source.column + 1) continue;
                    if (Math.Abs(target.lane - source.lane) <= 1) source.connectedIds.Add(target.id);
                }
            }
            return nodes;
        }

        private static readonly EncounterType[] RollableEncounters =
            { EncounterType.Battle, EncounterType.Rescue, EncounterType.Salvage, EncounterType.Trade, EncounterType.Checkpoint, EncounterType.Storm };

        private static EncounterType EncounterFor(int column, int lane, RegionDefinition region, ref uint random)
        {
            if (column == 2) return EncounterType.Battle;
            if (column == 6) return EncounterType.EliteBattle;
            return RollableEncounters[WeightedIndex(ref random, region.encounterWeights)];
        }

        private static string EncounterIdFor(EncounterType type)
        {
            switch (type)
            {
                case EncounterType.Rescue: return "drifting_refugees";
                case EncounterType.Salvage: return "ruined_dock";
                case EncounterType.Trade: return "free_port";
                case EncounterType.Checkpoint: return "imperial_checkpoint";
                case EncounterType.Storm: return "storm_eye";
                default: return "";
            }
        }

        public static EncounterDefinition GetEncounter(string id)
        {
            return !string.IsNullOrEmpty(id) && Encounters.TryGetValue(id, out var value) ? value : null;
        }

        /// <summary>Authored event ids for a type, in a stable order (baseline event first).</summary>
        public static List<string> EncounterIds(EncounterType type)
        {
            var ids = new List<string>();
            var baseline = EncounterIdFor(type);
            if (!string.IsNullOrEmpty(baseline) && Encounters.ContainsKey(baseline)) ids.Add(baseline);
            foreach (var pair in Encounters)
                if (pair.Value.type == type && pair.Key != baseline) ids.Add(pair.Key);
            return ids;
        }

        /// <summary>
        /// Replaces the baseline event on every non-combat node with a draw from that type's pool,
        /// without replacement until the pool is exhausted. Uses its own RNG stream so route rolls stay intact.
        /// </summary>
        public static void AssignEncounterVariants(List<RouteNodeState> nodes, int seed)
        {
            var random = SeededRandom.Seed(seed, 0x5E1EC7u);
            var pools = new Dictionary<EncounterType, List<string>>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var all = EncounterIds(node.encounterType);
                if (all.Count == 0) continue;
                if (!pools.TryGetValue(node.encounterType, out var pool) || pool.Count == 0)
                {
                    pool = new List<string>(all);
                    pools[node.encounterType] = pool;
                }
                var index = SeededRandom.Range(ref random, 0, pool.Count);
                node.encounterId = pool[index];
                pool.RemoveAt(index);
            }
        }

        public static WeatherProfile GetWeather(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Thunderhead:
                    return new WeatherProfile { type = type, nameKey = "weather.thunder", accuracyModifier = -0.08f, wardRegenModifier = 0.7f, hazardInterval = 6f };
                case WeatherType.Turbulence:
                    return new WeatherProfile { type = type, nameKey = "weather.turbulence", accuracyModifier = -0.12f, squadronTimeModifier = 1.3f, hazardInterval = 8f };
                case WeatherType.AetherCurrent:
                    return new WeatherProfile { type = type, nameKey = "weather.aether", accuracyModifier = 0.04f, wardRegenModifier = 1.35f, hazardInterval = 7f };
                case WeatherType.Icing:
                    return new WeatherProfile { type = type, nameKey = "weather.icing", accuracyModifier = -0.04f, squadronTimeModifier = 1.15f, hazardInterval = 7f };
                case WeatherType.CloudCover:
                    return new WeatherProfile { type = type, nameKey = "weather.cloud", accuracyModifier = -0.15f, squadronTimeModifier = 0.9f, hazardInterval = 10f };
                default:
                    return new WeatherProfile { type = WeatherType.Clear, nameKey = "weather.clear", accuracyModifier = 0f, hazardInterval = 12f };
            }
        }

        private static Dictionary<string, EncounterDefinition> BuildEncounters()
        {
            var result = new Dictionary<string, EncounterDefinition>();
            EncounterLibrary.AddAll(result);

            result["drifting_refugees"] = new EncounterDefinition
            {
                id = "drifting_refugees", type = EncounterType.Rescue,
                titleKey = "encounter.refugees.title", bodyKey = "encounter.refugees.body",
                choices = new List<EncounterChoiceDefinition>
                {
                    new EncounterChoiceDefinition { id = "rescue", textKey = "choice.rescue", resultKey = "result.rescue", suppliesCost = 2, survivorDelta = 84, moraleDelta = 8 },
                    new EncounterChoiceDefinition { id = "tow", textKey = "choice.tow", resultKey = "result.tow", aetherCost = 1, survivorDelta = 45, salvageDelta = 6, moraleDelta = 3 },
                    new EncounterChoiceDefinition { id = "leave", textKey = "choice.leave", resultKey = "result.leave", moraleDelta = -7 }
                }
            };

            result["ruined_dock"] = new EncounterDefinition
            {
                id = "ruined_dock", type = EncounterType.Salvage,
                titleKey = "encounter.dock.title", bodyKey = "encounter.dock.body",
                choices = new List<EncounterChoiceDefinition>
                {
                    new EncounterChoiceDefinition { id = "salvage", textKey = "choice.salvage", resultKey = "result.salvage", salvageDelta = 12, ordnanceDelta = 2 },
                    new EncounterChoiceDefinition { id = "stabilize", textKey = "choice.stabilize", resultKey = "result.stabilize", salvageCost = 4, moraleDelta = 9, survivorDelta = 18 },
                    new EncounterChoiceDefinition { id = "scout", textKey = "choice.scout", resultKey = "result.scout", requiredTag = "support.pathfinder", aetherDelta = 2, salvageDelta = 5 }
                }
            };

            result["free_port"] = new EncounterDefinition
            {
                id = "free_port", type = EncounterType.Trade,
                titleKey = "encounter.port.title", bodyKey = "encounter.port.body",
                choices = new List<EncounterChoiceDefinition>
                {
                    new EncounterChoiceDefinition { id = "fuel", textKey = "choice.buy_aether", resultKey = "result.trade", salvageCost = 6, aetherDelta = 4 },
                    new EncounterChoiceDefinition { id = "supply", textKey = "choice.buy_supplies", resultKey = "result.trade", salvageCost = 6, suppliesDelta = 5 },
                    new EncounterChoiceDefinition { id = "repair", textKey = "choice.repair", resultKey = "result.repair", salvageCost = 8 },
                    new EncounterChoiceDefinition { id = "depart", textKey = "choice.depart", resultKey = "result.depart" }
                }
            };

            result["imperial_checkpoint"] = new EncounterDefinition
            {
                id = "imperial_checkpoint", type = EncounterType.Checkpoint,
                titleKey = "encounter.checkpoint.title", bodyKey = "encounter.checkpoint.body",
                choices = new List<EncounterChoiceDefinition>
                {
                    new EncounterChoiceDefinition { id = "bribe", textKey = "choice.bribe", resultKey = "result.bribe", salvageCost = 9, moraleDelta = -2 },
                    new EncounterChoiceDefinition { id = "reformist", textKey = "choice.reformist", resultKey = "result.reformist", requiredTag = "lineage.human", suppliesCost = 1, moraleDelta = 4 },
                    new EncounterChoiceDefinition { id = "fight", textKey = "choice.fight", resultKey = "result.fight", startsBattle = true }
                }
            };

            result["storm_eye"] = new EncounterDefinition
            {
                id = "storm_eye", type = EncounterType.Storm,
                titleKey = "encounter.storm.title", bodyKey = "encounter.storm.body",
                choices = new List<EncounterChoiceDefinition>
                {
                    new EncounterChoiceDefinition { id = "high", textKey = "choice.climb", resultKey = "result.climb", aetherCost = 2, moraleDelta = 2 },
                    new EncounterChoiceDefinition { id = "ride", textKey = "choice.ride", resultKey = "result.ride", requiredTag = "lineage.elf", aetherDelta = 2, moraleDelta = 5 },
                    new EncounterChoiceDefinition { id = "push", textKey = "choice.push", resultKey = "result.push", survivorDelta = -24, moraleDelta = -6 }
                }
            };

            return result;
        }
    }
}
