using System;
using System.IO;
using System.Text;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace AetherArk.Tests
{
    public sealed class AuditCheckpointTests
    {
        private string root;
        private ReproductionStore store;
        private static string Fixture(string name) => Path.Combine(Application.dataPath, "_Project/Tests/EditMode/Fixtures/audit_v1", name + ".json");

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "aether-audit-import-" + Guid.NewGuid().ToString("N"));
            store = new ReproductionStore(root);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }

        [TestCase("zephyr-17000-b21-t100", 21, 100)]
        [TestCase("zephyr-17000-b1-t1", 1, 1)]
        public void MonoExportLoadsWithProvenanceAndRoundTripsThroughUnity(string name, int battle, int ticks)
        {
            var path = Fixture(name); var before = File.ReadAllBytes(path);
            var payload = store.Load(path, out var differentBuild);
            Assert.That(differentBuild, Is.True, "Mono export must never claim to be the same Unity build");
            Assert.That(payload.run.schemaVersion, Is.EqualTo(2));
            Assert.That(payload.run.seed, Is.EqualTo(17000));
            Assert.That(payload.run.playerShip.id, Is.EqualTo("ship_zephyr"));
            Assert.That(payload.run.phase, Is.EqualTo(GamePhase.Combat));
            Assert.That(payload.run.combatElapsed, Is.EqualTo(ticks * .1f).Within(.00001f));
            Assert.That(payload.audit.producer, Is.EqualTo("headless-audit-v1"));
            Assert.That(payload.audit.battleOrdinal, Is.EqualTo(battle));
            Assert.That(payload.audit.completedTicks, Is.EqualTo(ticks));
            Assert.That(payload.audit.wingPolicy, Is.EqualTo("adaptive"));
            var roundTrip = store.Load(store.Capture(payload.run, payload.profile), out var unityBuildChanged);
            Assert.That(unityBuildChanged, Is.False);
            Assert.That(JsonUtility.ToJson(roundTrip.run), Is.EqualTo(JsonUtility.ToJson(payload.run)));
            Assert.That(JsonUtility.ToJson(roundTrip.profile), Is.EqualTo(JsonUtility.ToJson(payload.profile)));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));

            // Optional CI artifact: compare every field with the original Mono JSON using the Python verifier.
            // Never writes the fixture or normal save; caller supplies a fresh output directory.
            var artifactDirectory = Environment.GetEnvironmentVariable("AETHER_AUDIT_ROUNDTRIP_DIR");
            if (!string.IsNullOrEmpty(artifactDirectory))
            {
                Directory.CreateDirectory(artifactDirectory);
                using (var stream = new FileStream(Path.Combine(artifactDirectory, name + ".json"), FileMode.CreateNew))
                {
                    var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        [Test]
        public void LateBattleRetainsUnsignedRngDamageCrewPathAndPurchasedLoadout()
        {
            var state = store.Load(Fixture("zephyr-17000-b21-t100"), out _).run;
            Assert.That(state.regionIndex, Is.EqualTo(6));
            Assert.That(state.totalTravelCount, Is.EqualTo(41));
            Assert.That(state.random.route, Is.EqualTo(94922169u));
            Assert.That(state.random.combat, Is.EqualTo(2838187044u));
            Assert.That(state.random.events, Is.EqualTo(1977682864u));
            Assert.That(state.resources.ordnance, Is.EqualTo(1));
            Assert.That(state.playerShip.hull, Is.EqualTo(43.69719f));
            Assert.That(state.enemyShip.id, Is.EqualTo("enemy_cruiser_veteran"));
            Assert.That(state.crew.Count, Is.EqualTo(8));
            Assert.That(state.installedModules, Does.Contain("aether_capacitor"));
            var engineer = state.crew.Find(crew => crew.id == "crew_engineer");
            Assert.That(engineer.IsMoving, Is.True);
            Assert.That(engineer.movement.x, Is.EqualTo(1.82551825f));
            Assert.That(engineer.movement.y, Is.EqualTo(1.42069018f));
            Assert.That(engineer.movement.speed, Is.EqualTo(.5f));
            Assert.That(engineer.movement.path.Count, Is.EqualTo(3));
            Assert.That(engineer.movement.destination, Is.EqualTo(ShipSystemType.Ward));
        }

        [TestCase("zephyr-17000-b21-t100")]
        [TestCase("zephyr-17000-b1-t1")]
        public void ImportedCopiesCanResumeIdenticallyWithoutChangingSnapshot(string name)
        {
            var path = Fixture(name); var before = File.ReadAllBytes(path);
            var a = new GameSimulation(store.Load(path, out _).run);
            var b = new GameSimulation(store.Load(path, out _).run);
            a.SetPaused(true); b.SetPaused(true);
            for (var tick = 0; tick < 100; tick++)
            {
                if (tick % 10 == 0) { a.FireAllReady(ShipSystemType.Weapons); b.FireAllReady(ShipSystemType.Weapons); }
                Assert.That(ReproductionStore.Step(a), Is.EqualTo(ReproductionStore.Step(b)));
                Assert.That(JsonUtility.ToJson(a.State), Is.EqualTo(JsonUtility.ToJson(b.State)));
            }
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
        }

        [TestCase("producer")]
        [TestCase("ticks")]
        [TestCase("battle")]
        [TestCase("delta")]
        [TestCase("boundary")]
        [TestCase("policy")]
        [TestCase("source")]
        public void InvalidCheckpointMetadataIsRejectedEvenWithRecomputedChecksum(string fault)
        {
            var envelope = JsonUtility.FromJson<CombatSnapshot>(File.ReadAllText(Fixture("zephyr-17000-b21-t100")));
            var payload = JsonUtility.FromJson<CombatSnapshotPayload>(envelope.payloadJson);
            switch (fault)
            {
                case "producer": payload.audit.producer = "unknown"; break;
                case "ticks": payload.audit.completedTicks = -1; break;
                case "battle": payload.audit.battleOrdinal = 0; break;
                case "delta": payload.audit.fixedDelta = .2f; break;
                case "boundary": payload.audit.boundary = "after-orders"; break;
                case "policy": payload.audit.wingPolicy = "typo"; break;
                case "source": payload.audit.sourceSha256 = new string('z', 64); break;
            }
            envelope.payloadJson = JsonUtility.ToJson(payload);
            envelope.sha256 = CombatSnapshotFile.Digest(envelope.payloadJson);
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "invalid.json");
            File.WriteAllText(path, JsonUtility.ToJson(envelope));
            Assert.That(Assert.Throws<InvalidDataException>(() => store.Load(path, out _)).Message, Is.EqualTo("repro.invalid_snapshot"));
        }
    }
}
