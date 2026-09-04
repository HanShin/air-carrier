using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using AetherArk.Content;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;

namespace AetherArk.Tests
{
    public sealed class GameSimulationTests
    {
        private static ProfileState Profile(Difficulty difficulty = Difficulty.Standard)
        {
            return new ProfileState
            {
                captainName = "Test Captain",
                captainLineage = CrewLineage.Human,
                difficulty = difficulty,
                supportShip = SupportShipType.Workshop
            };
        }

        [Test]
        public void NewRun_IsDeterministicForSameSeed()
        {
            var first = GameSimulation.NewRun(Profile(), 41234).State;
            var second = GameSimulation.NewRun(Profile(), 41234).State;

            Assert.That(first.routeNodes.Count, Is.EqualTo(second.routeNodes.Count));
            for (var i = 0; i < first.routeNodes.Count; i++)
            {
                Assert.That(first.routeNodes[i].encounterType, Is.EqualTo(second.routeNodes[i].encounterType));
                Assert.That(first.routeNodes[i].weather, Is.EqualTo(second.routeNodes[i].weather));
                Assert.That(first.routeNodes[i].aetherCost, Is.EqualTo(second.routeNodes[i].aetherCost));
            }
        }

        [Test]
        public void Route_ContainsMandatoryPacingBattlesAndSevenJumps()
        {
            var state = GameSimulation.NewRun(Profile(), 41234).State;

            Assert.That(state.routeNodes.Exists(node => node.column == 2 && node.encounterType == EncounterType.Battle), Is.True);
            Assert.That(state.routeNodes.FindAll(node => node.column == 6 && node.encounterType == EncounterType.EliteBattle).Count, Is.EqualTo(3));
            Assert.That(state.routeNodes.Exists(node => node.column == 7 && node.encounterType == EncounterType.Gate), Is.True);
        }

        [Test]
        public void FullRun_CanReachAndCompleteSkyGateFinale()
        {
            var simulation = GameSimulation.NewRun(Profile(), 91573);
            var safety = 0;

            while (simulation.State.phase != GamePhase.Victory && simulation.State.phase != GamePhase.Defeat && safety++ < 80)
            {
                switch (simulation.State.phase)
                {
                    case GamePhase.RouteMap:
                        var destination = simulation.State.routeNodes.Find(simulation.CanTravelTo);
                        if (destination == null)
                        {
                            Assert.That(simulation.EmergencyAetherBurn().success, Is.True);
                        }
                        else
                        {
                            Assert.That(simulation.TravelTo(destination.id).success, Is.True);
                        }
                        break;
                    case GamePhase.Encounter:
                        var encounter = simulation.ActiveEncounter;
                        var choice = encounter.choices.Find(simulation.CanChoose);
                        Assert.That(choice, Is.Not.Null, $"Encounter {encounter.id} had no available choice");
                        Assert.That(simulation.ChooseEncounter(choice.id).success, Is.True);
                        break;
                    case GamePhase.Combat:
                        simulation.ApplyDamage(simulation.State.enemyShip, ShipSystemType.AetherCore, 999f, true);
                        simulation.SetPaused(false);
                        simulation.Tick(0.1f);
                        break;
                }
            }

            Assert.That(safety, Is.LessThan(80), "The run did not terminate");
            Assert.That(simulation.State.travelCount, Is.EqualTo(7));
            Assert.That(simulation.State.phase, Is.EqualTo(GamePhase.Victory));
            Assert.That(simulation.State.defeatReason, Is.EqualTo(DefeatReason.None));
        }

        [Test]
        public void FirstExpedition_UsesLockedVerifiedSeedUntilTutorialVictory()
        {
            var profile = Profile(Difficulty.Story);
            profile.tutorialSeen = false;
            var simulation = GameSimulation.NewRun(profile, GameSimulation.FirstExpeditionSeed);

            Assert.That(simulation.State.isFirstExpedition, Is.True);
            Assert.That(simulation.State.seed, Is.EqualTo(32838));
            Assert.That(simulation.State.routeNodes.FindAll(node =>
                    node.encounterType == EncounterType.Battle || node.encounterType == EncounterType.EliteBattle).Count,
                Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void PowerAllocation_CannotExceedCoreOutput()
        {
            var simulation = GameSimulation.NewRun(Profile(), 1);
            simulation.BeginCombat(1, false);

            var first = simulation.ChangePower(ShipSystemType.Sensors, 1);
            var second = simulation.ChangePower(ShipSystemType.Ward, 1);

            Assert.That(first.success, Is.True);
            Assert.That(simulation.State.playerShip.AllocatedPower(), Is.EqualTo(simulation.State.playerShip.coreOutput));
            Assert.That(second.success, Is.False);
            Assert.That(simulation.State.playerShip.AllocatedPower(), Is.LessThanOrEqualTo(simulation.State.playerShip.coreOutput));
        }

        [Test]
        public void Damage_ConsumesWardThenArmorThenHull()
        {
            var simulation = GameSimulation.NewRun(Profile(), 2);
            var ship = simulation.State.playerShip;
            var startingHull = ship.hull;

            simulation.ApplyDamage(ship, ShipSystemType.Weapons, 15f, false);
            Assert.That(ship.ward, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ship.armor, Is.EqualTo(15f).Within(0.001f));
            Assert.That(ship.hull, Is.EqualTo(startingHull).Within(0.001f));

            simulation.ApplyDamage(ship, ShipSystemType.Weapons, 20f, false);
            Assert.That(ship.armor, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ship.hull, Is.EqualTo(startingHull - 5f).Within(0.001f));
            Assert.That(ship.GetSystem(ShipSystemType.Weapons).damage, Is.GreaterThan(0f));
        }

        [Test]
        public void EncounterChoice_ConsumesCostsAndAppliesConvoyOutcome()
        {
            var simulation = GameSimulation.NewRun(Profile(), 3);
            var beforeSupplies = simulation.State.resources.supplies;
            var beforeSurvivors = simulation.State.convoy.survivors;
            simulation.State.phase = GamePhase.Encounter;
            simulation.State.activeEncounterId = "drifting_refugees";

            var result = simulation.ChooseEncounter("rescue");

            Assert.That(result.success, Is.True);
            Assert.That(simulation.State.resources.supplies, Is.EqualTo(beforeSupplies - 2));
            Assert.That(simulation.State.convoy.survivors, Is.EqualTo(beforeSurvivors + 84));
            Assert.That(simulation.State.phase, Is.EqualTo(GamePhase.RouteMap));
        }

        [Test]
        public void CaptainDeath_EndsRunImmediatelyAfterResolutionTick()
        {
            var simulation = GameSimulation.NewRun(Profile(), 4);
            simulation.BeginCombat(1, false);
            simulation.SetPaused(false);
            simulation.State.crew.Find(crew => crew.isCaptain).isDead = true;

            simulation.Tick(0.1f);

            Assert.That(simulation.State.phase, Is.EqualTo(GamePhase.Defeat));
            Assert.That(simulation.State.defeatReason, Is.EqualTo(DefeatReason.CaptainLost));
        }

        [Test]
        public void SquadronLaunch_ConsumesOrdnanceAndUsesPilot()
        {
            var simulation = GameSimulation.NewRun(Profile(), 5);
            simulation.BeginCombat(1, false);
            var squadron = simulation.State.squadrons[0];
            var pilot = simulation.State.crew.Find(crew => crew.id == squadron.pilotCrewId);
            var before = simulation.State.resources.ordnance;

            var result = simulation.LaunchSquadron(squadron.id, SquadronMission.Intercept, ShipSystemType.FlightDeck);

            Assert.That(result.success, Is.True);
            Assert.That(simulation.State.resources.ordnance, Is.EqualTo(before - squadron.ordnanceCost));
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Launching));
            Assert.That(pilot.onSortie, Is.True);
        }

        [Test]
        public void SquadronLifecycle_ExposesLaunchMissionReturnAndRecoveryFeedback()
        {
            var simulation = GameSimulation.NewRun(Profile(), 501);
            simulation.BeginCombat(1, false);
            var squadron = simulation.State.squadrons[0];
            squadron.strength = 99;

            Assert.That(simulation.LaunchSquadron(squadron.id, SquadronMission.Intercept, ShipSystemType.FlightDeck).success, Is.True);
            Assert.That(squadron.phaseDuration, Is.GreaterThan(0f));
            simulation.SetPaused(false);
            squadron.missionTimer = 0f;
            simulation.Tick(0.1f);
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.OnMission));
            Assert.That(simulation.State.combatAlertKey, Is.EqualTo("alert.squadron_on_mission"));

