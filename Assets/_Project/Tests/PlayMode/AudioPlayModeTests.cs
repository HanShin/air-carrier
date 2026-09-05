using System.Collections;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using AudioSettings = AetherArk.Core.AudioSettings;

namespace AetherArk.Tests
{
    public sealed class AudioPlayModeTests
    {
        [UnityTest]
        public IEnumerator MusicCrossfadesDucksAndDoesNotRestartOnRefreshOrPause()
        {
            var parent = new GameObject("AudioTest");
            var settings = new AudioSettings();
            var audio = new GameAudio(parent.transform, settings);
            try
            {
                var simulation = GameSimulation.NewRun(new ProfileState(), 41);
                audio.Observe(simulation);
                audio.Tick(2f, simulation.State, false);
                Assert.That(audio.Mood, Is.EqualTo(MusicMood.Voyage));
                var transitions = audio.MusicTransitionCount;
                for (var i = 0; i < 20; i++) audio.Tick(0.2f, simulation.State, false);
                Assert.That(audio.MusicTransitionCount, Is.EqualTo(transitions));
                simulation.BeginCombat(1, false);
                simulation.SetPaused(false);
                audio.Tick(2f, simulation.State, false);
                var runningVolume = audio.MusicLevel;
                Assert.That(runningVolume, Is.GreaterThan(0f));
                simulation.SetPaused(true);
                audio.Tick(2f, simulation.State, false);
                Assert.That(audio.MusicLevel, Is.LessThan(runningVolume));
                Assert.That(audio.MusicTransitionCount, Is.EqualTo(transitions + 1));
                settings.muted = true;
                audio.ApplySettings(settings);
                foreach (var source in parent.GetComponentsInChildren<AudioSource>()) Assert.That(source.mute, Is.True);
                var cues = audio.PlayedCueCount;
                audio.Play(SoundCue.Cannon);
                Assert.That(audio.PlayedCueCount, Is.EqualTo(cues));
                Assert.That(parent.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(7), "Voice count must remain bounded");
            }
            finally { audio.Dispose(); Object.Destroy(parent); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator LiveCommandsPlayOnceAndOldSavesDoNotReplayCues()
        {
            var parent = new GameObject("AudioTest");
            var audio = new GameAudio(parent.transform, new AudioSettings());
            try
            {
                var simulation = GameSimulation.NewRun(new ProfileState(), 42);
                simulation.BeginCombat(1, false);
                audio.Observe(simulation);
                Assert.That(audio.PlayedCueCount, Is.Zero);
                Assert.That(simulation.FireMainWeapon(ShipSystemType.Weapons).success, Is.True);
                Assert.That(audio.LastCue, Is.EqualTo(SoundCue.Cannon));
                var count = audio.PlayedCueCount;
                simulation.FireMainWeapon(ShipSystemType.Weapons); // Still reloading: no fire event.
                audio.Observe(simulation);
                Assert.That(audio.PlayedCueCount, Is.EqualTo(count));
                simulation.LaunchSquadron(simulation.State.squadrons[0].id, SquadronMission.Bombard, ShipSystemType.Weapons);
                Assert.That(audio.LastCue, Is.EqualTo(SoundCue.Launch));
                count = audio.PlayedCueCount;
                audio.Observe(new GameSimulation(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(simulation.State))));
                simulation.LaunchSquadron(simulation.State.squadrons[1].id, SquadronMission.Bombard, ShipSystemType.Weapons);
                Assert.That(audio.PlayedCueCount, Is.EqualTo(count), "Old simulation must be unsubscribed and saved logs not replayed");
            }
            finally { audio.Dispose(); Object.Destroy(parent); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator WarningGateLimitsRepetitionButLetsCriticalEscalationThrough()
        {
            var parent = new GameObject("AudioTest");
            var audio = new GameAudio(parent.transform, new AudioSettings());
            try
            {
                var simulation = GameSimulation.NewRun(new ProfileState(), 43);
                simulation.BeginCombat(1, false);
                audio.Observe(simulation);
                var ship = simulation.State.playerShip;
                ship.ward = ship.armor = 0f;
                ship.hull = ship.maxHull = 100f;
                simulation.ApplyDamage(ship, ShipSystemType.Weapons, 5f, true);
                Assert.That(audio.LastCue, Is.EqualTo(SoundCue.Warning));
                var count = audio.PlayedCueCount;
                simulation.ApplyDamage(ship, ShipSystemType.Weapons, 5f, true);
                Assert.That(audio.PlayedCueCount, Is.EqualTo(count));
                ship.hull = 25f;
                simulation.ApplyDamage(ship, ShipSystemType.Weapons, 5f, true);
                Assert.That(audio.LastCue, Is.EqualTo(SoundCue.Critical));
                Assert.That(audio.PlayedCueCount, Is.EqualTo(count + 1));
                Assert.That(simulation.State.isPaused, Is.True);
            }
            finally { audio.Dispose(); Object.Destroy(parent); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioPanelPreservesPauseAndVolumeButtonsPersist()
        {
            yield return null;
            var controller = Object.FindFirstObjectByType<GameController>();
            var original = JsonUtility.ToJson(controller.Profile.audio);
            try
            {
                controller.StartRun();
                controller.Simulation.BeginCombat(1, false);
                controller.Simulation.SetPaused(false);
                controller.ToggleAudioSettings();
                yield return null;
                Assert.That(controller.Simulation.State.isPaused, Is.True);
                Assert.That(GameObject.Find("AudioPanel"), Is.Not.Null);
                var elapsed = controller.Simulation.State.combatElapsed;
                yield return null;
                Assert.That(controller.Simulation.State.combatElapsed, Is.EqualTo(elapsed));
                controller.AdjustMusicVolume(-1f);
                yield return null;
                Assert.That(GameObject.Find("MusicDown").GetComponent<Button>().interactable, Is.False);
                GameObject.Find("MusicUp").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(new SaveService().LoadProfile().audio.musicVolume, Is.EqualTo(0.1f).Within(0.001f));
                GameObject.Find("AudioMute").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(new SaveService().LoadProfile().audio.muted, Is.EqualTo(controller.Profile.audio.muted));
                GameObject.Find("AudioClose").GetComponent<Button>().onClick.Invoke();
                Assert.That(controller.Simulation.State.isPaused, Is.False);
                controller.Simulation.SetPaused(true);
                controller.ToggleAudioSettings();
                controller.ToggleAudioSettings();
                Assert.That(controller.Simulation.State.isPaused, Is.True, "Already paused combat must remain paused");
                controller.AbandonRun();
            }
            finally
            {
                controller.Profile.audio = JsonUtility.FromJson<AudioSettings>(original);
                controller.Audio.ApplySettings(controller.Profile.audio);
                new SaveService().SaveProfile(controller.Profile);
                controller.ShowMenu();
            }
            yield return null;
        }
    }
}
