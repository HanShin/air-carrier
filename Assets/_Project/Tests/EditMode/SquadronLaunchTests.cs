using System;
using System.IO;
using AetherArk.Content;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace AetherArk.Tests
{
    public sealed class SquadronLaunchTests
    {
        [TestCase("missing", "command.pilot_missing")]
        [TestCase("empty_id", "command.pilot_missing")]
        [TestCase("dead", "command.pilot_dead")]
        [TestCase("downed", "command.pilot_downed")]
        [TestCase("negative_health", "command.pilot_downed")]
        [TestCase("flag_busy", "command.pilot_busy")]
        [TestCase("launching", "command.pilot_busy")]
        [TestCase("on_mission", "command.pilot_busy")]
        [TestCase("recovering", "command.pilot_busy")]
        public void UnavailablePilotsRejectWithoutAnyMutation(string scenario, string key)
        {
            var simulation = Battle();
            SetPilotScenario(simulation.State, scenario);
            AssertRejectedUnchanged(simulation, SquadronMission.Bombard, ShipSystemType.Weapons, key);
        }

        [TestCase(SquadronMission.Intercept)]
        [TestCase(SquadronMission.Bombard)]
        [TestCase(SquadronMission.Escort)]
        [TestCase(SquadronMission.Recon)]
        [TestCase(SquadronMission.Assault)]
        public void InjuredButActivePilotCanLaunchEveryExistingMission(SquadronMission mission)
        {
            var simulation = Battle();
            var state = simulation.State;
            var squadron = state.squadrons[0];
            var pilot = state.crew.Find(c => c.id == squadron.pilotCrewId);
            pilot.health = 1f;
            var before = JsonUtility.ToJson(state);
            for (var i = 0; i < 12; i++) Assert.That(simulation.CheckSquadronLaunch(squadron.id, mission, ShipSystemType.Weapons).success, Is.True);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before), "UI queries must be read-only");
            var ammo = state.resources.ordnance;
            var random = state.random.combat;
            Assert.That(simulation.Execute(new LaunchSquadronCommand(squadron.id, mission, ShipSystemType.Weapons)).success, Is.True);
            Assert.That(state.resources.ordnance, Is.EqualTo(ammo - GameSimulation.SquadronLaunchCost(squadron)));
            Assert.That(pilot.onSortie, Is.True);
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Launching));
            Assert.That(state.random.combat, Is.EqualTo(random));
        }

        [TestCase(SquadronMission.None)]
        [TestCase(SquadronMission.Recall)]
        [TestCase((SquadronMission)999)]
        public void InvalidMissionDoesNotSpendAnything(SquadronMission mission)
        {
            AssertRejectedUnchanged(Battle(), mission, ShipSystemType.Weapons, "command.invalid_mission");
        }

        [TestCase(SquadronMission.Bombard)]
        [TestCase(SquadronMission.Assault)]
        public void InvalidStrikeTargetIsRejectedBeforeDispatch(SquadronMission mission)
        {
            AssertRejectedUnchanged(Battle(), mission, (ShipSystemType)999, "command.invalid_system");
        }

        [Test]
        public void SharedPilotIsExclusiveUntilTheFirstSquadronFinishesRecovery()
        {
            var simulation = Battle(); var state = simulation.State;
            var first = state.squadrons[0]; var second = state.squadrons[1];
            second.pilotCrewId = first.pilotCrewId;
            var pilot = state.crew.Find(c => c.id == first.pilotCrewId);
            Assert.That(simulation.LaunchSquadron(first.id, SquadronMission.Intercept, ShipSystemType.Weapons).success, Is.True);
            Assert.That(simulation.LaunchSquadron(second.id, SquadronMission.Bombard, ShipSystemType.Weapons).messageKey, Is.EqualTo("command.pilot_busy"));
            pilot.onSortie = false; // Legacy/inconsistent save: the active wing still owns the pilot.
            var ammo = state.resources.ordnance;
            Assert.That(simulation.LaunchSquadron(second.id, SquadronMission.Bombard, ShipSystemType.Weapons).messageKey, Is.EqualTo("command.pilot_busy"));
            Assert.That(state.resources.ordnance, Is.EqualTo(ammo));
            pilot.onSortie = true;
            state.autoPauseOnWarning = false;
            state.weatherHazardTimer = state.enemySquadronCooldown = 1000f;
            foreach (var slot in state.enemyShip.weaponSlots) slot.cooldown = 1000f;
            simulation.SetPaused(false);
            for (var i = 0; i < 3; i++) { first.missionTimer = 0f; simulation.Tick(0.1f); }
            Assert.That(first.status, Is.EqualTo(SquadronStatus.Ready));
            Assert.That(pilot.onSortie, Is.False);
            Assert.That(simulation.LaunchSquadron(second.id, SquadronMission.Bombard, ShipSystemType.Weapons).success, Is.True);
        }

        [Test]
        public void HospitalRescueRestoresEligibilityButDoesNotResurrectDeadPilots()
        {
            var simulation = Battle(); var state = simulation.State;
            state.convoy.supportShip = SupportShipType.Hospital;
            var downed = state.crew.Find(c => c.id == state.squadrons[0].pilotCrewId);
            var dead = state.crew.Find(c => c.id == state.squadrons[1].pilotCrewId);
            downed.health = dead.health = 0f; dead.isDead = true;
            Assert.That(simulation.CheckSquadronLaunch(state.squadrons[0].id, SquadronMission.Intercept, ShipSystemType.Weapons).messageKey, Is.EqualTo("command.pilot_downed"));
            Assert.That(simulation.UseSupportAbility().success, Is.True);
            Assert.That(simulation.CheckSquadronLaunch(state.squadrons[0].id, SquadronMission.Intercept, ShipSystemType.Weapons).success, Is.True);
            Assert.That(simulation.CheckSquadronLaunch(state.squadrons[1].id, SquadronMission.Bombard, ShipSystemType.Weapons).messageKey, Is.EqualTo("command.pilot_dead"));
        }

        [Test]
        public void ReplacingAircraftDoesNotMakeADeadPilotAvailable()
        {
            var simulation = Battle(); var state = simulation.State; var squadron = state.squadrons[0];
            SetPilotScenario(state, "dead");
            squadron.strength = 0; squadron.status = SquadronStatus.Destroyed;
            state.phase = GamePhase.RouteMap;
            Assert.That(simulation.RefitSquadrons().success, Is.True);
            Assert.That(squadron.CanLaunch, Is.True, "The airframe is ready; the pilot is not");
            simulation.BeginCombat(1, false);
            AssertRejectedUnchanged(simulation, SquadronMission.Intercept, ShipSystemType.Weapons, "command.pilot_dead");
        }

        [TestCase("dead", "command.pilot_dead")]
        [TestCase("downed", "command.pilot_downed")]
        [TestCase("missing", "command.pilot_missing")]
        [TestCase("on_mission", "command.pilot_busy")]
        public void SaveResumePreservesPilotRestrictions(string scenario, string key)
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-pilot-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var simulation = Battle(); SetPilotScenario(simulation.State, scenario);
                var service = new SaveService(root);
                service.SaveRun(simulation.State);
                var restored = service.LoadRun();
                Assert.That(restored.schemaVersion, Is.EqualTo(CrewMovementRules.RunVersion));
                AssertRejectedUnchanged(new GameSimulation(restored), SquadronMission.Bombard, ShipSystemType.Weapons, key);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void AuthoritativeCostAndDeckChecksRemainInTheSharedQuery()
        {
            var simulation = Battle(); var state = simulation.State; var wing = state.squadrons[0];
            wing.wingId = "thunder_bombers"; wing.ordnanceCost = 0;
            state.resources.ordnance = 2;
            Assert.That(GameSimulation.SquadronLaunchCost(wing), Is.EqualTo(3));
            AssertRejectedUnchanged(simulation, SquadronMission.Bombard, ShipSystemType.Weapons, "command.no_ordnance");
            state.resources.ordnance = 8;
            state.playerShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
            AssertRejectedUnchanged(simulation, SquadronMission.Bombard, ShipSystemType.Weapons, "command.deck_unpowered");
        }

        private static GameSimulation Battle()
        {
            var simulation = GameSimulation.NewRun(new ProfileState { tutorialSeen = true }, 6107);
            simulation.BeginCombat(1, false);
            return simulation;
        }

        private static void SetPilotScenario(RunState state, string scenario)
        {
            var squadron = state.squadrons[0]; var pilot = state.crew.Find(c => c.id == squadron.pilotCrewId);
            switch (scenario)
            {
                case "missing": state.crew.Remove(pilot); break;
                case "empty_id": squadron.pilotCrewId = ""; break;
                case "dead": pilot.isDead = true; break; // A stale positive HP field must not override death.
                case "downed": pilot.health = 0f; break;
                case "negative_health": pilot.health = -1f; break;
                case "flag_busy": pilot.onSortie = true; break;
                default:
                    var other = state.squadrons[1]; other.pilotCrewId = pilot.id;
                    other.status = scenario == "launching" ? SquadronStatus.Launching : scenario == "on_mission" ? SquadronStatus.OnMission : SquadronStatus.Recovering;
                    other.mission = SquadronMission.Recon;
                    pilot.onSortie = false;
                    break;
            }
        }

        private static void AssertRejectedUnchanged(GameSimulation simulation, SquadronMission mission, ShipSystemType target, string key)
        {
            var before = JsonUtility.ToJson(simulation.State); var events = 0;
            simulation.LogAdded += entry => events++;
            var id = simulation.State.squadrons[0].id;
            var readiness = simulation.CheckSquadronLaunch(id, mission, target);
            var result = simulation.Execute(new LaunchSquadronCommand(id, mission, target));
            Assert.That(readiness.success, Is.False);
            Assert.That(result.success, Is.False);
            Assert.That(readiness.messageKey, Is.EqualTo(key));
            Assert.That(result.messageKey, Is.EqualTo(key));
            Assert.That(JsonUtility.ToJson(simulation.State), Is.EqualTo(before), "Rejected launches must not mutate resources, crew, timers, logs or RNG");
            Assert.That(events, Is.Zero);
            foreach (Language language in Enum.GetValues(typeof(Language))) Assert.That(new LocalizationService(language).T(key), Is.Not.EqualTo(key));
        }
    }
}
