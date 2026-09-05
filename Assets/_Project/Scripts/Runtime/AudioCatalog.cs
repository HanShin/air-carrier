using AetherArk.Core;

namespace AetherArk.Runtime
{
    public enum MusicMood { Silence, Voyage, Port, Encounter, Combat, Finale }
    public enum SoundCue { Confirm, Reject, Cannon, Impact, Flyby, Launch, Recover, Resonance, Warning, Critical, Pause, Resume, Victory, Defeat }

    public static class AudioCatalog
    {
        public static MusicMood MoodFor(RunState state)
        {
            if (state == null) return MusicMood.Voyage;
            switch (state.phase)
            {
                case GamePhase.Combat: return state.isFinalBattle ? MusicMood.Finale : MusicMood.Combat;
                case GamePhase.Port: return MusicMood.Port;
                case GamePhase.Encounter: return MusicMood.Encounter;
                case GamePhase.Victory:
                case GamePhase.Defeat: return MusicMood.Silence;
                default: return MusicMood.Voyage;
            }
        }

        public static string MusicPath(MusicMood mood) => mood == MusicMood.Silence ? null : "Audio/Music/" + mood.ToString().ToLowerInvariant();
        public static string EffectPath(SoundCue cue) => "Audio/Effects/" + cue.ToString().ToLowerInvariant();

        public static SoundCue? CueForLog(string key)
        {
            switch (key)
            {
                case "log.player_hit":
                case "log.player_miss": return SoundCue.Cannon;
                case "log.enemy_hit":
                case "log.enemy_squadron_hit":
                case "log.bombardment": return SoundCue.Impact;
                case "log.enemy_miss":
                case "log.enemy_squadron_intercepted": return SoundCue.Flyby;
                case "log.squadron_launch": return SoundCue.Launch;
                case "log.squadron_recovered": return SoundCue.Recover;
                case "log.overcharge": return SoundCue.Resonance;
                case "log.module_installed":
                case "log.weapon_mounted":
                case "log.wing_embarked":
                case "log.crew_recruited": return SoundCue.Confirm;
                case "log.combat_victory":
                case "log.gate_opened":
                case "log.region_cleared": return SoundCue.Victory;
                case "log.defeat": return SoundCue.Defeat;
                default: return null;
            }
        }
    }
}
