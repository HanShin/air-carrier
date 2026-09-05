using System;
using System.IO;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;
using AudioSettings = AetherArk.Core.AudioSettings;

namespace AetherArk.Tests
{
    public sealed class AudioTests
    {
        [TestCase(GamePhase.RouteMap, false, MusicMood.Voyage)]
        [TestCase(GamePhase.Port, false, MusicMood.Port)]
        [TestCase(GamePhase.Encounter, false, MusicMood.Encounter)]
        [TestCase(GamePhase.Combat, false, MusicMood.Combat)]
        [TestCase(GamePhase.Combat, true, MusicMood.Finale)]
        [TestCase(GamePhase.Victory, true, MusicMood.Silence)]
        [TestCase(GamePhase.Defeat, false, MusicMood.Silence)]
        public void PhaseSelectsExpectedScore(GamePhase phase, bool finale, MusicMood expected)
        {
            Assert.That(AudioCatalog.MoodFor(new RunState { phase = phase, isFinalBattle = finale }), Is.EqualTo(expected));
            Assert.That(AudioCatalog.MoodFor(null), Is.EqualTo(MusicMood.Voyage));
        }

        [Test]
        public void EveryCueAndMoodLoadsWithTheCorrectImportPolicy()
        {
            foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
            {
                if (mood == MusicMood.Silence) continue;
                var clip = Resources.Load<AudioClip>(AudioCatalog.MusicPath(mood));
                Assert.That(clip, Is.Not.Null, mood.ToString());
                Assert.That(clip.channels, Is.EqualTo(2));
                Assert.That(clip.length, Is.InRange(14f, 30f));
                Assert.That(clip.loadType, Is.EqualTo(AudioClipLoadType.CompressedInMemory));
            }
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {
                var clip = Resources.Load<AudioClip>(AudioCatalog.EffectPath(cue));
                Assert.That(clip, Is.Not.Null, cue.ToString());
                Assert.That(clip.length, Is.InRange(0.2f, 3.5f));
                Assert.That(clip.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True);
                var peak = 0f;
                foreach (var sample in samples) peak = Math.Max(peak, Math.Abs(sample));
                Assert.That(peak, Is.InRange(0.3f, 0.8f), cue + " must be audible without clipping");
            }
        }

        [Test]
        public void AudioSettingsClampCorruptValuesAndKeepExplicitSilence()
        {
            var settings = new AudioSettings { musicVolume = float.NaN, effectsVolume = float.PositiveInfinity };
            settings.Normalize();
            Assert.That(settings.musicVolume, Is.EqualTo(0.5f));
            Assert.That(settings.effectsVolume, Is.EqualTo(0.7f));
            settings.musicVolume = -4f;
            settings.effectsVolume = 7f;
            settings.Normalize();
            Assert.That(settings.musicVolume, Is.Zero);
            Assert.That(settings.effectsVolume, Is.EqualTo(1f));
        }

        [TestCase("v1", 0.5f, 0.7f, false)]
        [TestCase("v2", 0.3f, 0.8f, true)]
        public void ProfileFixturesMigrateAndRoundTripWithoutChangingRunData(string version, float music, float effects, bool muted)
        {
            var root = Path.Combine(Path.GetTempPath(), "aether-audio-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var fixture = Path.Combine(Application.dataPath, "_Project/Tests/EditMode/Fixtures", version);
                File.Copy(Path.Combine(fixture, "profile.json"), Path.Combine(root, "profile.json"));
                File.Copy(Path.Combine(fixture, "suspended_run.json"), Path.Combine(root, "suspended_run.json"));
                var service = new SaveService(root);
                var profile = service.LoadProfile();
                Assert.That(profile.schemaVersion, Is.EqualTo(2));
                Assert.That(profile.captainName, Is.EqualTo("Fixture Captain"));
                Assert.That(profile.accessibility.highContrast, Is.True);
                Assert.That(profile.audio.musicVolume, Is.EqualTo(music).Within(0.001f));
                Assert.That(profile.audio.effectsVolume, Is.EqualTo(effects).Within(0.001f));
                Assert.That(profile.audio.muted, Is.EqualTo(muted));
                var runBefore = JsonUtility.ToJson(service.LoadRun());
                profile.audio.musicVolume = 0f;
                profile.audio.effectsVolume = 0.2f;
                profile.audio.muted = true;
                service.SaveProfile(profile);
                var loaded = service.LoadProfile();
                Assert.That(loaded.audio.musicVolume, Is.Zero);
                Assert.That(loaded.audio.effectsVolume, Is.EqualTo(0.2f).Within(0.001f));
                Assert.That(loaded.audio.muted, Is.True);
                Assert.That(JsonUtility.ToJson(service.LoadRun()), Is.EqualTo(runBefore));
            }
            finally { Directory.Delete(root, true); }
        }

        [Test]
        public void PresentationNotificationsDoNotChangeDeterministicStateOrReplayHistory()
        {
            var profile = new ProfileState();
            var observed = GameSimulation.NewRun(profile, 713);
            var silent = GameSimulation.NewRun(profile, 713);
            var count = 0;
            observed.LogAdded += entry => count++;
            observed.CombatAlertRaised += severity => count++;
            foreach (var simulation in new[] { observed, silent })
            {
                simulation.BeginCombat(1, false);
                simulation.FireMainWeapon(ShipSystemType.Weapons);
                simulation.LaunchSquadron(simulation.State.squadrons[0].id, SquadronMission.Bombard, ShipSystemType.Weapons);
                simulation.State.autoPauseOnWarning = false;
                simulation.SetPaused(false);
                for (var i = 0; i < 80; i++) simulation.Tick(0.1f);
            }
            Assert.That(count, Is.GreaterThan(2));
            Assert.That(JsonUtility.ToJson(observed.State), Is.EqualTo(JsonUtility.ToJson(silent.State)));
            var restored = new GameSimulation(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(observed.State)));
            var restoredCount = 0;
            restored.LogAdded += entry => restoredCount++;
            Assert.That(restoredCount, Is.Zero);
        }
    }
}
