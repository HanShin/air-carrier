using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace AetherArk.Tests
{
    public sealed class ReproductionStoreTests
    {
        private string root;
        private ReproductionStore store;
        private ProfileState profile;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "aether-repro-test-" + Guid.NewGuid().ToString("N"));
            store = new ReproductionStore(root);
            profile = ReproductionStore.SeedProfile(new ProfileState(), "ship_zephyr", Difficulty.Standard);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }

        [TestCase("0", 0)]
        [TestCase("-1", -1)]
        [TestCase("2147483647", int.MaxValue)]
        [TestCase("-2147483648", int.MinValue)]
        [TestCase(" 32838 ", 32838)]
        public void SeedParserPreservesEverySupportedInteger(string value, int expected)
        {
            Assert.That(ReproductionStore.TrySeed(value, out var seed), Is.True);
            Assert.That(seed, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("1.5")]
        [TestCase("2147483648")]
        [TestCase("-2147483649")]
        [TestCase("1e3")]
        [TestCase("1_000")]
        public void InvalidSeedIsNotSilentlyReplaced(string value)
        {
            Assert.That(ReproductionStore.TrySeed(value, out _), Is.False);
        }

        [Test]
        public void SeededPresetClonesProfileAndNeverMakes32838ATutorial()
        {
            var source = new ProfileState { captainName = "Normal captain", captainLineage = CrewLineage.Elf };
            var before = JsonUtility.ToJson(source);
            var testProfile = ReproductionStore.SeedProfile(source, "ship_zephyr", Difficulty.Harsh);
            var first = GameSimulation.NewRun(testProfile, 32838);
            var second = GameSimulation.NewRun(ReproductionStore.SeedProfile(source, "ship_zephyr", Difficulty.Harsh), 32838);
            Assert.That(first.State.regionCount, Is.EqualTo(6));
            Assert.That(first.State.isFirstExpedition, Is.False);
            Assert.That(first.State.playerShip.id, Is.EqualTo("ship_zephyr"));
            Assert.That(JsonUtility.ToJson(first.State), Is.EqualTo(JsonUtility.ToJson(second.State)));
            testProfile.audio.musicVolume = 0f;
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
        }

        [Test]
        public void CaptureRoundTripPreservesLiveBattleWithoutAliasingOrMutation()
        {
            var simulation = Battle();
            simulation.LaunchSquadron(simulation.State.squadrons[2].id, SquadronMission.Recon, ShipSystemType.Weapons);
            DebugScenarios.ApplyDamageShowcase(simulation.State);
            simulation.SetPaused(false);
            var before = JsonUtility.ToJson(simulation.State); var beforeProfile = JsonUtility.ToJson(profile);
            var path = store.Capture(simulation.State, profile);
            var loaded = store.Load(path, out var differentBuild);
            Assert.That(differentBuild, Is.False);
            Assert.That(JsonUtility.ToJson(loaded.run), Is.EqualTo(before));
            Assert.That(JsonUtility.ToJson(simulation.State), Is.EqualTo(before));
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(beforeProfile));
            loaded.run.resources.ordnance += 7;
            Assert.That(JsonUtility.ToJson(simulation.State), Is.EqualTo(before));
        }

        [Test]
        public void SameCaptureCommandsAndFixedStepsProduceIdenticalStates()
        {
            var path = store.Capture(Battle().State, profile);
            var a = new GameSimulation(store.Load(path, out _).run);
            var b = new GameSimulation(store.Load(path, out _).run);
            foreach (var simulation in new[] { a, b })
            {
                simulation.LaunchSquadron(simulation.State.squadrons[2].id, SquadronMission.Recon, ShipSystemType.Engines);
                simulation.ChangeAltitude(AltitudeBand.High);
            }
            for (var step = 0; step < 150; step++)
            {
                if (step % 10 == 0) { a.Execute(new FireWeaponCommand(ShipSystemType.Engines)); b.Execute(new FireWeaponCommand(ShipSystemType.Engines)); }
                Assert.That(ReproductionStore.Step(a), Is.EqualTo(ReproductionStore.Step(b)));
                Assert.That(JsonUtility.ToJson(a.State), Is.EqualTo(JsonUtility.ToJson(b.State)), "Step " + step);
            }
            Assert.That(a.State.combatElapsed, Is.GreaterThan(0f));
        }

        [Test]
        public void CaptureNamesAreUniqueAndPreviousFilesRemainByteIdentical()
        {
            var state = Battle().State;
            var first = store.Capture(state, profile); var bytes = File.ReadAllBytes(first);
            state.resources.ordnance++;
            var second = store.Capture(state, profile);
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(File.ReadAllBytes(first), Is.EqualTo(bytes));
            Assert.That(store.LatestSnapshot(), Is.EqualTo(second));
            Assert.That(Directory.GetFiles(store.SnapshotDirectory, "*.tmp"), Is.Empty);
        }

        [TestCase("format", "repro.invalid_snapshot")]
        [TestCase("checksum", "repro.invalid_snapshot")]
        [TestCase("version", "repro.unsupported_version")]
        [TestCase("run_version", "repro.unsupported_version")]
        [TestCase("profile_version", "repro.unsupported_version")]
        [TestCase("missing_ship", "repro.invalid_snapshot")]
        [TestCase("missing_system", "repro.invalid_snapshot")]
        [TestCase("missing_rng", "repro.invalid_snapshot")]
        [TestCase("duplicate_crew", "repro.invalid_snapshot")]
        [TestCase("empty_log_entry", "repro.invalid_snapshot")]
        [TestCase("mission", "repro.invalid_snapshot")]
        [TestCase("target", "repro.invalid_snapshot")]
        [TestCase("phase", "repro.combat_only")]
        public void DamagedOrUnsupportedSnapshotFailsExplicitly(string fault, string expected)
        {
            var path = store.Capture(Battle().State, profile);
            var envelope = JsonUtility.FromJson<CombatSnapshot>(File.ReadAllText(path));
            var payload = JsonUtility.FromJson<CombatSnapshotPayload>(envelope.payloadJson);
            switch (fault)
            {
                case "format": envelope.format = "wrong"; break;
                case "version": envelope.formatVersion = 99; break;
                case "run_version": payload.run.schemaVersion = 99; break;
                case "profile_version": payload.profile.schemaVersion = 99; break;
                case "missing_ship": payload.run.enemyShip = null; break;
                case "missing_system": payload.run.enemyShip.systems.RemoveAt(0); break;
                case "missing_rng": payload.run.random = null; break;
                case "duplicate_crew": payload.run.crew[1].id = payload.run.crew[0].id; break;
                case "empty_log_entry": payload.run.combatLog.Add(new CombatLogEntry(null)); break;
                case "mission": payload.run.squadrons[0].mission = (SquadronMission)999; break;
                case "target": payload.run.selectedEnemySystem = (ShipSystemType)999; break;
                case "phase": payload.run.phase = GamePhase.Defeat; break;
            }
            envelope.payloadJson = JsonUtility.ToJson(payload);
            using (var hash = SHA256.Create()) envelope.sha256 = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(envelope.payloadJson))).Replace("-", "").ToLowerInvariant();
            if (fault == "checksum") envelope.sha256 = "bad";
            File.WriteAllText(path, JsonUtility.ToJson(envelope));
            Assert.That(Assert.Throws<InvalidDataException>(() => store.Load(path, out _)).Message, Is.EqualTo(expected));
        }

        [TestCase("{")]
        [TestCase("null")]
        [TestCase("{}")]
        public void MalformedJsonIsRejected(string contents)
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "broken.json"); File.WriteAllText(path, contents);
            Assert.That(Assert.Throws<InvalidDataException>(() => store.Load(path, out _)).Message, Is.EqualTo("repro.invalid_snapshot"));
        }

        [Test]
        public void OversizedAndMissingFilesFailBeforeParsing()
        {
            Assert.That(Assert.Throws<InvalidDataException>(() => store.Load(Path.Combine(root, "absent.json"), out _)).Message, Is.EqualTo("repro.snapshot_missing"));
            Directory.CreateDirectory(root); var path = Path.Combine(root, "large.json");
            using (var stream = File.Create(path)) stream.SetLength(ReproductionStore.MaxSnapshotBytes + 1);
            Assert.That(Assert.Throws<InvalidDataException>(() => store.Load(path, out _)).Message, Is.EqualTo("repro.invalid_snapshot"));
        }

        [Test]
        public void DifferentBuildWarnsButDoesNotChangePayload()
        {
            var state = Battle().State; var before = JsonUtility.ToJson(state);
            var path = store.Capture(state, profile);
            var envelope = JsonUtility.FromJson<CombatSnapshot>(File.ReadAllText(path));
            envelope.simulationBuild = "older-build";
            File.WriteAllText(path, JsonUtility.ToJson(envelope));
            var payload = store.Load(path, out var warning);
            Assert.That(warning, Is.True);
            Assert.That(JsonUtility.ToJson(payload.run), Is.EqualTo(before));
        }

        [Test]
        public void FileFailureDoesNotMutateTheBattle()
        {
            Directory.CreateDirectory(root); var blocked = Path.Combine(root, "not-a-directory"); File.WriteAllText(blocked, "sentinel");
            var state = Battle().State; var before = JsonUtility.ToJson(state);
            Assert.Throws<IOException>(() => new ReproductionStore(blocked).Capture(state, profile));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(File.ReadAllText(blocked), Is.EqualTo("sentinel"));
        }

        [Test]
        public void InvalidLiveStateIsRejectedBeforeWritingAnything()
        {
            var state = Battle().State;
            state.combatLog = null;
            Assert.Throws<InvalidDataException>(() => store.Capture(state, profile));
            state = Battle().State; state.playerShip.hull = float.NaN;
            Assert.Throws<InvalidDataException>(() => store.Capture(state, profile));
            Assert.That(Directory.Exists(store.SnapshotDirectory), Is.False);
        }

        [Test]
        public void StepRequiresPausedCombatAndUsesExactlyOneTenthSecond()
        {
            var simulation = Battle(); var before = simulation.State.combatElapsed;
            Assert.That(ReproductionStore.Step(simulation), Is.True);
            Assert.That(simulation.State.combatElapsed - before, Is.EqualTo(0.1f).Within(0.00001f));
            Assert.That(simulation.State.isPaused, Is.True);
            simulation.SetPaused(false);
            Assert.That(ReproductionStore.Step(simulation), Is.False);
            simulation.State.phase = GamePhase.Victory;
            Assert.That(ReproductionStore.Step(simulation), Is.False);
            Assert.That(ReproductionStore.Step(null), Is.False);
        }

        private GameSimulation Battle()
        {
            var simulation = GameSimulation.NewRun(profile, 17000); simulation.BeginCombat(1, false); return simulation;
        }
    }
}