            squadron.missionTimer = 0f;
            simulation.Tick(0.1f);
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Recovering));
            Assert.That(simulation.State.combatAlertKey, Is.EqualTo("alert.squadron_returning"));

            squadron.missionTimer = 0f;
            simulation.Tick(0.1f);
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Ready));
            Assert.That(simulation.State.combatAlertKey, Is.EqualTo("alert.squadron_recovered"));
        }

        [Test]
        public void HullPenetration_AutoPausesWithSpecificDangerReason()
        {
            var simulation = GameSimulation.NewRun(Profile(), 502);
            simulation.BeginCombat(1, false);
            simulation.SetPaused(false);

            simulation.ApplyDamage(simulation.State.playerShip, ShipSystemType.Weapons, 40f, false);

            Assert.That(simulation.State.isPaused, Is.True);
            Assert.That(simulation.State.combatAlertPausedBattle, Is.True);
            Assert.That(simulation.State.combatAlertKey, Is.EqualTo("alert.hull_breached"));
            Assert.That(simulation.State.combatAlertArgument, Is.EqualTo(ShipSystemType.Weapons.ToString()));
        }

        [Test]
        public void DisabledAutoPause_StillPublishesDangerWithoutStoppingCombat()
        {
            var profile = Profile();
            profile.accessibility.autoPauseOnWarning = false;
            var simulation = GameSimulation.NewRun(profile, 503);
            simulation.BeginCombat(1, false);
            simulation.SetPaused(false);

            simulation.ApplyDamage(simulation.State.playerShip, ShipSystemType.Weapons, 40f, false);

            Assert.That(simulation.State.isPaused, Is.False);
            Assert.That(simulation.State.combatAlertPausedBattle, Is.False);
            Assert.That(simulation.State.combatAlertKey, Is.EqualTo("alert.hull_breached"));
            Assert.That(simulation.State.combatAlertSeverity, Is.EqualTo(AlertSeverity.Warning));
        }

        [Test]
        public void StoryDifficulty_ProvidesAdditionalResources()
        {
            var story = GameSimulation.NewRun(Profile(Difficulty.Story), 7).State;
            var standard = GameSimulation.NewRun(Profile(Difficulty.Standard), 7).State;

            Assert.That(story.resources.aether, Is.GreaterThan(standard.resources.aether));
            Assert.That(story.resources.supplies, Is.GreaterThan(standard.resources.supplies));
            Assert.That(story.playerShip.maxHull, Is.GreaterThan(standard.playerShip.maxHull));
        }

        [Test]
        public void StrandedConvoy_CanTradeLivesAndMoraleForEmergencyFuel()
        {
            var simulation = GameSimulation.NewRun(Profile(), 71);
            simulation.State.resources.aether = 0;
            var survivors = simulation.State.convoy.survivors;
            var morale = simulation.State.convoy.morale;

            var result = simulation.EmergencyAetherBurn();

            Assert.That(result.success, Is.True);
            Assert.That(simulation.State.resources.aether, Is.EqualTo(2));
            Assert.That(simulation.State.convoy.survivors, Is.EqualTo(survivors - 12));
            Assert.That(simulation.State.convoy.morale, Is.EqualTo(morale - 6));
            Assert.That(simulation.HasAffordableRoute(), Is.True);
        }

        [Test]
        public void EmptyMagazine_CanConvertSalvageIntoEmergencyOrdnance()
        {
            var simulation = GameSimulation.NewRun(Profile(), 72);
            simulation.BeginCombat(1, false);
            simulation.State.resources.ordnance = 0;
            simulation.State.resources.salvage = 8;
            var morale = simulation.State.convoy.morale;

            var result = simulation.EmergencyOrdnanceAssembly();

            Assert.That(result.success, Is.True);
            Assert.That(simulation.State.resources.ordnance, Is.EqualTo(3));
            Assert.That(simulation.State.resources.salvage, Is.EqualTo(5));
            Assert.That(simulation.State.convoy.morale, Is.EqualTo(morale - 1));
            Assert.That(simulation.State.playerShip.instability, Is.EqualTo(8f));
        }

        [Test]
        public void EmergencyOrdnance_FallsBackToSuppliesWhenSalvageIsShort()
        {
            var simulation = GameSimulation.NewRun(Profile(), 73);
            simulation.BeginCombat(1, false);
            simulation.State.resources.ordnance = 0;
            simulation.State.resources.salvage = 1;
            simulation.State.resources.supplies = 5;

            var result = simulation.EmergencyOrdnanceAssembly();

            Assert.That(result.success, Is.True);
            Assert.That(simulation.State.resources.ordnance, Is.EqualTo(3));
            Assert.That(simulation.State.resources.salvage, Is.EqualTo(0));
            Assert.That(simulation.State.resources.supplies, Is.EqualTo(3));
        }

        [Test]
        public void SaveService_RoundTripsProfileAndRun()
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-ark-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var service = new SaveService(root);
                var profile = Profile();
                profile.language = Language.English;
                var run = GameSimulation.NewRun(profile, 9981).State;
                run.resources.salvage = 77;

                service.SaveProfile(profile);
                service.SaveRun(run);
                var loadedProfile = service.LoadProfile();
                var loadedRun = service.LoadRun();

                Assert.That(loadedProfile.language, Is.EqualTo(Language.English));
                Assert.That(loadedRun.seed, Is.EqualTo(9981));
                Assert.That(loadedRun.resources.salvage, Is.EqualTo(77));
                Assert.That(loadedRun.routeNodes.Count, Is.EqualTo(run.routeNodes.Count));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static GameSimulation RunWithStrikeCarrier()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            for (var seed = 1; seed < 400; seed++)
            {
                var simulation = GameSimulation.NewRun(profile, seed);
                simulation.BeginCombat(1, false);
                if (simulation.State.enemyShip.id == "enemy_carrier") return simulation;
            }
            Assert.Fail("No tier-1 battle produced the Imperial Strike Carrier.");
            return null;
        }

        [Test]
        public void FirstExpedition_NeverSpawnsStrikeCarrier()
        {
            var profile = Profile(Difficulty.Story);
            profile.tutorialSeen = false;
            var simulation = GameSimulation.NewRun(profile, GameSimulation.FirstExpeditionSeed);

            for (var battle = 0; battle < 60; battle++)
            {
                simulation.BeginCombat(1, false);
                Assert.That(simulation.State.enemyShip.id, Is.EqualTo("enemy_cutter"));
            }
        }

        [Test]
        public void StrikeCarrier_SpawnsWithDeckHeavyPowerBudget()
        {
            var enemy = RunWithStrikeCarrier().State.enemyShip;

            Assert.That(enemy.nameKey, Is.EqualTo("ship.enemy_carrier"));
            Assert.That(enemy.GetSystem(ShipSystemType.FlightDeck).power, Is.EqualTo(3));
            Assert.That(enemy.GetSystem(ShipSystemType.Weapons).power, Is.EqualTo(1));
            Assert.That(enemy.AllocatedPower(), Is.LessThanOrEqualTo(enemy.coreOutput));
        }

        [Test]
        public void StrikeCarrier_AirStrikeHitsDeckUnlessInterceptorsAreReady()
        {
            var simulation = RunWithStrikeCarrier();
            var state = simulation.State;
            state.playerShip.ward = 0f;
            state.playerShip.armor = 0f;
            state.playerShip.GetSystem(ShipSystemType.Ward).power = 0;
            state.enemyShip.GetSystem(ShipSystemType.Weapons).power = 0;
            state.currentWeather = WeatherType.Clear;
            state.weatherHazardTimer = 999f;
            state.interceptCharges = 1;
            simulation.SetPaused(false);

            for (var i = 0; i < 106; i++) simulation.Tick(0.1f);
            Assert.That(state.interceptCharges, Is.EqualTo(0));
            Assert.That(state.playerShip.GetSystem(ShipSystemType.FlightDeck).damage, Is.EqualTo(0f).Within(0.001f));

            for (var i = 0; i < 160; i++) simulation.Tick(0.1f);
            Assert.That(state.playerShip.GetSystem(ShipSystemType.FlightDeck).damage, Is.GreaterThan(0f));
        }

        [TestCase("ship.enemy_cutter")]
        [TestCase("ship.enemy_cruiser")]
        [TestCase("ship.enemy_carrier")]
        [TestCase("ship.enemy_scout")]
        [TestCase("ship.enemy_boarder")]
        [TestCase("ship.enemy_monitor")]
        public void EnemyShipNames_AreLocalizedInBothLanguages(string key)
        {
            Assert.That(new LocalizationService(Language.Korean).T(key), Is.Not.EqualTo(key));
            Assert.That(new LocalizationService(Language.English).T(key), Is.Not.EqualTo(key));
            Assert.That(new LocalizationService(Language.Korean).T(key), Is.Not.EqualTo(new LocalizationService(Language.English).T(key)));
        }

        [TestCase("ship_vanguard")]
        [TestCase("ship_bastion")]
        [TestCase("ship_zephyr")]
        [TestCase("enemy_cutter")]
        [TestCase("enemy_cruiser")]
        [TestCase("enemy_carrier")]
        [TestCase("enemy_scout")]
        [TestCase("enemy_boarder")]
        [TestCase("enemy_monitor")]
        public void DeckPlan_CoversEverySystemWithoutOverlapInsideGrid(string shipId)
        {
            var random = 7u;
            var ship = shipId.StartsWith("ship_") ? ContentCatalog.CreateFlagship(shipId) : ContentCatalog.CreateEnemyById(shipId, ref random);
            Assert.That(ship.id, Is.EqualTo(shipId));
            var plan = ContentCatalog.GetDeckPlan(ship.id);
            Assert.That(plan, Is.Not.Null);

            var occupied = new bool[plan.columns, plan.rows];
            foreach (var system in ship.systems)
            {
                var tile = plan.GetTile(system.type);
                Assert.That(tile, Is.Not.Null, system.type + " has no deck tile");
                Assert.That(plan.tiles.FindAll(t => t.system == system.type).Count, Is.EqualTo(1));
                for (var x = tile.column; x < tile.column + tile.width; x++)
                for (var y = tile.row; y < tile.row + tile.height; y++)
                {
                    Assert.That(x, Is.InRange(0, plan.columns - 1), system.type + " leaves the grid");
                    Assert.That(y, Is.InRange(0, plan.rows - 1), system.type + " leaves the grid");
                    Assert.That(occupied[x, y], Is.False, system.type + " overlaps another room");
                    occupied[x, y] = true;
                }
            }
        }

        private static ShipState FindCarrier()
        {
            for (var seed = 1u; seed < 500u; seed++)
            {
                var random = seed;
                var ship = ContentCatalog.CreateEnemy(1, true, ref random);
                if (ship.id == "enemy_carrier") return ship;
            }
            Assert.Fail("No carrier found");
            return null;
        }

        [Test]
        public void BlueprintRules_ClassifyRoomByPowerDamageAndDisabledState()
        {
            var system = new ShipSystemState { type = ShipSystemType.Weapons, power = 2, maxPower = 4, maxDamage = 100f };
            Assert.That(BlueprintRules.Classify(system), Is.EqualTo(RoomCondition.Operational));

            system.power = 0;
            Assert.That(BlueprintRules.Classify(system), Is.EqualTo(RoomCondition.Unpowered));

            system.power = 2;
            system.damage = 40f;
            Assert.That(BlueprintRules.Classify(system), Is.EqualTo(RoomCondition.Damaged));

            system.damage = 100f;
            Assert.That(BlueprintRules.Classify(system), Is.EqualTo(RoomCondition.Disabled));

            var core = new ShipSystemState { type = ShipSystemType.AetherCore, power = 0, maxPower = 0, maxDamage = 100f };
            Assert.That(BlueprintRules.Classify(core), Is.EqualTo(RoomCondition.Operational), "systems without a power budget are never 'unpowered'");
        }

        [TestCase("Liora", "L")]
        [TestCase("아린", "아")]
        [TestCase("", "?")]
        [TestCase(null, "?")]
        public void BlueprintRules_CrewInitialUsesFirstCharacter(string name, string expected)
        {
            Assert.That(BlueprintRules.CrewInitial(name), Is.EqualTo(expected));
        }

        [Test]
        public void DebugScenarios_DamageShowcaseExercisesEveryHazardOverlay()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 5);
            simulation.BeginCombat(1, false);

            DebugScenarios.ApplyDamageShowcase(simulation.State);
            var ship = simulation.State.playerShip;

            Assert.That(BlueprintRules.Classify(ship.GetSystem(ShipSystemType.Weapons)), Is.EqualTo(RoomCondition.Damaged));
            Assert.That(ship.GetRoom(ShipSystemType.Weapons).fire, Is.GreaterThan(10f));
            Assert.That(ship.GetRoom(ShipSystemType.Engines).breach, Is.GreaterThan(10f));
            Assert.That(ship.GetRoom(ShipSystemType.LifeSupport).oxygen, Is.LessThan(30f));
            Assert.That(BlueprintRules.Classify(ship.GetSystem(ShipSystemType.Sensors)), Is.EqualTo(RoomCondition.Disabled));
            Assert.That(BlueprintRules.Classify(ship.GetSystem(ShipSystemType.Infirmary)), Is.EqualTo(RoomCondition.Unpowered));
            Assert.That(ship.ward, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ship.armor, Is.LessThan(ship.maxArmor));
            Assert.That(simulation.State.crew.Exists(crew => crew.IsDowned), Is.True, "one crew member should be downed so the token state is visible");
            Assert.That(BlueprintRules.Classify(simulation.State.enemyShip.GetSystem(ShipSystemType.Ward)), Is.EqualTo(RoomCondition.Damaged));
            Assert.That(simulation.State.enemyShip.GetRoom(ShipSystemType.FlightDeck).fire, Is.GreaterThan(10f));
        }

        [Test]
        public void RouteRules_NextStormColumnFollowsTheTravelCount()
        {
            var state = new RunState { travelCount = 0 };
            Assert.That(RouteRules.NextStormColumn(state), Is.EqualTo(-1));
            state.travelCount = 2;
            Assert.That(RouteRules.NextStormColumn(state), Is.EqualTo(0));
            state.travelCount = 5;
            Assert.That(RouteRules.NextStormColumn(state), Is.EqualTo(3));
        }

        [Test]
        public void RouteRules_StormColumnAfterTravelMatchesPrediction()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 41234);
            for (var jump = 0; jump < 3; jump++)
            {
                var predicted = RouteRules.NextStormColumn(simulation.State);
                var destination = simulation.State.routeNodes.Find(simulation.CanTravelTo);
                Assert.That(destination, Is.Not.Null);
                simulation.TravelTo(destination.id);
                Assert.That(simulation.State.stormColumn, Is.EqualTo(predicted));
                if (simulation.State.phase != GamePhase.RouteMap) break;
            }
        }

        [TestCase(EncounterType.Start, "node.departure")]
        [TestCase(EncounterType.EliteBattle, "node.elitebattle")]
        [TestCase(EncounterType.Gate, "node.gate")]
        public void RouteRules_NameKeysResolveInBothLanguages(EncounterType type, string expectedKey)
        {
            Assert.That(RouteRules.NameKey(type), Is.EqualTo(expectedKey));
            Assert.That(new LocalizationService(Language.Korean).T(expectedKey), Is.Not.EqualTo(expectedKey));
            Assert.That(new LocalizationService(Language.English).T(expectedKey), Is.Not.EqualTo(expectedKey));
        }

        [Test]
        public void RouteRules_EveryEncounterTypeHasADistinctGlyphExceptBattleTiers()
        {
            var glyphs = new System.Collections.Generic.HashSet<string>();
            foreach (EncounterType type in Enum.GetValues(typeof(EncounterType)))
            {
                var glyph = RouteRules.Glyph(type);
                Assert.That(glyph, Is.Not.Empty);
                if (type != EncounterType.EliteBattle) Assert.That(glyphs.Add(glyph), Is.True, type + " shares a glyph");
            }
            Assert.That(RouteRules.Glyph(EncounterType.EliteBattle), Is.EqualTo(RouteRules.Glyph(EncounterType.Battle)));
        }

        [TestCase("enemy_scout", 1)]
        [TestCase("enemy_boarder", 1)]
        [TestCase("enemy_monitor", 2)]
        public void EnemyRoster_NewSilhouettesSpawnOnTheirTierWhenVariantsAreAllowed(string shipId, int tier)
        {
            var found = false;
            for (var seed = 1u; seed < 600u && !found; seed++)
            {
                var random = seed;
                found = ContentCatalog.CreateEnemy(tier, true, ref random).id == shipId;
            }
            Assert.That(found, Is.True, shipId + " never spawned on tier " + tier);
        }

        [Test]
        public void EnemyRoster_EveryShipRespectsItsPowerBudget()
        {
            foreach (var id in new[] { "enemy_cutter", "enemy_carrier", "enemy_scout", "enemy_boarder", "enemy_cruiser", "enemy_monitor" })
            {
                var random = 3u;
                var ship = ContentCatalog.CreateEnemyById(id, ref random);
                Assert.That(ship, Is.Not.Null, id);
                Assert.That(ship.AllocatedPower(), Is.LessThanOrEqualTo(ship.coreOutput), id + " over-allocates power");
                Assert.That(ship.systems.Count, Is.EqualTo(10), id + " must carry all ten systems");
            }
            var boarder = ContentCatalog.CreateEnemyById("enemy_boarder", ref Unused);
            Assert.That(boarder.boardingCapable, Is.True);
            Assert.That(ContentCatalog.CreateEnemyById("enemy_carrier", ref Unused).boardingCapable, Is.False);
        }

        private static uint Unused = 1u;

        private static GameSimulation RunAgainst(string shipId)
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 11);
            simulation.BeginCombat(shipId == "enemy_monitor" || shipId == "enemy_cruiser" ? 2 : 1, false);
            var random = simulation.State.random.combat;
            simulation.State.enemyShip = ContentCatalog.CreateEnemyById(shipId, ref random);
            simulation.State.random.combat = random;
            return simulation;
        }

        private static void IsolateFromOtherThreats(RunState state)
        {
            state.enemyShip.GetSystem(ShipSystemType.Weapons).power = 0;
            state.currentWeather = WeatherType.Clear;
            state.weatherHazardTimer = 999f;
        }

        [Test]
        public void BoardingBarge_LandsBoardersUnlessInterceptorsAreReady()
        {
            var simulation = RunAgainst("enemy_boarder");
            var state = simulation.State;
            IsolateFromOtherThreats(state);
            state.interceptCharges = 1;
            simulation.SetPaused(false);

            for (var i = 0; i < 106; i++) simulation.Tick(0.1f);
            Assert.That(state.interceptCharges, Is.EqualTo(0));
            Assert.That(state.playerShip.rooms.Exists(room => room.intruders > 0), Is.False, "interceptors should repel the first boarding party");

            for (var i = 0; i < 160; i++) simulation.Tick(0.1f);
            Assert.That(state.playerShip.rooms.Exists(room => room.intruders > 0), Is.True, "the second boarding party should land");
            Assert.That(state.combatLog.Exists(entry => entry.key == "log.boarders"), Is.True);
        }

        [Test]
        public void Boarders_AreClearedByCrewButWreckAnUnattendedRoom()
        {
            var simulation = RunAgainst("enemy_boarder");
            var state = simulation.State;
            IsolateFromOtherThreats(state);
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0; // no further parties
            var marine = state.crew.Find(crew => crew.role == CrewRole.Marine);
            simulation.MoveCrew(marine.id, ShipSystemType.Weapons);
            foreach (var crew in state.crew) if (crew.id != marine.id) crew.currentRoom = ShipSystemType.Bridge;

            state.playerShip.GetRoom(ShipSystemType.Weapons).intruders = 2;
            state.playerShip.GetRoom(ShipSystemType.Sensors).intruders = 2;
            var sensorsBefore = state.playerShip.GetSystem(ShipSystemType.Sensors).damage;
            simulation.SetPaused(false);
            for (var i = 0; i < 60; i++) simulation.Tick(0.1f);

            Assert.That(state.playerShip.GetRoom(ShipSystemType.Weapons).intruders, Is.EqualTo(0f).Within(0.001f), "a marine should clear two boarders within six seconds");
            Assert.That(state.playerShip.GetRoom(ShipSystemType.Sensors).intruders, Is.GreaterThan(0f), "nobody is fighting the boarders in Sensors");
            Assert.That(state.playerShip.GetSystem(ShipSystemType.Sensors).damage, Is.GreaterThan(sensorsBefore), "unattended boarders should wreck the system");
        }

        [Test]
        public void Ward_PausesRegenerationRightAfterBeingHit()
        {
            var simulation = RunAgainst("enemy_cutter");
            var state = simulation.State;
            IsolateFromOtherThreats(state);
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
            var ship = state.playerShip;
            ship.ward = ship.maxWard;
            simulation.SetPaused(false);

            simulation.ApplyDamage(ship, ShipSystemType.Weapons, 4f, false);
            var afterHit = ship.ward;
            for (var i = 0; i < 50; i++) simulation.Tick(0.1f); // 5 s: still inside the 6 s recharge delay
            Assert.That(ship.ward, Is.EqualTo(afterHit).Within(0.001f), "ward must not regenerate during the recharge delay");

            for (var i = 0; i < 70; i++) simulation.Tick(0.1f); // 9 s total: the 6 s delay has expired
            Assert.That(ship.ward, Is.GreaterThan(afterHit), "ward should regenerate once the delay expires");
        }

        [Test]
        public void Ward_RechargeDelayAppliesToTheEnemyToo()
        {
            var simulation = RunAgainst("enemy_monitor");
            var state = simulation.State;
            IsolateFromOtherThreats(state);
            var enemy = state.enemyShip;
            enemy.ward = enemy.maxWard;
            simulation.SetPaused(false);

            simulation.ApplyDamage(enemy, ShipSystemType.Ward, 5f, false);
            var afterHit = enemy.ward;
            for (var i = 0; i < 20; i++) simulation.Tick(0.1f);
            Assert.That(enemy.ward, Is.EqualTo(afterHit).Within(0.001f));
        }

        private static readonly EncounterType[] EventTypes =
            { EncounterType.Rescue, EncounterType.Salvage, EncounterType.Trade, EncounterType.Checkpoint, EncounterType.Storm };

        [Test]
        public void EventLibrary_HasAtLeastTwentyEventsPerTypeAndRegionTaggedOnes()
        {
            var tagged = 0;
            foreach (var type in EventTypes)
            {
                Assert.That(ContentCatalog.EncounterIds(type).Count, Is.GreaterThanOrEqualTo(20), type.ToString());
                foreach (var id in ContentCatalog.EncounterIds(type))
                {
                    var encounter = ContentCatalog.GetEncounter(id);
                    if (encounter.regions != null && encounter.regions.Length > 0)
                    {
                        tagged++;
                        foreach (var region in encounter.regions) Assert.That(region, Is.InRange(1, ContentCatalog.RegionCount), id);
                    }
                }
            }
            Assert.That(tagged, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void EventAssignment_HonoursRegionTags()
        {
            string taggedId = null; int[] taggedRegions = null;
            foreach (var type in EventTypes)
            foreach (var id in ContentCatalog.EncounterIds(type))
            {
                var encounter = ContentCatalog.GetEncounter(id);
                if (encounter.regions != null && encounter.regions.Length > 0 && encounter.regions.Length < ContentCatalog.RegionCount) { taggedId = id; taggedRegions = encounter.regions; break; }
            }
            Assert.That(taggedId, Is.Not.Null);
            var inside = Array.IndexOf(taggedRegions, 1) >= 0 ? 1 : taggedRegions[0];
            var outside = 1;
            while (Array.IndexOf(taggedRegions, outside) >= 0) outside++;

            var seenInside = false; var seenOutside = false;
            for (var seed = 1; seed <= 300; seed++)
            {
                var insideNodes = ContentCatalog.CreateRoute(seed * 13, inside);
                ContentCatalog.AssignEncounterVariants(insideNodes, seed * 13, inside);
                if (insideNodes.Exists(node => node.encounterId == taggedId)) seenInside = true;
                var outsideNodes = ContentCatalog.CreateRoute(seed * 13, outside);
                ContentCatalog.AssignEncounterVariants(outsideNodes, seed * 13, outside);
                if (outsideNodes.Exists(node => node.encounterId == taggedId)) seenOutside = true;
            }
            Assert.That(seenInside, Is.True, taggedId + " should appear in region " + inside);
            Assert.That(seenOutside, Is.False, taggedId + " must not appear in region " + outside);
        }

        [Test]
        public void EventLibrary_EveryEventIsLocalizedAndStructurallyValid()
        {
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            void Localized(string key, string context)
            {
                Assert.That(ko.T(key), Is.Not.EqualTo(key), context + " missing Korean " + key);
                Assert.That(en.T(key), Is.Not.EqualTo(key), context + " missing English " + key);
                Assert.That(ko.T(key), Is.Not.EqualTo(en.T(key)), context + " has identical ko/en text for " + key);
            }

            foreach (var type in EventTypes)
            foreach (var id in ContentCatalog.EncounterIds(type))
            {
                var encounter = ContentCatalog.GetEncounter(id);
                Assert.That(encounter, Is.Not.Null, id);
                Assert.That(encounter.type, Is.EqualTo(type), id);
                Localized(encounter.titleKey, id);
                Localized(encounter.bodyKey, id);
                var visible = encounter.choices.FindAll(choice => !choice.hidden);
                Assert.That(visible.Count, Is.InRange(2, 4), id + " visible choices");
                Assert.That(visible.Exists(choice => choice.aetherCost == 0 && choice.suppliesCost == 0 && choice.ordnanceCost == 0 && choice.salvageCost == 0 && string.IsNullOrEmpty(choice.requiredTag)),
                    Is.True, id + " needs a free, untagged choice");
                foreach (var choice in encounter.choices)
                {
                    Localized(choice.textKey, id + "/" + choice.id);
                    Localized(choice.resultKey, id + "/" + choice.id);
                    Assert.That(choice.successChance, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), id + "/" + choice.id);
                    if (choice.successChance < 1f)
                    {
                        var failure = encounter.choices.Find(item => item.id == choice.failureChoiceId);
                        Assert.That(failure, Is.Not.Null, id + "/" + choice.id + " failure choice missing");
                        Assert.That(failure.hidden, Is.True, id + "/" + choice.id + " failure choice must be hidden");
                    }
                    if (choice.hidden) Assert.That(choice.successChance, Is.EqualTo(1f), id + "/" + choice.id + " hidden choices cannot gamble");
                }
            }
        }

        [Test]
        public void HiddenChoices_CannotBeChosen()
        {
            var simulation = GameSimulation.NewRun(Profile(), 3);
            simulation.State.phase = GamePhase.Encounter;
            simulation.State.activeEncounterId = "burning_ferry";
            var hidden = simulation.ActiveEncounter.choices.Find(choice => choice.hidden);
            Assert.That(hidden, Is.Not.Null);
            Assert.That(simulation.CanChoose(hidden), Is.False);
            Assert.That(simulation.ChooseEncounter(hidden.id).success, Is.False);
        }

        [Test]
        public void Gamble_AppliesTheHiddenFailureChoiceWhenTheRollFails()
        {
            var sawSuccess = false;
            var sawFailure = false;
            for (var seed = 1; seed < 200 && !(sawSuccess && sawFailure); seed++)
            {
                var simulation = GameSimulation.NewRun(Profile(), seed);
                simulation.State.phase = GamePhase.Encounter;
                simulation.State.activeEncounterId = "burning_ferry";
                var gamble = simulation.ActiveEncounter.choices.Find(choice => choice.successChance < 1f);
                var failure = simulation.ActiveEncounter.choices.Find(choice => choice.id == gamble.failureChoiceId);
                var hullBefore = simulation.State.playerShip.hull;
                var survivorsBefore = simulation.State.convoy.survivors;

                var result = simulation.ChooseEncounter(gamble.id);
                Assert.That(result.success, Is.True);
                if (result.messageKey == failure.resultKey)
                {
                    sawFailure = true;
                    Assert.That(simulation.State.playerShip.hull, Is.EqualTo(hullBefore + failure.hullDelta).Within(0.001f));
                    Assert.That(simulation.State.convoy.survivors, Is.EqualTo(survivorsBefore + failure.survivorDelta));
                }
                else
                {
                    sawSuccess = true;
                    Assert.That(result.messageKey, Is.EqualTo(gamble.resultKey));
                    Assert.That(simulation.State.convoy.survivors, Is.EqualTo(survivorsBefore + gamble.survivorDelta));
                }
            }
            Assert.That(sawSuccess && sawFailure, Is.True, "both gamble outcomes should occur across seeds");
        }

        [Test]
        public void ChoiceEffects_RepairRefitInstabilityAndEliteBattleApply()
        {
            var simulation = GameSimulation.NewRun(Profile(), 8);
            var state = simulation.State;
            state.playerShip.hull -= 10f;
            state.playerShip.armor -= 10f;
            state.playerShip.instability = 40f;
            state.squadrons[0].strength = 1;
            state.phase = GamePhase.Encounter;
            state.activeEncounterId = "refit_yard";
            var refit = simulation.ActiveEncounter.choices.Find(choice => choice.refitSquadrons);
            Assert.That(refit, Is.Not.Null);
            state.resources.salvage = 50;
            Assert.That(simulation.ChooseEncounter(refit.id).success, Is.True);
            Assert.That(state.squadrons[0].strength, Is.EqualTo(state.squadrons[0].maxStrength));

            state.phase = GamePhase.Encounter;
            state.activeEncounterId = "refit_yard";
            var plating = simulation.ActiveEncounter.choices.Find(choice => choice.armorDelta > 0);
            var armorBefore = state.playerShip.armor;
            Assert.That(simulation.ChooseEncounter(plating.id).success, Is.True);
            Assert.That(state.playerShip.armor, Is.EqualTo(Math.Min(state.playerShip.maxArmor, armorBefore + plating.armorDelta)).Within(0.001f));

            state.phase = GamePhase.Encounter;
            state.activeEncounterId = "ion_squall";
            var calm = simulation.ActiveEncounter.choices.Find(choice => choice.instabilityDelta < 0);
            state.resources.aether = 10;
            Assert.That(simulation.ChooseEncounter(calm.id).success, Is.True);
            Assert.That(state.playerShip.instability, Is.EqualTo(40f + calm.instabilityDelta).Within(0.001f));

            state.phase = GamePhase.Encounter;
            state.activeEncounterId = "blockade_toll";
            var fight = simulation.ActiveEncounter.choices.Find(choice => !choice.hidden && choice.startsBattle && choice.battleTier >= 2);
            Assert.That(fight, Is.Not.Null, "blockade_toll should offer an elite fight");
            Assert.That(simulation.ChooseEncounter(fight.id).success, Is.True);
            Assert.That(state.phase, Is.EqualTo(GamePhase.Combat));
            Assert.That(state.enemyShip.id, Is.EqualTo("enemy_cruiser").Or.EqualTo("enemy_monitor"));
        }

        [Test]
        public void EventAssignment_IsDeterministicAndAvoidsRepeatsAfterTheTutorial()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var first = GameSimulation.NewRun(profile, 777).State.routeNodes;
            var second = GameSimulation.NewRun(profile, 777).State.routeNodes;
            for (var i = 0; i < first.Count; i++) Assert.That(first[i].encounterId, Is.EqualTo(second[i].encounterId));

            foreach (var type in EventTypes)
            {
                var ids = first.FindAll(node => node.encounterType == type).ConvertAll(node => node.encounterId);
                var pool = ContentCatalog.EncounterIds(type).Count;
                var distinct = new System.Collections.Generic.HashSet<string>(ids);
                Assert.That(distinct.Count, Is.EqualTo(Math.Min(ids.Count, pool)), type + " repeats an event before exhausting its pool");
            }
            var baseline = new[] { "drifting_refugees", "ruined_dock", "free_port", "imperial_checkpoint", "storm_eye" };
            Assert.That(first.Exists(node => node.encounterId != null && node.encounterId.Length > 0 && Array.IndexOf(baseline, node.encounterId) < 0 && Array.IndexOf(EventTypes, node.encounterType) >= 0),
                Is.True, "a post-tutorial run should include at least one non-baseline event");
        }

        [Test]
        public void EventAssignment_FirstExpeditionKeepsBaselineEvents()
        {
            var profile = Profile(Difficulty.Story);
            profile.tutorialSeen = false;
            var nodes = GameSimulation.NewRun(profile, GameSimulation.FirstExpeditionSeed).State.routeNodes;
            var baseline = new[] { "drifting_refugees", "ruined_dock", "free_port", "imperial_checkpoint", "storm_eye" };
            foreach (var node in nodes)
            {
                if (Array.IndexOf(EventTypes, node.encounterType) < 0) continue;
                Assert.That(baseline, Does.Contain(node.encounterId), node.id);
            }
        }

        [Test]
        public void Regions_FourAreDefinedAndLocalized()
        {
            Assert.That(ContentCatalog.RegionCount, Is.EqualTo(6));
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            for (var index = 1; index <= ContentCatalog.RegionCount; index++)
            {
                var region = ContentCatalog.GetRegion(index);
                Assert.That(region, Is.Not.Null, "region " + index);
                Assert.That(region.index, Is.EqualTo(index));
                Assert.That(ko.T(region.nameKey), Is.Not.EqualTo(region.nameKey));
                Assert.That(en.T(region.nameKey), Is.Not.EqualTo(region.nameKey));
            }
        }

        [Test]
        public void Route_RegionOneMatchesTheLegacyGenerator()
        {
            var legacy = ContentCatalog.CreateRoute(32838);
            var region = ContentCatalog.CreateRoute(32838, 1);
            Assert.That(region.Count, Is.EqualTo(legacy.Count));
            for (var i = 0; i < legacy.Count; i++)
            {
                Assert.That(region[i].encounterType, Is.EqualTo(legacy[i].encounterType), region[i].id);
                Assert.That(region[i].weather, Is.EqualTo(legacy[i].weather), region[i].id);
                Assert.That(region[i].aetherCost, Is.EqualTo(legacy[i].aetherCost), region[i].id);
            }
        }

        private static float Share(int regionIndex, Func<RouteNodeState, bool> predicate, Func<RouteNodeState, bool> population)
        {
            var hits = 0; var total = 0;
            for (var seed = 1; seed <= 150; seed++)
            {
                foreach (var node in ContentCatalog.CreateRoute(seed * 31, regionIndex))
                {
                    if (!population(node)) continue;
                    total++;
                    if (predicate(node)) hits++;
                }
            }
            return total == 0 ? 0f : (float)hits / total;
        }

        [Test]
        public void Route_LaterRegionsBiasWeatherAndEncounters()
        {
            Func<RouteNodeState, bool> generated = node => node.column >= 1 && node.column <= 6;
            Func<RouteNodeState, bool> rollable = node => node.column >= 1 && node.column <= 6 && node.column != 2 && node.column != 6;

            var stormyInCorridor = Share(2, n => n.weather == WeatherType.Thunderhead || n.weather == WeatherType.Turbulence, generated);
            var stormyInDawn = Share(1, n => n.weather == WeatherType.Thunderhead || n.weather == WeatherType.Turbulence, generated);
            Assert.That(stormyInCorridor, Is.GreaterThan(0.45f));
            Assert.That(stormyInCorridor, Is.GreaterThan(stormyInDawn + 0.1f));

            var icyInHeights = Share(3, n => n.weather == WeatherType.Icing || n.weather == WeatherType.CloudCover, generated);
            Assert.That(icyInHeights, Is.GreaterThan(0.45f));

            var checkpointsInCordon = Share(4, n => n.encounterType == EncounterType.Checkpoint, rollable);
            var checkpointsInDawn = Share(1, n => n.encounterType == EncounterType.Checkpoint, rollable);
            Assert.That(checkpointsInCordon, Is.GreaterThan(checkpointsInDawn + 0.08f));

            var rescuesInHeights = Share(3, n => n.encounterType == EncounterType.Rescue || n.encounterType == EncounterType.Salvage, rollable);
            var rescuesInDawn = Share(1, n => n.encounterType == EncounterType.Rescue || n.encounterType == EncounterType.Salvage, rollable);
            Assert.That(rescuesInHeights, Is.GreaterThan(rescuesInDawn + 0.08f));
        }

        private static void WinCurrentBattle(GameSimulation simulation)
        {
            simulation.ApplyDamage(simulation.State.enemyShip, ShipSystemType.AetherCore, 999f, true);
            simulation.SetPaused(false);
            simulation.Tick(0.1f);
        }

        [Test]
        public void Campaign_GateVictoryAdvancesToTheNextRegionWithAPortStop()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 505);
            var state = simulation.State;
            Assert.That(state.regionCount, Is.EqualTo(6));
            Assert.That(state.regionIndex, Is.EqualTo(1));
            var firstRoute = state.routeNodes.ConvertAll(node => node.encounterType.ToString() + node.weather);

            state.playerShip.hull = 10f;
            state.playerShip.armor = 2f;
            state.travelCount = 7;
            state.stormColumn = 4;
            var aetherBefore = state.resources.aether;
            var maxHullBefore = state.playerShip.maxHull;
            simulation.BeginCombat(2, true);
            WinCurrentBattle(simulation);

            Assert.That(state.phase, Is.EqualTo(GamePhase.Port), "the campaign should dock at the port after the first gate");
            Assert.That(simulation.DepartPort().success, Is.True);
            Assert.That(state.phase, Is.EqualTo(GamePhase.RouteMap), "the campaign should continue after the port");
            Assert.That(state.regionIndex, Is.EqualTo(2));
            Assert.That(state.travelCount, Is.EqualTo(0));
            Assert.That(state.stormColumn, Is.EqualTo(-1));
            Assert.That(state.currentNodeId, Is.EqualTo("n0_1"));
            Assert.That(state.enemyShip, Is.Null);
            Assert.That(state.playerShip.hull, Is.GreaterThan(10f));
            Assert.That(state.playerShip.armor, Is.EqualTo(state.playerShip.maxArmor).Within(0.001f));
            Assert.That(state.resources.aether, Is.GreaterThan(aetherBefore));
            Assert.That(state.routeNodes.ConvertAll(node => node.encounterType.ToString() + node.weather), Is.Not.EqualTo(firstRoute), "the next region needs its own route");
            Assert.That(state.combatLog.Exists(entry => entry.key == "log.region_cleared"), Is.True);
            Assert.That(state.playerShip.hull, Is.EqualTo(state.playerShip.maxHull).Within(0.001f), "port stop should fully repair the hull");
            Assert.That(state.playerShip.coreOutput, Is.EqualTo(12 + 1), "each cleared region should grow the core output");
            Assert.That(state.playerShip.maxHull, Is.EqualTo(maxHullBefore + 3f).Within(0.001f), "each cleared region should grow the hull");
            Assert.That(state.playerShip.AllocatedPower(), Is.LessThanOrEqualTo(state.playerShip.coreOutput));
            Assert.That(state.resources.ordnance, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void Campaign_LastRegionGateVictoryEndsTheRun()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 506);
            simulation.State.regionIndex = simulation.State.regionCount;
            simulation.BeginCombat(2, true);
            WinCurrentBattle(simulation);
            Assert.That(simulation.State.phase, Is.EqualTo(GamePhase.Victory));
        }

        [Test]
        public void Campaign_FirstExpeditionIsASingleRegion()
        {
            var profile = Profile(Difficulty.Story);
            profile.tutorialSeen = false;
            var simulation = GameSimulation.NewRun(profile, GameSimulation.FirstExpeditionSeed);
            Assert.That(simulation.State.regionCount, Is.EqualTo(1));
            simulation.BeginCombat(2, true);
            WinCurrentBattle(simulation);
            Assert.That(simulation.State.phase, Is.EqualTo(GamePhase.Victory));
        }

        [Test]
        public void Campaign_EnemiesScaleWithTheRegion()
        {
            var profile = Profile(Difficulty.Story);
            profile.tutorialSeen = false; // variants disabled: always the cutter, so hulls are comparable
            var early = GameSimulation.NewRun(profile, 9);
            early.BeginCombat(1, false);
            var late = GameSimulation.NewRun(profile, 9);
            late.State.regionIndex = 3;
            late.BeginCombat(1, false);
            Assert.That(late.State.enemyShip.id, Is.EqualTo(early.State.enemyShip.id));
            Assert.That(late.State.enemyShip.maxHull, Is.GreaterThan(early.State.enemyShip.maxHull * 1.15f));
            Assert.That(late.State.enemyShip.hull, Is.EqualTo(late.State.enemyShip.maxHull).Within(0.001f));
        }

        [Test]
        public void SaveService_RoundTripsRegionFields()
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-ark-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var service = new SaveService(root);
                var profile = Profile();
                profile.tutorialSeen = true;
                var run = GameSimulation.NewRun(profile, 12).State;
                run.regionIndex = 3;
                run.totalTravelCount = 15;
                service.SaveRun(run);
                var loaded = service.LoadRun();
                Assert.That(loaded.regionIndex, Is.EqualTo(3));
                Assert.That(loaded.regionCount, Is.EqualTo(ContentCatalog.RegionCount));
                Assert.That(loaded.totalTravelCount, Is.EqualTo(15));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void GateReinforcement_GrowsWithTheRegion()
        {
            var profile = Profile(Difficulty.Story);
            profile.tutorialSeen = false; // baseline ships only, so the bonus is measurable
            var regular = GameSimulation.NewRun(profile, 21);
            regular.BeginCombat(2, false);
            var baseHull = regular.State.enemyShip.maxHull;

            var firstGate = GameSimulation.NewRun(profile, 21);
            firstGate.BeginCombat(2, true);
            var firstBonus = firstGate.State.enemyShip.maxHull - baseHull;
            Assert.That(firstBonus, Is.EqualTo(6f).Within(0.001f), "the first gate should be only lightly reinforced");

            var lateGate = GameSimulation.NewRun(profile, 21);
            lateGate.State.regionIndex = 3;
            lateGate.BeginCombat(2, true);
            var scaledBase = baseHull * ContentCatalog.GetRegion(3).enemyStatMultiplier;
            Assert.That(lateGate.State.enemyShip.maxHull - scaledBase, Is.EqualTo(10f).Within(0.1f), "later gates gain more reinforcement on top of region scaling");
        }

        [Test]
        public void EnemyFirepower_ScalesWithTheRegion()
        {
            var profile = Profile(Difficulty.Standard);
            profile.tutorialSeen = false; // always the cutter
            var early = GameSimulation.NewRun(profile, 33);
            early.BeginCombat(1, false);
            var late = GameSimulation.NewRun(profile, 33);
            late.State.regionIndex = 3;
            late.BeginCombat(1, false);

            var earlyShot = early.EnemyShotDamage();
            var lateShot = late.EnemyShotDamage();
            Assert.That(earlyShot, Is.GreaterThan(0f));
            Assert.That(lateShot, Is.EqualTo(earlyShot * ContentCatalog.GetRegion(3).enemyDamageMultiplier).Within(0.01f),
                "enemy shot damage must follow the region multiplier, not just enemy hull");
        }

        private static string FixtureRoot(string version, [CallerFilePath] string sourcePath = "")
        {
            // The source path resolves the repository even when the tests run from an external harness directory.
            var candidates = new[] { Path.GetDirectoryName(sourcePath) ?? "", Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (var start in candidates)
            {
                var directory = new DirectoryInfo(start);
                while (directory != null)
                {
                    var probe = Path.Combine(directory.FullName, "Assets", "_Project", "Tests", "EditMode", "Fixtures", version);
                    if (Directory.Exists(probe)) return probe;
                    directory = directory.Parent;
                }
            }
            Assert.Fail("Fixture folder not found for " + version + "; run Aether Ark/Write Save Fixtures in the editor.");
            return null;
        }

        [Test]
        public void SaveFixtures_V1ProfileAndRunLoadThroughTheCurrentMigrations()
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-ark-fixture-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                // Copy so the test can never mutate the committed fixture.
                foreach (var file in Directory.GetFiles(FixtureRoot("v1"), "*.json"))
                    File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
                var service = new SaveService(root);

                var profile = service.LoadProfile();
                Assert.That(profile.schemaVersion, Is.GreaterThanOrEqualTo(1));
                Assert.That(profile.captainName, Is.EqualTo("Fixture Captain"));
                Assert.That(profile.captainLineage, Is.EqualTo(CrewLineage.Dwarf));
                Assert.That(profile.supportShip, Is.EqualTo(SupportShipType.Pathfinder));
                Assert.That(profile.language, Is.EqualTo(Language.English));
                Assert.That(profile.tutorialSeen, Is.True);
                Assert.That(profile.accessibility.combatSpeed, Is.EqualTo(1.25f).Within(0.001f));
                Assert.That(profile.accessibility.highContrast, Is.True);

                var run = service.LoadRun();
                Assert.That(run, Is.Not.Null, "a suspended v1 run must still load");
                Assert.That(run.phase, Is.EqualTo(GamePhase.RouteMap));
                Assert.That(run.seed, Is.EqualTo(424242));
                Assert.That(run.regionIndex, Is.EqualTo(2));
                Assert.That(run.regionCount, Is.EqualTo(4), "a save keeps the campaign length it was started with");
                Assert.That(run.totalTravelCount, Is.EqualTo(9));
                Assert.That(run.currentNodeId, Is.EqualTo("n2_1"));
                Assert.That(run.routeNodes.Count, Is.EqualTo(20));
                Assert.That(run.crew.Count, Is.EqualTo(6));
                Assert.That(run.crew[4].IsDowned, Is.True);
                Assert.That(run.squadrons[0].strength, Is.EqualTo(2));
                Assert.That(run.resources.salvage, Is.EqualTo(37));
                Assert.That(run.playerShip.hull, Is.EqualTo(21f).Within(0.001f));
                Assert.That(run.playerShip.GetSystem(ShipSystemType.Weapons).damage, Is.EqualTo(30f).Within(0.001f));
                Assert.That(run.playerShip.GetRoom(ShipSystemType.Weapons).fire, Is.EqualTo(12f).Within(0.001f));
                Assert.That(ContentCatalog.GetDeckPlan(run.playerShip.id), Is.Not.Null, "a loaded ship must still map to a deck plan");

                // A loaded run must be playable: resume, travel once and reach a real phase.
                var simulation = new GameSimulation(run);
                var destination = run.routeNodes.Find(simulation.CanTravelTo);
                Assert.That(destination, Is.Not.Null, "the fixture's current node must have a reachable neighbour");
                Assert.That(simulation.TravelTo(destination.id).success, Is.True);
                Assert.That(run.phase, Is.EqualTo(GamePhase.Combat).Or.EqualTo(GamePhase.Encounter).Or.EqualTo(GamePhase.RouteMap));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void ModuleLibrary_HasThirtyLocalizedDistinctModulesWithEffects()
        {
            var ids = ContentCatalog.ModuleIds();
            Assert.That(ids.Count, Is.GreaterThanOrEqualTo(30));
            Assert.That(new System.Collections.Generic.HashSet<string>(ids).Count, Is.EqualTo(ids.Count), "module ids must be unique");
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            foreach (var id in ids)
            {
                var module = ContentCatalog.GetModule(id);
                Assert.That(module.cost, Is.GreaterThan(0), id);
                Assert.That(module.tier, Is.InRange(1, 3), id);
                Assert.That(ko.T(module.nameKey), Is.Not.EqualTo(module.nameKey), id);
                Assert.That(en.T(module.nameKey), Is.Not.EqualTo(module.nameKey), id);
                Assert.That(ko.T(module.descriptionKey), Is.Not.EqualTo(module.descriptionKey), id);
                Assert.That(en.T(module.descriptionKey), Is.Not.EqualTo(module.descriptionKey), id);
                Assert.That(ModuleRules.HasAnyEffect(module), Is.True, id + " does nothing");
            }
        }

        [Test]
        public void ModuleOffers_AreDeterministicDistinctAndExcludeInstalled()
        {
            var installed = new System.Collections.Generic.List<string> { "reinforced_ribs" };
            var first = ContentCatalog.OfferModules(1234, 2, installed);
            var second = ContentCatalog.OfferModules(1234, 2, installed);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Count, Is.EqualTo(3));
            Assert.That(new System.Collections.Generic.HashSet<string>(first).Count, Is.EqualTo(3));
            Assert.That(first, Does.Not.Contain("reinforced_ribs"));
            Assert.That(ContentCatalog.OfferModules(1234, 3, installed), Is.Not.EqualTo(first), "offers should change with the region");
        }

        private static GameSimulation RunAtPort()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 600);
            simulation.BeginCombat(2, true);
            WinCurrentBattle(simulation);
            Assert.That(simulation.State.phase, Is.EqualTo(GamePhase.Port), "clearing a gate should dock at the port");
            return simulation;
        }

        [Test]
        public void Port_PurchaseAppliesFlatStatsAndRespectsSalvageSlotsAndDuplicates()
        {
            var simulation = RunAtPort();
            var state = simulation.State;
            state.resources.salvage = 100;
            var hullBefore = state.playerShip.maxHull;

            Assert.That(simulation.PurchaseModule("reinforced_ribs").success, Is.True);
            Assert.That(state.installedModules, Does.Contain("reinforced_ribs"));
            Assert.That(state.playerShip.maxHull, Is.EqualTo(hullBefore + ContentCatalog.GetModule("reinforced_ribs").maxHull).Within(0.001f));
            Assert.That(state.playerShip.hull, Is.EqualTo(state.playerShip.maxHull).Within(0.001f), "a hull module should also fill the new capacity at the port");
            Assert.That(state.resources.salvage, Is.EqualTo(100 - ContentCatalog.GetModule("reinforced_ribs").cost));
            Assert.That(simulation.PurchaseModule("reinforced_ribs").success, Is.False, "no duplicates");

            state.resources.salvage = 0;
            Assert.That(simulation.PurchaseModule("rifled_barrels").success, Is.False, "unaffordable");
            state.resources.salvage = 500;
            foreach (var id in new[] { "rifled_barrels", "damage_control_teams", "long_range_array" })
                Assert.That(simulation.PurchaseModule(id).success, Is.True, id);
            Assert.That(simulation.PurchaseModule("extended_hangar").success, Is.False, "slots are full");
            Assert.That(state.installedModules.Count, Is.EqualTo(state.playerShip.moduleSlots));

            Assert.That(simulation.DepartPort().success, Is.True);
            Assert.That(state.phase, Is.EqualTo(GamePhase.RouteMap));
            Assert.That(simulation.PurchaseModule("extended_hangar").success, Is.False, "purchases only happen at the port");
        }

        [Test]
        public void Modules_ChangeCombatAndRouteRulesThroughTheModifierSet()
        {
            var simulation = RunAtPort();
            var state = simulation.State;
            state.resources.salvage = 500;
            var baseDamage = simulation.PlayerShotDamage();
            Assert.That(simulation.PurchaseModule("rifled_barrels").success, Is.True);
            Assert.That(simulation.PlayerShotDamage(), Is.EqualTo(baseDamage * ContentCatalog.GetModule("rifled_barrels").weaponDamage).Within(0.001f));

            Assert.That(simulation.PurchaseModule("navigator_charts").success, Is.True);
            simulation.DepartPort();
            var expensive = state.routeNodes.Find(node => node.aetherCost >= 2);
            if (expensive != null) Assert.That(simulation.TravelCost(expensive), Is.EqualTo(expensive.aetherCost - 1));
            var cheap = state.routeNodes.Find(node => node.aetherCost == 1);
            Assert.That(simulation.TravelCost(cheap), Is.EqualTo(1), "the discount never drops a jump below one aether");

            state.installedModules.Add("escort_doctrine");
            state.installedModules.Add("salvage_cranes");
            var salvageBefore = state.resources.salvage;
            simulation.BeginCombat(1, false);
            Assert.That(state.interceptCharges, Is.EqualTo(ContentCatalog.GetModule("escort_doctrine").interceptCharges));
            WinCurrentBattle(simulation);
            Assert.That(state.resources.salvage - salvageBefore, Is.EqualTo(9 + ContentCatalog.GetModule("salvage_cranes").salvageReward));
        }

        [Test]
        public void Modules_RaiseSquadronStrengthAndPersistInSaves()
        {
            var simulation = RunAtPort();
            var state = simulation.State;
            state.resources.salvage = 500;
            var maxBefore = state.squadrons[0].maxStrength;
            Assert.That(simulation.PurchaseModule("extended_hangar").success, Is.True);
            Assert.That(state.squadrons[0].maxStrength, Is.EqualTo(maxBefore + 1));
            Assert.That(state.squadrons[0].strength, Is.EqualTo(state.squadrons[0].maxStrength));

            var root = Path.Combine(Path.GetTempPath(), "aether-ark-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var service = new SaveService(root);
                service.SaveRun(state);
                var loaded = service.LoadRun();
                Assert.That(loaded.installedModules, Is.EqualTo(state.installedModules));
                Assert.That(loaded.playerShip.moduleSlots, Is.EqualTo(state.playerShip.moduleSlots));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void Modules_BoardingDefenceClearsIntrudersFaster()
        {
            float TimeToClear(bool armoury)
            {
                var simulation = RunAgainst("enemy_boarder");
                var state = simulation.State;
                IsolateFromOtherThreats(state);
                state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
                if (armoury) state.installedModules.Add("boarding_armory");
                var marine = state.crew.Find(crew => crew.role == CrewRole.Marine);
                simulation.MoveCrew(marine.id, ShipSystemType.Weapons);
                state.playerShip.GetRoom(ShipSystemType.Weapons).intruders = 3;
                simulation.SetPaused(false);
                var elapsed = 0f;
                while (state.playerShip.GetRoom(ShipSystemType.Weapons).intruders > 0 && elapsed < 60f) { simulation.Tick(0.1f); elapsed += 0.1f; }
                return elapsed;
            }
            Assert.That(TimeToClear(true), Is.LessThan(TimeToClear(false) * 0.75f));
        }

        [Test]
        public void WeaponLibrary_HasEighteenLocalizedValidWeapons()
        {
            var ids = ContentCatalog.WeaponIds();
            Assert.That(ids.Count, Is.GreaterThanOrEqualTo(18));
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            foreach (var id in ids)
            {
                var weapon = ContentCatalog.GetWeapon(id);
                Assert.That(ko.T(weapon.nameKey), Is.Not.EqualTo(weapon.nameKey), id);
                Assert.That(en.T(weapon.nameKey), Is.Not.EqualTo(weapon.nameKey), id);
                Assert.That(ko.T(weapon.descriptionKey), Is.Not.EqualTo(weapon.descriptionKey), id);
                Assert.That(weapon.damage, Is.GreaterThan(0f), id);
                Assert.That(weapon.cooldown, Is.GreaterThan(0f), id);
                Assert.That(weapon.powerCost, Is.InRange(1, 3), id);
                Assert.That(weapon.cost, Is.GreaterThan(0), id);
            }
        }

        [Test]
        public void Loadout_NewRunMountsTheAetherCannonAndOldSavesAreBackfilled()
        {
            var tutorialProfile = Profile(Difficulty.Story);
            tutorialProfile.tutorialSeen = false;
            var tutorial = GameSimulation.NewRun(tutorialProfile, GameSimulation.FirstExpeditionSeed).State;
            Assert.That(tutorial.weaponSlots.Count, Is.EqualTo(2), "the tutorial expedition mounts both starting weapons to teach the slots");
            Assert.That(tutorial.weaponSlots[1].weaponId, Is.EqualTo("ward_lance"));

            var profile = Profile();
            profile.tutorialSeen = true;
            var state = GameSimulation.NewRun(profile, 4).State;
            Assert.That(state.weaponSlots.ConvertAll(slot => slot.weaponId), Is.EqualTo(new[] { "aether_cannon", "ward_lance" }), "every run sails with the cannon and the ward lance");
            Assert.That(state.playerShip.weaponHardpoints, Is.EqualTo(2));

            var root = Path.Combine(Path.GetTempPath(), "aether-ark-weapons-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var service = new SaveService(root);
                state.weaponSlots.Clear(); // simulate a save written before weapons existed
                service.SaveRun(state);
                var loaded = service.LoadRun();
                Assert.That(loaded.weaponSlots.Count, Is.EqualTo(2));
                Assert.That(loaded.weaponSlots[0].weaponId, Is.EqualTo("aether_cannon"));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static GameSimulation ArmedRun(params string[] weaponIds)
        {
            var simulation = RunAgainst("enemy_cutter");
            var state = simulation.State;
            IsolateFromOtherThreats(state);
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
            state.weaponSlots.Clear();
            foreach (var id in weaponIds) state.weaponSlots.Add(new WeaponSlotState { weaponId = id });
            state.playerShip.weaponHardpoints = Math.Max(2, weaponIds.Length);
            return simulation;
        }

        [Test]
        public void Power_GatesMountedWeaponsInSlotOrder()
        {
            var simulation = ArmedRun("aether_cannon", "heavy_cannon"); // costs 2 + 2
            var weapons = simulation.State.playerShip.GetSystem(ShipSystemType.Weapons);
            weapons.power = 2;
            Assert.That(simulation.IsWeaponPowered(0), Is.True);
            Assert.That(simulation.IsWeaponPowered(1), Is.False);
            Assert.That(simulation.FireWeapon(1, ShipSystemType.Weapons).success, Is.False, "an unpowered slot cannot fire");
            weapons.power = 4;
            Assert.That(simulation.IsWeaponPowered(1), Is.True);
        }

        [Test]
        public void Fire_UsesPerSlotCooldownsAndFireAllFiresEveryReadySlot()
        {
            var simulation = ArmedRun("aether_cannon", "ward_lance");
            simulation.State.playerShip.GetSystem(ShipSystemType.Weapons).power = 4;
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.True);
            Assert.That(simulation.State.weaponSlots[0].cooldown, Is.GreaterThan(0f));
            Assert.That(simulation.State.weaponSlots[1].cooldown, Is.EqualTo(0f));
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.False, "slot 0 is cooling down");
            Assert.That(simulation.FireAllReady(ShipSystemType.Weapons).success, Is.True, "slot 1 is still ready");
            Assert.That(simulation.State.weaponSlots[1].cooldown, Is.GreaterThan(0f));
            Assert.That(simulation.State.hasFiredWeapon, Is.True);
        }

        private static float TotalDamageTaken(ShipState before, ShipState after)
        {
            return (before.ward - after.ward) + (before.armor - after.armor) + (before.hull - after.hull);
        }

        [Test]
        public void WeaponFamilies_LancesStripWardsAndPiercersBiteArmor()
        {
            var lanceRun = ArmedRun("ward_lance");
            var lance = ContentCatalog.GetWeapon("ward_lance");
            var enemy = lanceRun.State.enemyShip;
            enemy.ward = 20f; enemy.maxWard = 20f;
            lanceRun.ApplyWeaponHit(lance, enemy, ShipSystemType.Weapons, lance.damage);
            Assert.That(20f - enemy.ward, Is.EqualTo(lance.damage * lance.wardMultiplier).Within(0.01f), "a lance hits wards with its multiplier");

            var piercerRun = ArmedRun("bolt_thrower");
            var piercer = ContentCatalog.GetWeapon("bolt_thrower");
            var target = piercerRun.State.enemyShip;
            target.ward = 0f; target.armor = 30f; target.maxArmor = 30f;
            var hullBefore = target.hull;
            piercerRun.ApplyWeaponHit(piercer, target, ShipSystemType.Weapons, piercer.damage);
            Assert.That(hullBefore - target.hull, Is.EqualTo(piercer.damage * piercer.armorPiercing).Within(0.01f), "the piercing fraction bypasses armor");
            Assert.That(30f - target.armor, Is.EqualTo(piercer.damage * (1f - piercer.armorPiercing)).Within(0.01f));
        }

        [Test]
        public void Missiles_IgnoreWardsAndConsumeOrdnance()
        {
            var simulation = ArmedRun("rocket_pod");
            var state = simulation.State;
            state.playerShip.GetSystem(ShipSystemType.Weapons).power = 4;
            var rocket = ContentCatalog.GetWeapon("rocket_pod");
            Assert.That(rocket.ignoresWard, Is.True);
            Assert.That(rocket.ordnancePerShot, Is.EqualTo(1));
            state.resources.ordnance = 1;
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.True);
            Assert.That(state.resources.ordnance, Is.EqualTo(0));
            state.weaponSlots[0].cooldown = 0f;
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.False, "no ordnance left");

            var enemy = state.enemyShip;
            enemy.ward = 10f; enemy.armor = 0f;
            var hullBefore = enemy.hull;
            simulation.ApplyWeaponHit(rocket, enemy, ShipSystemType.Weapons, rocket.damage);
            Assert.That(enemy.ward, Is.EqualTo(10f).Within(0.001f), "missiles bypass the ward entirely");
            Assert.That(hullBefore - enemy.hull, Is.EqualTo(rocket.damage).Within(0.01f));
        }

        [Test]
        public void Flak_GrantsAnInterceptChargePerShot()
        {
            var simulation = ArmedRun("flak_battery");
            var state = simulation.State;
            state.playerShip.GetSystem(ShipSystemType.Weapons).power = 4;
            state.interceptCharges = 0;
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.True);
            Assert.That(state.interceptCharges, Is.EqualTo(1));
        }

        [Test]
        public void Port_SellsWeaponsIntoHardpointsAndReplacesTheLastOneWithARefund()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 606);
            var state = simulation.State;
            simulation.BeginCombat(2, true);
            WinCurrentBattle(simulation);
            Assert.That(state.phase, Is.EqualTo(GamePhase.Port));

            var offers = simulation.PortWeaponOffers();
            Assert.That(offers.Count, Is.EqualTo(2));
            Assert.That(offers, Is.EqualTo(simulation.PortWeaponOffers()), "offers are deterministic");
            state.resources.salvage = 200;
            var first = ContentCatalog.GetWeapon(offers[0]);
            var firstReplaced = state.weaponSlots.Count >= state.playerShip.weaponHardpoints ? ContentCatalog.GetWeapon(state.weaponSlots[state.weaponSlots.Count - 1].weaponId) : null;
            var salvageBefore = state.resources.salvage;
            Assert.That(simulation.PurchaseWeapon(offers[0]).success, Is.True);
            Assert.That(state.weaponSlots.Count, Is.EqualTo(2));
            Assert.That(state.weaponSlots[1].weaponId, Is.EqualTo(offers[0]));
            Assert.That(state.resources.salvage, Is.EqualTo(salvageBefore - first.cost + (firstReplaced != null ? firstReplaced.cost / 2 : 0)));

            var second = ContentCatalog.GetWeapon(offers[1]);
            var replaced = ContentCatalog.GetWeapon(state.weaponSlots[state.weaponSlots.Count - 1].weaponId);
            salvageBefore = state.resources.salvage;
            Assert.That(simulation.PurchaseWeapon(offers[1]).success, Is.True, "a full loadout replaces the last hardpoint");
            Assert.That(state.weaponSlots.Count, Is.EqualTo(2));
            Assert.That(state.weaponSlots[1].weaponId, Is.EqualTo(offers[1]));
            Assert.That(state.resources.salvage, Is.EqualTo(salvageBefore - second.cost + replaced.cost / 2));
            Assert.That(simulation.PurchaseWeapon(offers[1]).success, Is.False, "already mounted");
        }

        [Test]
        public void EnemyLoadouts_FireThroughTheSameWeaponRules()
        {
            var simulation = RunAgainst("enemy_cruiser");
            var state = simulation.State;
            Assert.That(state.enemyShip.weaponSlots.Count, Is.GreaterThanOrEqualTo(1));
            state.currentWeather = WeatherType.Clear;
            state.weatherHazardTimer = 999f;
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
            state.playerShip.ward = 0f; state.playerShip.armor = 0f;
            state.playerShip.GetSystem(ShipSystemType.Ward).power = 0;
            var hullBefore = state.playerShip.hull;
            simulation.SetPaused(false);
            for (var i = 0; i < 300; i++) simulation.Tick(0.1f);
            Assert.That(state.playerShip.hull, Is.LessThan(hullBefore), "enemy weapons should land within thirty seconds");
            Assert.That(state.combatLog.Exists(entry => entry.key == "log.enemy_hit"), Is.True);
        }

        [Test]
        public void SparePower_ShortensWeaponCooldowns()
        {
            var simulation = ArmedRun("aether_cannon"); // costs 2
            var weapons = simulation.State.playerShip.GetSystem(ShipSystemType.Weapons);
            var cannon = ContentCatalog.GetWeapon("aether_cannon");

            weapons.power = 2;
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.True);
            Assert.That(simulation.State.weaponSlots[0].cooldown, Is.EqualTo(cannon.cooldown).Within(0.001f), "no spare power: base cooldown");

            simulation.State.weaponSlots[0].cooldown = 0f;
            weapons.power = 4; // two spare points
            Assert.That(simulation.FireWeapon(0, ShipSystemType.Weapons).success, Is.True);
            Assert.That(simulation.State.weaponSlots[0].cooldown, Is.LessThan(cannon.cooldown * 0.85f), "spare weapons power should speed reloading");
        }

        [Test]
        public void Flagships_ThreeAreDefinedLocalizedAndWithinPowerBudget()
        {
            var ids = ContentCatalog.FlagshipIds();
            Assert.That(ids, Is.EquivalentTo(new[] { "ship_vanguard", "ship_bastion", "ship_zephyr" }));
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            foreach (var id in ids)
            {
                var definition = ContentCatalog.GetFlagship(id);
                Assert.That(ko.T(definition.nameKey), Is.Not.EqualTo(definition.nameKey), id);
                Assert.That(en.T(definition.descriptionKey), Is.Not.EqualTo(definition.descriptionKey), id);
                var ship = ContentCatalog.CreateFlagship(id);
                Assert.That(ship.id, Is.EqualTo(id));
                Assert.That(ship.systems.Count, Is.EqualTo(10), id);
                Assert.That(ship.AllocatedPower(), Is.LessThanOrEqualTo(ship.coreOutput), id + " over-allocates power");
                Assert.That(ship.weaponHardpoints, Is.GreaterThanOrEqualTo(definition.startingWeapons.Length), id + " must fit its starting weapons");
                foreach (var weapon in definition.startingWeapons) Assert.That(ContentCatalog.GetWeapon(weapon), Is.Not.Null, id + " starting weapon " + weapon);
            }
        }

        [Test]
        public void NewRun_UsesTheChosenFlagshipWithItsLoadoutAndSlots()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            profile.campaignVictories = 1;
            profile.flagshipId = "ship_bastion";
            var state = GameSimulation.NewRun(profile, 71).State;
            Assert.That(state.playerShip.id, Is.EqualTo("ship_bastion"));
            Assert.That(state.playerShip.weaponHardpoints, Is.EqualTo(3));
            Assert.That(state.playerShip.moduleSlots, Is.EqualTo(5));
            Assert.That(state.weaponSlots.ConvertAll(slot => slot.weaponId), Is.EqualTo(new[] { "heavy_cannon" }), "the Bastion sails with its heavy cannon only");
            Assert.That(ContentCatalog.GetDeckPlan(state.playerShip.id), Is.Not.Null);

            profile.flagshipId = "ship_zephyr";
            var zephyr = GameSimulation.NewRun(profile, 72).State;
            Assert.That(zephyr.playerShip.id, Is.EqualTo("ship_zephyr"));
            Assert.That(zephyr.playerShip.weaponHardpoints, Is.EqualTo(2));
            Assert.That(zephyr.weaponSlots.ConvertAll(slot => slot.weaponId), Is.EqualTo(new[] { "aether_cannon", "ward_lance" }));
            Assert.That(zephyr.squadrons.Find(s => s.type == SquadronType.Interceptor).maxStrength, Is.EqualTo(5), "the Zephyr's interceptors start larger");
        }

        [Test]
        public void NewRun_FallsBackToTheVanguardWhenLockedUnknownOrTutorial()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            profile.flagshipId = "ship_zephyr"; // locked: no campaign victory yet
            Assert.That(GameSimulation.NewRun(profile, 73).State.playerShip.id, Is.EqualTo("ship_vanguard"));
            profile.flagshipId = "ship_nonsense";
            Assert.That(GameSimulation.NewRun(profile, 74).State.playerShip.id, Is.EqualTo("ship_vanguard"));

            var tutorial = Profile(Difficulty.Story);
            tutorial.tutorialSeen = false;
            tutorial.flagshipId = "ship_bastion";
            Assert.That(GameSimulation.NewRun(tutorial, GameSimulation.FirstExpeditionSeed).State.playerShip.id, Is.EqualTo("ship_vanguard"));
        }

        [Test]
        public void UnlockRules_OpenTheBastionAfterTheTutorialAndTheZephyrAfterACampaign()
        {
            var fresh = Profile();
            fresh.tutorialSeen = false;
            Assert.That(UnlockRules.IsFlagshipUnlocked(fresh, "ship_vanguard"), Is.True);
            Assert.That(UnlockRules.IsFlagshipUnlocked(fresh, "ship_bastion"), Is.False);
            Assert.That(UnlockRules.IsFlagshipUnlocked(fresh, "ship_zephyr"), Is.False);

            var tutorialRun = GameSimulation.NewRun(fresh, GameSimulation.FirstExpeditionSeed).State;
            tutorialRun.phase = GamePhase.Victory;
            UnlockRules.RecordVictory(fresh, tutorialRun);
            Assert.That(fresh.tutorialSeen, Is.True);
            Assert.That(UnlockRules.IsFlagshipUnlocked(fresh, "ship_bastion"), Is.True);
            Assert.That(UnlockRules.IsFlagshipUnlocked(fresh, "ship_zephyr"), Is.False, "a one-region tutorial is not a campaign");

            var campaign = GameSimulation.NewRun(fresh, 75).State;
            campaign.regionIndex = campaign.regionCount; // victory only happens at the last gate
            campaign.phase = GamePhase.Victory;
            UnlockRules.RecordVictory(fresh, campaign);
            Assert.That(fresh.campaignVictories, Is.EqualTo(1));
            Assert.That(UnlockRules.IsFlagshipUnlocked(fresh, "ship_zephyr"), Is.True);
            Assert.That(UnlockRules.UnlockedFlagships(fresh), Is.EqualTo(new[] { "ship_vanguard", "ship_bastion", "ship_zephyr" }));
        }

        [Test]
        public void SaveService_RoundTripsFlagshipChoiceAndCampaignVictories()
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-ark-flagship-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var service = new SaveService(root);
                var profile = Profile();
                profile.flagshipId = "ship_bastion";
                profile.campaignVictories = 2;
                service.SaveProfile(profile);
                var loaded = service.LoadProfile();
                Assert.That(loaded.flagshipId, Is.EqualTo("ship_bastion"));
                Assert.That(loaded.campaignVictories, Is.EqualTo(2));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void WingLibrary_HasNineLocalizedValidWings()
        {
            var ids = ContentCatalog.WingIds();
            Assert.That(ids.Count, Is.GreaterThanOrEqualTo(9));
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            foreach (var id in ids)
            {
                var wing = ContentCatalog.GetWing(id);
                Assert.That(ko.T(wing.nameKey), Is.Not.EqualTo(wing.nameKey), id);
                Assert.That(en.T(wing.descriptionKey), Is.Not.EqualTo(wing.descriptionKey), id);
                Assert.That(wing.strength, Is.GreaterThan(0), id);
                Assert.That(wing.cost, Is.GreaterThan(0), id);
                Assert.That(wing.missionTime, Is.GreaterThan(0f), id);
                Assert.That(wing.lossResistance, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), id);
            }
        }

        [Test]
        public void Wings_DefaultPerFlagshipAndOldSavesAreBackfilled()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var vanguard = GameSimulation.NewRun(profile, 81).State;
            Assert.That(vanguard.squadrons.ConvertAll(s => s.wingId), Is.EqualTo(new[] { "kestrel_interceptors", "ember_bombers" }));
            Assert.That(vanguard.squadrons[0].displayKey, Is.EqualTo(ContentCatalog.GetWing("kestrel_interceptors").nameKey));

            profile.campaignVictories = 1;
            profile.flagshipId = "ship_zephyr";
            var zephyr = GameSimulation.NewRun(profile, 82).State;
            Assert.That(zephyr.squadrons.Count, Is.EqualTo(3), "the Zephyr carries three wings");
            Assert.That(zephyr.squadrons[2].wingId, Is.EqualTo("far_eyes"));
            Assert.That(zephyr.playerShip.wingBays, Is.EqualTo(3));

            foreach (var squadron in vanguard.squadrons) squadron.wingId = null; // pre-wing save
            GameSimulation.EnsureWings(vanguard);
            Assert.That(vanguard.squadrons[0].wingId, Is.EqualTo("kestrel_interceptors"));
            Assert.That(vanguard.squadrons[1].wingId, Is.EqualTo("ember_bombers"));
        }

        private static GameSimulation WingRun(string wingId, int bay = 0)
        {
            var simulation = RunAgainst("enemy_cutter");
            var state = simulation.State;
            IsolateFromOtherThreats(state);
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
            state.playerShip.GetSystem(ShipSystemType.FlightDeck).power = 3;
            var wing = ContentCatalog.GetWing(wingId);
            var squadron = state.squadrons[bay];
            squadron.wingId = wingId; squadron.displayKey = wing.nameKey; squadron.type = wing.type;
            squadron.strength = wing.strength; squadron.maxStrength = wing.strength; squadron.ordnanceCost = wing.ordnanceCost;
            state.resources.ordnance = 20;
            state.currentWeather = WeatherType.Clear;
            simulation.SetPaused(false);
            return simulation;
        }

        private static void FlyMission(GameSimulation simulation, int bay, SquadronMission mission, ShipSystemType target)
        {
            Assert.That(simulation.LaunchSquadron(simulation.State.squadrons[bay].id, mission, target).success, Is.True);
            for (var i = 0; i < 120 && simulation.State.squadrons[bay].status != SquadronStatus.Ready && simulation.State.squadrons[bay].status != SquadronStatus.Destroyed; i++)
                simulation.Tick(0.1f);
        }

        [Test]
        public void Wings_UseTheirOwnOrdnanceCostAndInterceptCharges()
        {
            var simulation = WingRun("gale_lancers");
            var state = simulation.State;
            var wing = ContentCatalog.GetWing("gale_lancers");
            var ordnanceBefore = state.resources.ordnance;
            state.interceptCharges = 0;
            FlyMission(simulation, 0, SquadronMission.Intercept, ShipSystemType.FlightDeck);
            Assert.That(ordnanceBefore - state.resources.ordnance, Is.EqualTo(wing.ordnanceCost));
            Assert.That(state.interceptCharges, Is.EqualTo(Math.Min(GameSimulation.MaxInterceptCharges, wing.interceptCharges)), "a wing's intercept sortie grants its own charges");
        }

        [Test]
        public void Wings_BombardDamageEscortWardReconAndAssaultFollowTheWing()
        {
            var bomber = WingRun("thunder_bombers", 1);
            var enemy = bomber.State.enemyShip;
            enemy.ward = 0f; enemy.armor = 0f; enemy.hull = 100f; enemy.maxHull = 100f;
            enemy.GetSystem(ShipSystemType.Ward).power = 0; // no ward regrowth during the sortie
            var wing = ContentCatalog.GetWing("thunder_bombers");
            FlyMission(bomber, 1, SquadronMission.Bombard, ShipSystemType.Weapons);
            var expected = (6f + bomber.State.squadrons[1].strength) * wing.bombardDamage;
            Assert.That(100f - enemy.hull, Is.EqualTo(expected).Within(0.05f).Or.GreaterThan(expected - 0.05f), "bombard damage scales by the wing multiplier (plus any weapon fire)");

            var escort = WingRun("sky_wardens");
            var ship = escort.State.playerShip;
            ship.GetSystem(ShipSystemType.Ward).power = 0; ship.ward = 0f;
            escort.State.interceptCharges = 0;
            FlyMission(escort, 0, SquadronMission.Escort, ShipSystemType.FlightDeck);
            Assert.That(ship.ward, Is.EqualTo(ContentCatalog.GetWing("sky_wardens").escortWard).Within(0.001f));
            Assert.That(escort.State.interceptCharges, Is.EqualTo(ContentCatalog.GetWing("sky_wardens").escortCharges));

            var recon = WingRun("far_eyes");
            FlyMission(recon, 0, SquadronMission.Recon, ShipSystemType.Sensors);
            Assert.That(recon.State.reconBonusSeconds, Is.GreaterThan(15f).And.LessThanOrEqualTo(ContentCatalog.GetWing("far_eyes").reconSeconds));

            var assault = WingRun("storm_marines");
            var target = assault.State.enemyShip.GetSystem(ShipSystemType.Ward);
            target.damage = 0f;
            FlyMission(assault, 0, SquadronMission.Assault, ShipSystemType.Ward);
            Assert.That(target.damage, Is.EqualTo(ContentCatalog.GetWing("storm_marines").assaultSabotage).Within(0.001f));
        }

        [Test]
        public void Wings_LossResistanceScalesTheLossRoll()
        {
            var lossesFragile = 0; var lossesTough = 0;
            for (var seed = 1; seed <= 60; seed++)
            {
                foreach (var pair in new[] { ("kestrel_interceptors", 0), ("ghost_kites", 1) })
                {
                    var simulation = WingRun(pair.Item1);
                    simulation.State.random.combat = SeededRandom.Seed(seed, 0xC0B47u);
                    simulation.State.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 2; // raise the base loss chance
                    var before = simulation.State.squadrons[0].strength;
                    FlyMission(simulation, 0, SquadronMission.Intercept, ShipSystemType.FlightDeck);
                    if (simulation.State.squadrons[0].strength < before) { if (pair.Item2 == 0) lossesFragile++; else lossesTough++; }
                }
            }
            Assert.That(lossesTough, Is.LessThan(lossesFragile), "loss resistance should reduce attrition over many sorties");
        }

        [Test]
        public void Port_SellsWingsIntoBaysReplacingTheSameSpecialtyWithARefund()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 607);
            var state = simulation.State;
            simulation.BeginCombat(2, true);
            WinCurrentBattle(simulation);
            Assert.That(state.phase, Is.EqualTo(GamePhase.Port));
            var offer = simulation.PortWingOffers();
            Assert.That(offer.Count, Is.EqualTo(1));
            Assert.That(offer, Is.EqualTo(simulation.PortWingOffers()), "offers are deterministic");

            state.resources.salvage = 200;
            var wing = ContentCatalog.GetWing(offer[0]);
            var sameSpecialty = state.squadrons.FindIndex(s => ContentCatalog.GetWing(s.wingId).type == wing.type);
            var replacedIndex = sameSpecialty >= 0 ? sameSpecialty : state.squadrons.Count - 1;
            var replaced = ContentCatalog.GetWing(state.squadrons[replacedIndex].wingId);
            var pilot = state.squadrons[replacedIndex].pilotCrewId;
            var salvageBefore = state.resources.salvage;
            Assert.That(simulation.PurchaseWing(offer[0]).success, Is.True);
            Assert.That(state.squadrons[replacedIndex].wingId, Is.EqualTo(offer[0]));
            Assert.That(state.squadrons[replacedIndex].pilotCrewId, Is.EqualTo(pilot), "the bay keeps its pilot");
            Assert.That(state.squadrons[replacedIndex].strength, Is.EqualTo(wing.strength));
            Assert.That(state.resources.salvage, Is.EqualTo(salvageBefore - wing.cost + replaced.cost / 2));
            Assert.That(simulation.PurchaseWing(offer[0]).success, Is.False, "already carried");
        }

        [Test]
        public void EnemyLibrary_HasTwelveSilhouettesAndThirtyValidConfigs()
        {
            var ids = new List<string>(ContentCatalog.EnemyIds());
            Assert.That(ids.Count, Is.GreaterThanOrEqualTo(30));
            var silhouettes = new System.Collections.Generic.HashSet<string>();
            var ko = new LocalizationService(Language.Korean);
            var en = new LocalizationService(Language.English);
            foreach (var id in ids)
            {
                var definition = ContentCatalog.GetEnemyDefinition(id);
                Assert.That(definition, Is.Not.Null, id);
                silhouettes.Add(definition.silhouette);
                Assert.That(definition.weight, Is.GreaterThan(0), id);
                Assert.That(definition.minRegion, Is.InRange(1, ContentCatalog.RegionCount), id);
                Assert.That(definition.tier, Is.InRange(1, 3), id); // tier 3 is reserved for the finale boss
                Assert.That(ko.T(definition.nameKey), Is.Not.EqualTo(definition.nameKey), id);
                Assert.That(en.T(definition.nameKey), Is.Not.EqualTo(definition.nameKey), id);
                foreach (var weapon in definition.weapons) Assert.That(ContentCatalog.GetWeapon(weapon), Is.Not.Null, id + " weapon " + weapon);
                var random = 5u;
                var ship = ContentCatalog.CreateEnemyById(id, ref random);
                Assert.That(ship.systems.Count, Is.EqualTo(10), id);
                Assert.That(ship.AllocatedPower(), Is.LessThanOrEqualTo(ship.coreOutput), id + " over-allocates power");
                Assert.That(ContentCatalog.DeckPlanFor(ship), Is.Not.Null, id + " has no deck plan through its silhouette");
                Assert.That(ship.nameKey, Is.EqualTo(definition.nameKey));
            }
            Assert.That(silhouettes.Count, Is.GreaterThanOrEqualTo(12));
        }

        [Test]
        public void EnemySelection_RespectsRegionGatesAndKeepsTheTutorialBaseline()
        {
            var lateOnly = new List<string>();
            foreach (var id in ContentCatalog.EnemyIds())
                if (ContentCatalog.GetEnemyDefinition(id).minRegion >= 3) lateOnly.Add(id);
            Assert.That(lateOnly.Count, Is.GreaterThan(0));

            var seenLateInRegionOne = false;
            var seenLateInRegionFour = false;
            for (var seed = 1u; seed < 400u; seed++)
            {
                var r1 = seed; var r4 = seed;
                var early = ContentCatalog.CreateEnemy(1, true, 1, ref r1);
                var late = ContentCatalog.CreateEnemy(1, true, 4, ref r4);
                if (lateOnly.Contains(early.id)) seenLateInRegionOne = true;
                if (lateOnly.Contains(late.id)) seenLateInRegionFour = true;
            }
            Assert.That(seenLateInRegionOne, Is.False, "region-gated configs must not appear in region 1");
            Assert.That(seenLateInRegionFour, Is.True, "region-gated configs should appear by region 4");

            var random = 9u;
            Assert.That(ContentCatalog.CreateEnemy(1, false, 4, ref random).id, Is.EqualTo("enemy_cutter"));
            Assert.That(ContentCatalog.CreateEnemy(2, false, 4, ref random).id, Is.EqualTo("enemy_cruiser"));
        }

        [Test]
        public void Route_RegionsFiveAndSixBiasWeatherTowardTheirThemes()
        {
            Func<RouteNodeState, bool> generated = node => node.column >= 1 && node.column <= 6;
            var abyss = Share(5, n => n.weather == WeatherType.Turbulence || n.weather == WeatherType.AetherCurrent, generated);
            var throne = Share(6, n => n.weather == WeatherType.Clear || n.weather == WeatherType.AetherCurrent, generated);
            Assert.That(abyss, Is.GreaterThan(0.45f));
            Assert.That(throne, Is.GreaterThan(0.45f));
            Assert.That(ContentCatalog.GetRegion(6).enemyStatMultiplier, Is.GreaterThan(ContentCatalog.GetRegion(5).enemyStatMultiplier));
        }

        [Test]
        public void Finale_TheLastGateOfACampaignIsGuardedByTheWarden()
        {
            var profile = Profile();
            profile.tutorialSeen = true;
            var simulation = GameSimulation.NewRun(profile, 909);
            simulation.State.regionIndex = simulation.State.regionCount;
            simulation.BeginCombat(2, true);
            Assert.That(simulation.State.enemyShip.id, Is.EqualTo("enemy_gate_warden"));
            Assert.That(ContentCatalog.DeckPlanFor(simulation.State.enemyShip), Is.Not.Null);
            Assert.That(simulation.State.enemyShip.maxHull, Is.GreaterThan(46f), "gate reinforcement still applies to the boss");

            var midway = GameSimulation.NewRun(profile, 910);
            midway.State.regionIndex = 2;
            midway.BeginCombat(2, true);
            Assert.That(midway.State.enemyShip.id, Is.Not.EqualTo("enemy_gate_warden"), "only the final gate is guarded");

            var tutorial = Profile(Difficulty.Story);
            tutorial.tutorialSeen = false;
            var first = GameSimulation.NewRun(tutorial, GameSimulation.FirstExpeditionSeed);
            first.BeginCombat(2, true);
            Assert.That(first.State.enemyShip.id, Is.EqualTo("enemy_cruiser"), "the tutorial gate stays the storm cruiser");
        }

        [Test]
        public void Finale_LastRegionRouteNamesTheThroneGate()
        {
            var last = ContentCatalog.CreateRoute(11, ContentCatalog.RegionCount);
            Assert.That(last.Find(node => node.encounterType == EncounterType.Gate).nameKey, Is.EqualTo("node.final_gate"));
            var earlier = ContentCatalog.CreateRoute(11, 2);
            Assert.That(earlier.Find(node => node.encounterType == EncounterType.Gate).nameKey, Is.EqualTo("node.gate"));
            var ko = new LocalizationService(Language.Korean);
            Assert.That(ko.T("node.final_gate"), Is.Not.EqualTo("node.final_gate"));
            Assert.That(ko.T("ship.enemy_gate_warden"), Is.Not.EqualTo("ship.enemy_gate_warden"));
        }

        [TestCase(WeatherType.Thunderhead, -0.08f)]
        [TestCase(WeatherType.Turbulence, -0.12f)]
        [TestCase(WeatherType.AetherCurrent, 0.04f)]
        public void WeatherProfiles_ExposeDistinctAccuracyRules(WeatherType type, float expected)
        {
            Assert.That(ContentCatalog.GetWeather(type).accuracyModifier, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Lineages_AllHaveLocalizedAuthoredRules()
        {
            var korean = new LocalizationService(Language.Korean);
            var english = new LocalizationService(Language.English);
            var seen = new HashSet<CrewLineage>();
            foreach (var rule in LineageRules.All())
            {
                seen.Add(rule.lineage);
                Assert.That(rule.maxHealth, Is.GreaterThan(0f));
                Assert.That(korean.T(rule.descriptionKey), Is.Not.EqualTo(rule.descriptionKey));
                Assert.That(english.T(rule.descriptionKey), Is.Not.EqualTo(rule.descriptionKey));
            }
            Assert.That(seen.Count, Is.EqualTo(6));
            Assert.That(LineageRules.Get(CrewLineage.Elf).overchargeInstabilityMultiplier, Is.LessThan(1f));
            Assert.That(LineageRules.Get(CrewLineage.Dwarf).repairMultiplier, Is.GreaterThan(1f));
            Assert.That(LineageRules.Get(CrewLineage.Orc).boardingMultiplier, Is.GreaterThan(1f));
            Assert.That(LineageRules.Get(CrewLineage.Goblin).sortieTimeMultiplier, Is.LessThan(1f));
            Assert.That(LineageRules.Get(CrewLineage.Avian).oxygenDamageMultiplier, Is.LessThan(1f));
        }

        [Test]
        public void CaptainLineage_DoctrinesChangeStartingStrategy()
        {
            var human = GameSimulation.NewRun(ProfileWithLineage(CrewLineage.Human), 8101).State;
            var elf = GameSimulation.NewRun(ProfileWithLineage(CrewLineage.Elf), 8101).State;
            var dwarf = GameSimulation.NewRun(ProfileWithLineage(CrewLineage.Dwarf), 8101).State;
            var orc = GameSimulation.NewRun(ProfileWithLineage(CrewLineage.Orc), 8101).State;
            var goblin = GameSimulation.NewRun(ProfileWithLineage(CrewLineage.Goblin), 8101).State;
            var avian = GameSimulation.NewRun(ProfileWithLineage(CrewLineage.Avian), 8101).State;

            Assert.That(human.convoy.morale, Is.EqualTo(80));
            Assert.That(elf.resources.aether, Is.EqualTo(18));
            Assert.That(dwarf.playerShip.maxArmor, Is.EqualTo(22f));
            Assert.That(orc.playerShip.maxHull, Is.EqualTo(36f));
            Assert.That(orc.convoy.survivors, Is.EqualTo(1225));
            Assert.That(goblin.resources.ordnance, Is.EqualTo(10));
            Assert.That(goblin.resources.salvage, Is.EqualTo(24));
            Assert.That(avian.resources.aether, Is.EqualTo(17));
            Assert.That(avian.resources.supplies, Is.EqualTo(14));
        }

        [Test]
        public void ElfResonator_ReducesOverchargeInstability()
        {
            var simulation = GameSimulation.NewRun(Profile(), 8102);
            simulation.BeginCombat(1, false);
            var resonator = simulation.State.crew.Find(crew => crew.role == CrewRole.Resonator);
            resonator.currentRoom = ShipSystemType.Weapons;

            var result = simulation.Overcharge(ShipSystemType.Weapons);

            Assert.That(result.success, Is.True);
            Assert.That(simulation.State.playerShip.instability, Is.EqualTo(14.3f).Within(0.01f));
        }

        [Test]
        public void GoblinPilot_CompletesSortiePhasesFaster()
        {
            var goblin = GameSimulation.NewRun(Profile(), 8103);
            var human = GameSimulation.NewRun(Profile(), 8103);
            goblin.BeginCombat(1, false);
            human.BeginCombat(1, false);
            var goblinWing = goblin.State.squadrons[0];
            var humanWing = human.State.squadrons[0];
            human.State.crew.Find(crew => crew.id == humanWing.pilotCrewId).lineage = CrewLineage.Human;

            Assert.That(goblin.LaunchSquadron(goblinWing.id, SquadronMission.Intercept, ShipSystemType.FlightDeck).success, Is.True);
            Assert.That(human.LaunchSquadron(humanWing.id, SquadronMission.Intercept, ShipSystemType.FlightDeck).success, Is.True);
            Assert.That(goblinWing.missionTimer, Is.LessThan(humanWing.missionTimer));
            Assert.That(goblinWing.missionTimer / humanWing.missionTimer, Is.EqualTo(0.82f).Within(0.01f));
        }

        private static ProfileState ProfileWithLineage(CrewLineage lineage)
        {
            var profile = Profile();
            profile.captainLineage = lineage;
            return profile;
        }
    }
}
