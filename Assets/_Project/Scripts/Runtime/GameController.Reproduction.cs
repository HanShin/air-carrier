using System;
using System.IO;
using AetherArk.Content;
using AetherArk.Core;
using UnityEngine;

namespace AetherArk.Runtime
{
    public sealed partial class GameController
    {
        public bool ReproductionPanelOpen { get; private set; }
        public bool IsReproduction => normalSaves != null;
        public string ReproductionSeed = "17000";
        public string ReproductionFlagship = "ship_zephyr";
        public Difficulty ReproductionDifficulty = Difficulty.Standard;
        public string ReproductionSnapshotPath = string.Empty;
        public string ReproductionMessageKey { get; private set; } = "repro.help";
        public string ReproductionDetails { get; private set; } = string.Empty;
        public bool CanCaptureSnapshot => Debug.isDebugBuild && Simulation?.State.phase == GamePhase.Combat;
        public bool CanStepReproduction => IsReproduction && Simulation?.State.phase == GamePhase.Combat && Simulation.State.isPaused;

        private ReproductionStore reproductionStore;
        private SaveService normalSaves;
        private ProfileState normalProfile;
        private GameSimulation normalSimulation;
        private FrontendScreen normalScreen;
        private Language normalLanguage;
        private string normalCommandMessage;
        private ReproductionStore ReproductionFiles => reproductionStore ??
            (reproductionStore = new ReproductionStore(Path.Combine(saves.RootPath, "reproduction")));

        public void ToggleReproductionPanel()
        {
            if (!Debug.isDebugBuild || AudioSettingsOpen) return;
            ReproductionPanelOpen = !ReproductionPanelOpen;
            // Freeze by withholding Update, not by altering the snapshot's pause/alert fields.
            RefreshCurrentScreen();
        }

        public void CycleReproductionFlagship()
        {
            if (!Debug.isDebugBuild) return;
            var ids = ContentCatalog.FlagshipIds();
            ReproductionFlagship = ids[(ids.IndexOf(ReproductionFlagship) + 1) % ids.Count];
            view.ShowReproductionPanel();
        }

        public void CycleReproductionDifficulty()
        {
            if (!Debug.isDebugBuild) return;
            ReproductionDifficulty = Next(ReproductionDifficulty);
            view.ShowReproductionPanel();
        }

        public void StartSeededReproduction(bool battle)
        {
            if (!Debug.isDebugBuild) return;
            if (!ReproductionStore.TrySeed(ReproductionSeed, out var seed)) { ReproductionError("repro.invalid_seed"); return; }
            try
            {
                var profile = ReproductionStore.SeedProfile(Profile, ReproductionFlagship, ReproductionDifficulty);
                var simulation = GameSimulation.NewRun(profile, seed);
                if (battle) simulation.BeginCombat(1, false); // Natural first-tier encounter; never searches for another seed.
                ActivateReproduction(profile, simulation);
                ReproductionMessageKey = "repro.started";
                ReproductionDetails = "Seed " + seed;
                ReproductionPanelOpen = false;
                RefreshCurrentScreen();
            }
            catch (Exception exception) when (IsReproductionFileError(exception)) { ReproductionFailure(exception); }
        }

        public void CaptureReproductionSnapshot()
        {
            if (!Debug.isDebugBuild) return;
            if (!CanCaptureSnapshot) { ReproductionError("repro.combat_only"); return; }
            try
            {
                ReproductionSnapshotPath = ReproductionFiles.Capture(Simulation.State, Profile);
                ReproductionMessageKey = "repro.saved";
                ReproductionDetails = Path.GetFileName(ReproductionSnapshotPath);
                RefreshCurrentScreen();
            }
            catch (Exception exception) when (IsReproductionFileError(exception)) { ReproductionFailure(exception); }
        }

