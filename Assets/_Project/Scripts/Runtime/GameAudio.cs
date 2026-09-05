using System;
using System.Collections.Generic;
using AetherArk.Core;
using UnityEngine;
using AudioSettings = AetherArk.Core.AudioSettings;

namespace AetherArk.Runtime
{
    /// <summary>Presentation-only, bounded voice pool. Survives UI redraws, never reads gameplay RNG.</summary>
    public sealed class GameAudio : IDisposable
    {
        public const float CrossfadeSeconds = 1.6f;
        public MusicMood Mood { get; private set; } = MusicMood.Silence;
        public int MusicTransitionCount { get; private set; }
        public int PlayedCueCount { get; private set; }
        public SoundCue? LastCue { get; private set; }
        public float MusicLevel => music[activeMusic].volume;

        private readonly GameObject root;
        private readonly AudioSource[] music = new AudioSource[2];
        private readonly AudioSource[] effects = new AudioSource[5];
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly float[] cooldowns = new float[Enum.GetValues(typeof(SoundCue)).Length];
        private readonly float[] musicWeights = new float[2];
        private readonly float[] fadeStart = new float[2];
        private GameSimulation boundSimulation;
        private AudioSettings settings;
        private int activeMusic;
        private int nextVoice;
        private float fadeTime = CrossfadeSeconds;
        private float duck = 1f;
        private float warningCooldown;
        private AlertSeverity lastWarning;

        public GameAudio(Transform parent, AudioSettings audioSettings)
        {
            root = new GameObject("GameAudio");
            root.transform.SetParent(parent, false);
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null) root.AddComponent<AudioListener>();
            for (var i = 0; i < music.Length; i++) music[i] = CreateSource("Music" + i, true, 128);
            for (var i = 0; i < effects.Length; i++) effects[i] = CreateSource("Effect" + i, false, i == 4 ? 32 : 96);
            ApplySettings(audioSettings);
        }

        private AudioSource CreateSource(string name, bool loop, int priority)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = priority;
            source.volume = 0f;
            return source;
        }

        public void Observe(GameSimulation simulation)
        {
            if (ReferenceEquals(simulation, boundSimulation)) return;
            if (boundSimulation != null)
            {
                boundSimulation.LogAdded -= OnLog;
                boundSimulation.CombatAlertRaised -= OnAlert;
            }
            boundSimulation = simulation;
            if (simulation != null)
            {
                simulation.LogAdded += OnLog;
                simulation.CombatAlertRaised += OnAlert;
            }
            // Loading a save must not replay its old log entries, shots or warnings.
            foreach (var source in effects) source.Stop();
            Array.Clear(cooldowns, 0, cooldowns.Length);
            warningCooldown = 0f;
        }

        public void ApplySettings(AudioSettings value)
        {
            settings = value ?? new AudioSettings();
            settings.Normalize();
            ApplyVolumes();
        }

        public void Tick(float unscaledSeconds, RunState state, bool settingsOpen)
        {
            var dt = Math.Max(0f, unscaledSeconds);
            for (var i = 0; i < cooldowns.Length; i++) cooldowns[i] = Math.Max(0f, cooldowns[i] - dt);
            warningCooldown = Math.Max(0f, warningCooldown - dt);
            SetMood(AudioCatalog.MoodFor(state));
            fadeTime = Math.Min(CrossfadeSeconds, fadeTime + dt);
            var fraction = fadeTime / CrossfadeSeconds;
            for (var i = 0; i < music.Length; i++)
            {
                var target = i == activeMusic && Mood != MusicMood.Silence ? 1f : 0f;
                musicWeights[i] = fadeStart[i] + (target - fadeStart[i]) * fraction;
                if (fraction >= 1f && target == 0f) music[i].Stop();
            }
            var targetDuck = settingsOpen || (state != null && state.phase == GamePhase.Combat && state.isPaused) ? 0.4f : 1f;
            duck += (targetDuck - duck) * Math.Min(1f, dt * 5f);
            ApplyVolumes();
        }

        private void SetMood(MusicMood mood)
        {
            if (mood == Mood) return;
            Mood = mood;
            MusicTransitionCount++;
            fadeTime = 0f;
            Array.Copy(musicWeights, fadeStart, 2);
            if (mood == MusicMood.Silence) return;
            var clip = Load(AudioCatalog.MusicPath(mood));
            for (var i = 0; i < music.Length; i++)
            {
                if (music[i].clip != clip || musicWeights[i] <= 0f) continue;
                activeMusic = i; // Reverse an in-flight crossfade without restarting an already playing loop.
                return;
            }
            activeMusic = musicWeights[0] <= musicWeights[1] ? 0 : 1;
            // A third mood reuses the quieter voice, with the new clip fading in from silence.
            music[activeMusic].Stop();
            fadeStart[activeMusic] = musicWeights[activeMusic] = 0f;
            music[activeMusic].clip = clip;
            if (music[activeMusic].clip != null) music[activeMusic].Play();
        }

        public void Play(SoundCue cue)
        {
            if (settings.muted || settings.effectsVolume <= 0f || cooldowns[(int)cue] > 0f) return;
            var clip = Load(AudioCatalog.EffectPath(cue));
            if (clip == null) return;
            var important = cue == SoundCue.Warning || cue == SoundCue.Critical || cue == SoundCue.Victory || cue == SoundCue.Defeat;
            var source = effects[important ? 4 : nextVoice++ % 4];
            source.Stop();
            source.clip = clip;
            source.Play();
            cooldowns[(int)cue] = important ? 1.2f : 0.09f;
            LastCue = cue;
            PlayedCueCount++;
        }

        private void OnLog(CombatLogEntry entry)
        {
            var cue = AudioCatalog.CueForLog(entry.key);
            if (cue.HasValue) Play(cue.Value);
        }

        private void OnAlert(AlertSeverity severity)
        {
            if (severity == AlertSeverity.Info || (warningCooldown > 0f && severity <= lastWarning)) return;
            Play(severity == AlertSeverity.Critical ? SoundCue.Critical : SoundCue.Warning);
            warningCooldown = 2f;
            lastWarning = severity;
        }

        private AudioClip Load(string path)
        {
            if (!clips.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>(path);
                clips[path] = clip;
                if (clip == null) Debug.LogWarning("Missing optional audio: " + path);
            }
            return clip;
        }

        private void ApplyVolumes()
        {
            for (var i = 0; i < music.Length; i++)
            {
                music[i].mute = settings.muted;
                music[i].volume = settings.musicVolume * 0.65f * musicWeights[i] * duck;
            }
            for (var i = 0; i < effects.Length; i++)
            {
                effects[i].mute = settings.muted;
                effects[i].volume = settings.effectsVolume * (i == 4 ? 0.5f : 0.32f);
            }
        }

        public void Dispose()
        {
            Observe(null);
            UnityEngine.Object.Destroy(root);
        }
    }
}
