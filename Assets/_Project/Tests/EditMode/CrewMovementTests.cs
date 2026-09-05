using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AetherArk.Content;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace AetherArk.Tests
{
    public sealed class CrewMovementTests
    {
        private static ProfileState Profile(string ship = "ship_zephyr") => ReproductionStore.SeedProfile(new ProfileState(), ship, Difficulty.Standard);
        private static GameSimulation Battle(string ship = "ship_zephyr")
        {
            var simulation = GameSimulation.NewRun(Profile(ship), 17000);
            simulation.BeginCombat(1, false);
            simulation.State.autoPauseOnWarning = false;
            foreach (var system in simulation.State.enemyShip.systems) system.power = 0;
            simulation.State.weatherHazardTimer = 10000;
            return simulation;
        }
        private static CrewState Engineer(GameSimulation sim) => sim.State.crew.Find(c => c.role == CrewRole.Engineer);
        private static void Steps(GameSimulation sim, int count) { sim.SetPaused(false); for (var i = 0; i < count; i++) sim.Tick(0.1f); sim.SetPaused(true); }
        private static ShipSystemType OtherRoom(CrewState c) => c.currentRoom == ShipSystemType.Bridge ? ShipSystemType.Engines : ShipSystemType.Bridge;

        [TestCase("ship_vanguard")]
        [TestCase("ship_zephyr")]
        [TestCase("ship_bastion")]
        public void EveryRoomPairUsesSharedDoorwaysAndRemainsInsideDeck(string id)
        {
            // Actual catalog identifier is checked before using each flagship.
            var ship = ContentCatalog.GetFlagship(id);
            Assert.That(ship, Is.Not.Null);
            var sim = Battle(id); var plan = ContentCatalog.DeckPlanFor(sim.State.playerShip);
            foreach (var from in plan.tiles) foreach (var to in plan.tiles)
            {
                var c = new CrewState { id = "route", health = 100, maxHealth = 100, currentRoom = from.system };
                var station = CrewMovementRules.Station(plan, from.system, 0);
                c.movement = new CrewMovementState { x = station.x, y = station.y, destination = to.system };
                c.movement.path = CrewMovementRules.Path(plan, c, to.system, 0);
                Assert.That(c.movement.path.Count, Is.GreaterThan(0));
                for (var tick = 0; c.IsMoving && tick < 300; tick++)
                {
                    Assert.That(CrewMovementRules.IsValid(c, plan, true), Is.True, from.system + " -> " + to.system);
                    CrewMovementRules.Tick(c, 0.1f);
                }
                Assert.That(c.IsMoving, Is.False); Assert.That(c.currentRoom, Is.EqualTo(to.system));
                Assert.That(CrewMovementRules.IsValid(c, plan, true), Is.True);
            }
        }

        [Test]
        public void CornerTouchAndSeparatedRoomsDoNotCreateDoors()
        {
            var a = new DeckTile(); var b = new DeckTile { column = 1, row = 1 };
            Assert.That(CrewMovementRules.Door(a, b, out _, out _), Is.False);
            var plan = new DeckPlan { tiles = new List<DeckTile> { a, b } };
            a.system = ShipSystemType.Bridge; b.system = ShipSystemType.Engines;
            Assert.That(CrewMovementRules.Path(plan, new CrewState { currentRoom = a.system }, b.system, 0), Is.Empty);
        }

        [Test]
        public void PausedMoveQueuesWithoutTeleportingRepairOrConsumingRng()
        {
            var sim = Battle(); var c = Engineer(sim); var room = c.currentRoom; var x = c.movement.x;
            var random = JsonUtility.ToJson(sim.State.random);
            Assert.That(sim.MoveCrew(c.id, OtherRoom(c)).success, Is.True);
            var queued = JsonUtility.ToJson(sim.State);
            sim.Tick(0.1f);
            Assert.That(JsonUtility.ToJson(sim.State), Is.EqualTo(queued));
            Assert.That(c.currentRoom, Is.EqualTo(room)); Assert.That(c.movement.x, Is.EqualTo(x));
            Assert.That(JsonUtility.ToJson(sim.State.random), Is.EqualTo(random));
        }

        [Test]
        public void RepeatedOrderIsIdempotentAndRerouteStartsAtExactCurrentPosition()
        {
            var sim = Battle(); var c = Engineer(sim); var target = OtherRoom(c);
            sim.MoveCrew(c.id, target); Steps(sim, 5);
            var before = JsonUtility.ToJson(c);
            sim.MoveCrew(c.id, target);
            Assert.That(JsonUtility.ToJson(c), Is.EqualTo(before));
            var x = c.movement.x; var y = c.movement.y;
            sim.MoveCrew(c.id, ShipSystemType.Infirmary);
            Assert.That(c.movement.x, Is.EqualTo(x)); Assert.That(c.movement.y, Is.EqualTo(y));
            Assert.That(c.movement.destination, Is.EqualTo(ShipSystemType.Infirmary));
        }

        [Test]
        public void WalkersDoNotRepairUntilArrivalAndTakeHazardDamage()
        {
            var sim = Battle(); var c = Engineer(sim);
            foreach (var other in sim.State.crew) if (other != c) other.onSortie = true;
            var source = sim.State.playerShip.GetSystem(c.currentRoom); source.damage = 30;
            sim.State.playerShip.GetRoom(c.currentRoom).fire = 20;
            var target = sim.State.playerShip.GetSystem(OtherRoom(c)); target.damage = 30;
            sim.MoveCrew(c.id, target.type); var hp = c.health;
            Steps(sim, 1);
            Assert.That(source.damage, Is.EqualTo(30)); Assert.That(target.damage, Is.EqualTo(30));
            Assert.That(c.health, Is.LessThan(hp)); Assert.That(c.IsAtStation, Is.False);
            Steps(sim, 140);
            Assert.That(c.IsAtStation, Is.True); Assert.That(target.damage, Is.LessThan(30));
        }

        [Test]
        public void DownedAndSortieCrewCannotWalkAndDowningStopsAtCurrentPoint()
        {
            var sim = Battle(); var c = Engineer(sim);
            sim.MoveCrew(c.id, OtherRoom(c)); Steps(sim, 3); var x = c.movement.x;
            c.health = 0; Steps(sim, 1);
            Assert.That(c.IsMoving, Is.False); Assert.That(c.movement.x, Is.EqualTo(x));
            Assert.That(sim.MoveCrew(c.id, OtherRoom(c)).success, Is.False);
            c.health = 10; c.onSortie = true;
            Assert.That(sim.MoveCrew(c.id, OtherRoom(c)).success, Is.False);
        }

        [Test]
        public void MovingPilotCannotLaunchUntilArrival()
        {
            var sim = Battle(); var squadron = sim.State.squadrons[0]; var pilot = sim.State.crew.Find(c => c.id == squadron.pilotCrewId);
            sim.MoveCrew(pilot.id, OtherRoom(pilot)); var before = JsonUtility.ToJson(sim.State);
            Assert.That(sim.LaunchSquadron(squadron.id, SquadronMission.Intercept, ShipSystemType.Weapons).messageKey, Is.EqualTo("command.pilot_moving"));
            Assert.That(JsonUtility.ToJson(sim.State), Is.EqualTo(before));
            Steps(sim, 150);
            Assert.That(sim.LaunchSquadron(squadron.id, SquadronMission.Intercept, ShipSystemType.Weapons).success, Is.True);
        }

        [Test]
        public void InjuryAndLineageChangeSpeedAndMovementAccelerates()
        {
            var c = new CrewState { health = 100, maxHealth = 100, lineage = CrewLineage.Human };
            var healthy = CrewMovementRules.WalkingSpeed(c); c.health = 20;
            Assert.That(CrewMovementRules.WalkingSpeed(c), Is.LessThan(healthy));
            c.health = 100; c.lineage = CrewLineage.Dwarf; var slow = CrewMovementRules.WalkingSpeed(c);
            c.lineage = CrewLineage.Goblin; Assert.That(CrewMovementRules.WalkingSpeed(c), Is.GreaterThan(slow));
            var sim = Battle(); c = Engineer(sim); sim.MoveCrew(c.id, OtherRoom(c)); Steps(sim, 1);
            Assert.That(c.movement.speed, Is.GreaterThan(0).And.LessThan(CrewMovementRules.WalkingSpeed(c)));
        }

        [Test]
        public void EightStationSlotsAreDistinctAndInsideOneRoom()
        {
            var plan = ContentCatalog.GetDeckPlan("ship_zephyr"); var positions = new HashSet<string>();
            for (var i = 0; i < 8; i++)
            {
                var p = CrewMovementRules.Station(plan, ShipSystemType.Bridge, i);
                Assert.That(positions.Add(p.x + ":" + p.y), Is.True);
            }
        }

        [Test]
        public void SaveAndSnapshotResumeInFlightAtExactlyTheSameStep()
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-crew-" + Guid.NewGuid().ToString("N"));
            try
            {
                var sim = Battle(); var c = Engineer(sim); sim.MoveCrew(c.id, OtherRoom(c)); Steps(sim, 6);
                Assert.That(c.IsMoving, Is.True);
                var saves = new SaveService(root); saves.SaveRun(sim.State);
                var restored = new GameSimulation(saves.LoadRun());
                var store = new ReproductionStore(root); var capture = store.Capture(sim.State, Profile());
                var snapshot = new GameSimulation(store.Load(capture, out _).run);
                for (var i = 0; i < 100; i++)
                {
                    Steps(sim, 1); Steps(restored, 1); Steps(snapshot, 1);
                    Assert.That(JsonUtility.ToJson(restored.State), Is.EqualTo(JsonUtility.ToJson(sim.State)));
                    Assert.That(JsonUtility.ToJson(snapshot.State), Is.EqualTo(JsonUtility.ToJson(sim.State)));
                }
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [TestCase("v1")]
        [TestCase("v2")]
        public void HistoricalRunFixturesMigrateToStationaryCrewWithoutChangingSource(string version)
        {
            var source = Path.Combine(Application.dataPath, "_Project/Tests/EditMode/Fixtures", version, "suspended_run.json");
            var before = File.ReadAllText(source); var root = Path.Combine(Path.GetTempPath(), "aether-crew-old-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root); File.Copy(source, Path.Combine(root, "suspended_run.json"));
                var run = new SaveService(root).LoadRun();
                Assert.That(run.schemaVersion, Is.EqualTo(2));
                foreach (var c in run.crew)
                {
                    Assert.That(c.movement, Is.Not.Null); Assert.That(c.IsMoving, Is.False);
                    Assert.That(CrewMovementRules.IsValid(c, ContentCatalog.DeckPlanFor(run.playerShip), true), Is.True);
                }
                Assert.That(File.ReadAllText(source), Is.EqualTo(before));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void SnapshotRejectsRouteThroughWalls()
        {
            var sim = Battle(); var c = Engineer(sim);
            c.movement.destination = OtherRoom(c);
            c.movement.path.Add(new CrewWaypoint(-20, 4, c.movement.destination));
            var store = new ReproductionStore(Path.GetTempPath());
            Assert.Throws<InvalidDataException>(() => store.Capture(sim.State, Profile()));
        }

        [Test]
        public void CommittedV3FixturePreservesMidWalkPositionAndResumes()
        {
            var root = Path.Combine(Application.dataPath, "_Project/Tests/EditMode/Fixtures/v3");
            var run = new SaveService(root).LoadRun(); // Load is read-only; never save into a fixture directory.
            var c = Engineer(new GameSimulation(run)); var before = JsonUtility.ToJson(c.movement);
            Assert.That(c.IsMoving, Is.True); Assert.That(c.movement.distanceWalked, Is.GreaterThan(0));
            var store = new ReproductionStore(root);
            var capture = store.Load(store.LatestSnapshot(), out _);
            Assert.That(JsonUtility.ToJson(capture.run.crew.Find(x => x.id == c.id).movement), Is.EqualTo(before));
            var sim = new GameSimulation(run); Steps(sim, 1);
            Assert.That(JsonUtility.ToJson(c.movement), Is.Not.EqualTo(before));
        }

        [Test]
        public void LegacySnapshotMigratesWithoutRewritingItAndWarnsAboutChangedRules()
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-crew-snapshot-old-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                var legacyState = Battle().State; legacyState.schemaVersion = 1;
                var legacyRun = Regex.Replace(JsonUtility.ToJson(legacyState), ",\"movement\":\\{[^}]*\\}", "");
                Assert.That(legacyRun, Does.Not.Contain("\"movement\""));
                var payload = "{\"run\":" + legacyRun + ",\"profile\":" + JsonUtility.ToJson(Profile()) + "}";
                var envelope = new CombatSnapshot { format = ReproductionStore.Format, formatVersion = 1,
                    payloadJson = payload, simulationBuild = ReproductionStore.SimulationBuild, unityVersion = Application.unityVersion };
                using (var sha = SHA256.Create()) envelope.sha256 = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
                var json = JsonUtility.ToJson(envelope); var path = Path.Combine(root, "legacy.json"); File.WriteAllText(path, json);
                var restored = new ReproductionStore(root).Load(path, out var warning);
                Assert.That(warning, Is.True); Assert.That(restored.run.schemaVersion, Is.EqualTo(2));
                Assert.That(restored.run.crew.TrueForAll(c => c.movement != null && !c.IsMoving), Is.True);
                Assert.That(File.ReadAllText(path), Is.EqualTo(json));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void ResonatorCannotOverchargeWhileWalkingEvenBeforeLeavingTheRoom()
        {
            var sim = Battle(); var c = sim.State.crew.Find(x => x.role == CrewRole.Resonator);
            sim.MoveCrew(c.id, ShipSystemType.Weapons); Steps(sim, 150);
            var source = c.currentRoom; sim.MoveCrew(c.id, OtherRoom(c));
            Assert.That(sim.Overcharge(source).messageKey, Is.EqualTo("command.need_resonator"));
        }

        [Test]
        public void VictoryFinishesWalkOnlyAfterCombatAndActivityReflectsActualTask()
        {
            var sim = Battle(); var c = Engineer(sim); sim.MoveCrew(c.id, OtherRoom(c));
            Assert.That(CrewMovementRules.Activity(c, sim.State.playerShip), Is.EqualTo(CrewActivity.Walking));
            sim.State.enemyShip.hull = 0; Steps(sim, 1);
            Assert.That(sim.State.phase, Is.Not.EqualTo(GamePhase.Combat)); Assert.That(c.IsMoving, Is.False);
            var room = sim.State.playerShip.GetRoom(c.currentRoom); room.fire = 10;
            Assert.That(CrewMovementRules.Activity(c, sim.State.playerShip), Is.EqualTo(CrewActivity.Extinguishing));
            room.fire = 0; room.breach = 10;
            Assert.That(CrewMovementRules.Activity(c, sim.State.playerShip), Is.EqualTo(CrewActivity.Sealing));
        }
    }
}