        public void LoadReproductionSnapshot()
        {
            if (!Debug.isDebugBuild) return;
            try
            {
                var payload = ReproductionFiles.Load(ReproductionSnapshotPath?.Trim() ?? string.Empty, out var differentBuild);
                payload.run.isPaused = true; // File remains immutable; loading never resumes a dangerous frame automatically.
                ActivateReproduction(payload.profile, new GameSimulation(payload.run));
                ReproductionMessageKey = differentBuild ? "repro.different_build" : "repro.loaded";
                ReproductionDetails = Path.GetFileName(ReproductionSnapshotPath);
                ReproductionPanelOpen = true; // Show result/version warning before letting the user resume.
                RefreshCurrentScreen();
            }
            catch (Exception exception) when (IsReproductionFileError(exception)) { ReproductionFailure(exception); }
        }

        public void SelectLatestReproductionSnapshot()
        {
            if (!Debug.isDebugBuild) return;
            try
            {
                ReproductionSnapshotPath = ReproductionFiles.LatestSnapshot();
                ReproductionMessageKey = string.IsNullOrEmpty(ReproductionSnapshotPath) ? "repro.snapshot_missing" : "repro.selected";
                ReproductionDetails = string.IsNullOrEmpty(ReproductionSnapshotPath) ? string.Empty : Path.GetFileName(ReproductionSnapshotPath);
                RefreshCurrentScreen();
            }
            catch (Exception exception) when (IsReproductionFileError(exception)) { ReproductionFailure(exception); }
        }

        public void StepReproduction()
        {
            if (!Debug.isDebugBuild || AudioSettingsOpen || !CanStepReproduction) return;
            try
            {
                ReproductionStore.Step(Simulation);
                saves.SaveRun(Simulation.State);
                previousPhase = Simulation.State.phase;
                RefreshCurrentScreen();
            }
            catch (Exception exception) when (IsReproductionFileError(exception)) { ReproductionFailure(exception); }
        }

        public void ReturnFromReproduction()
        {
            if (!Debug.isDebugBuild || !IsReproduction) return;
            saves = normalSaves; Profile = normalProfile; Simulation = normalSimulation; Screen = normalScreen;
            L10n.Language = normalLanguage; LastCommandMessage = normalCommandMessage;
            normalSaves = null; normalProfile = null; normalSimulation = null;
            Audio.ApplySettings(Profile.audio); Audio.Observe(Simulation);
            ui.SetScale(Profile.accessibility.uiScale);
            previousPhase = Simulation == null ? GamePhase.MainMenu : Simulation.State.phase;
            ReproductionMessageKey = "repro.returned"; ReproductionDetails = string.Empty;
            ReproductionPanelOpen = true; // The normal battle stays frozen until the panel is explicitly closed.
            RefreshCurrentScreen();
        }

        private void ActivateReproduction(ProfileState profile, GameSimulation simulation)
        {
            var isolatedSaves = new SaveService(ReproductionFiles.SessionDirectory);
            // Complete IO before swapping any live references; failed imports leave the current game untouched.
            isolatedSaves.SaveProfile(profile);
            if (simulation != null) isolatedSaves.SaveRun(simulation.State);
            if (!IsReproduction)
            {
                normalSaves = saves; normalProfile = Profile; normalSimulation = Simulation;
                normalScreen = Screen; normalLanguage = L10n.Language; normalCommandMessage = LastCommandMessage;
            }
            saves = isolatedSaves; Profile = profile; Simulation = simulation;
            Screen = simulation == null ? FrontendScreen.Menu : FrontendScreen.Game;
            L10n.Language = profile.language;
            Audio.ApplySettings(profile.audio); Audio.Observe(simulation);
            ui.SetScale(profile.accessibility.uiScale);
            previousPhase = simulation == null ? GamePhase.MainMenu : simulation.State.phase;
            LastCommandMessage = string.Empty;
        }

        private void ReproductionError(string key)
        {
            ReproductionMessageKey = key; ReproductionDetails = string.Empty;
            ReproductionPanelOpen = true; RefreshCurrentScreen();
        }

