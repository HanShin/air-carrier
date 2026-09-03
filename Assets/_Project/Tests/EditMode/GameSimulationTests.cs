using System;
using System.IO;
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
        [TestCase("enemy_cutter")]
        [TestCase("enemy_cruiser")]
        [TestCase("enemy_carrier")]
        [TestCase("enemy_scout")]
        [TestCase("enemy_boarder")]
        [TestCase("enemy_monitor")]
        public void DeckPlan_CoversEverySystemWithoutOverlapInsideGrid(string shipId)
        {
            var random = 7u;
            var ship = shipId == "ship_vanguard" ? ContentCatalog.CreateVanguard() : ContentCatalog.CreateEnemyById(shipId, ref random);
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
            for (var i = 0; i < 20; i++) simulation.Tick(0.1f); // 2 s: inside the recharge delay
            Assert.That(ship.ward, Is.EqualTo(afterHit).Within(0.001f), "ward must not regenerate during the recharge delay");

            for (var i = 0; i < 40; i++) simulation.Tick(0.1f); // 6 s total: delay has expired
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
        public void EventLibrary_HasAtLeastTenEventsPerType()
        {
            foreach (var type in EventTypes)
                Assert.That(ContentCatalog.EncounterIds(type).Count, Is.GreaterThanOrEqualTo(10), type.ToString());
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

        [TestCase(WeatherType.Thunderhead, -0.08f)]
        [TestCase(WeatherType.Turbulence, -0.12f)]
        [TestCase(WeatherType.AetherCurrent, 0.04f)]
        public void WeatherProfiles_ExposeDistinctAccuracyRules(WeatherType type, float expected)
        {
            Assert.That(ContentCatalog.GetWeather(type).accuracyModifier, Is.EqualTo(expected).Within(0.001f));
        }
    }
}