        private void ReproductionFailure(Exception exception)
        {
            ReproductionError(exception is InvalidDataException && exception.Message.StartsWith("repro.", StringComparison.Ordinal)
                ? exception.Message : "repro.file_error");
        }

        private static bool IsReproductionFileError(Exception exception) =>
            exception is InvalidDataException || exception is IOException || exception is UnauthorizedAccessException ||
            exception is ArgumentException || exception is NotSupportedException;

        private bool PrepareDevelopmentSession(string[] args)
        {
            if (!Debug.isDebugBuild || !Array.Exists(args, arg => arg.StartsWith("-debug-", StringComparison.Ordinal))) return false;
            try
            {
                // All existing debug entry points now inherit an isolated profile/run destination.
                ActivateReproduction(JsonUtility.FromJson<ProfileState>(JsonUtility.ToJson(Profile)), null);
                var seedIndex = Array.IndexOf(args, "-debug-seed");
                var snapshotIndex = Array.IndexOf(args, "-debug-snapshot");
                var seededBattle = Array.IndexOf(args, "-debug-battle") >= 0;
                var legacyScenario = Array.Exists(new[] { "-debug-combat", "-debug-route", "-debug-event", "-debug-port", "-debug-setup", "-debug-damage", "-debug-pilots", "-debug-unpaused" }, key => Array.IndexOf(args, key) >= 0);
                var difficultyIndex = Array.IndexOf(args, "-debug-difficulty");
                var flagshipIndex = Array.IndexOf(args, "-debug-flagship");
                if ((seedIndex >= 0 && snapshotIndex >= 0) || ((seedIndex >= 0 || snapshotIndex >= 0) && legacyScenario) ||
                    (seededBattle && seedIndex < 0) || (difficultyIndex >= 0 && seedIndex < 0) || (snapshotIndex >= 0 && flagshipIndex >= 0))
                { ReproductionError("repro.invalid_arguments"); return true; }
                if (snapshotIndex >= 0)
                {
                    ReproductionSnapshotPath = snapshotIndex + 1 < args.Length ? args[snapshotIndex + 1] : string.Empty;
                    LoadReproductionSnapshot();
                    if (Array.IndexOf(args, "-debug-english") >= 0) L10n.Language = Language.English;
                    if (Array.IndexOf(args, "-debug-high-contrast") >= 0) Profile.accessibility.highContrast = true;
                    RefreshCurrentScreen();
                    return true;
                }
                if (seedIndex >= 0)
                {
                    ReproductionSeed = seedIndex + 1 < args.Length ? args[seedIndex + 1] : string.Empty;
                    if (flagshipIndex >= 0) ReproductionFlagship = flagshipIndex + 1 < args.Length ? args[flagshipIndex + 1] : string.Empty;
                    if (difficultyIndex >= 0 && (difficultyIndex + 1 >= args.Length || !Enum.TryParse(args[difficultyIndex + 1], true, out ReproductionDifficulty) ||
                        !Enum.IsDefined(typeof(Difficulty), ReproductionDifficulty)))
                    { ReproductionError("repro.invalid_config"); return true; }
                    if (Array.IndexOf(args, "-debug-english") >= 0) Profile.language = Language.English;
                    if (Array.IndexOf(args, "-debug-high-contrast") >= 0) Profile.accessibility.highContrast = true;
                    StartSeededReproduction(seededBattle);
                    return true;
                }
                if (Array.IndexOf(args, "-debug-repro") >= 0)
                {
                    if (Array.IndexOf(args, "-debug-english") >= 0) L10n.Language = Language.English;
                    if (Array.IndexOf(args, "-debug-high-contrast") >= 0) Profile.accessibility.highContrast = true;
                    ReproductionPanelOpen = true; RefreshCurrentScreen(); return true;
                }
                return false; // Continue through legacy debug scenarios in the isolated session.
            }
            catch (Exception exception) when (IsReproductionFileError(exception)) { ReproductionFailure(exception); return true; }
        }
    }
}
