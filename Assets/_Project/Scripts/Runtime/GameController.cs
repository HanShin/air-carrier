using System;
using AetherArk.Content;
using AetherArk.Core;
using UnityEngine;

namespace AetherArk.Runtime
{
    public enum FrontendScreen { Menu, Setup, Game }

    public sealed class GameController : MonoBehaviour
    {
        public ProfileState Profile { get; private set; }
        public GameSimulation Simulation { get; private set; }
        public LocalizationService L10n { get; private set; }
        public Texture2D Background { get; private set; }
        public FrontendScreen Screen { get; private set; }
        public string LastCommandMessage { get; private set; }

        private SaveService saves;
        private UiFactory ui;
        private GameView view;
        private GamePhase previousPhase;
        private float refreshTimer;

        private void Awake()
        {
            saves = new SaveService();
            Profile = saves.LoadProfile();
            L10n = new LocalizationService(Profile.language);
            Background = Resources.Load<Texture2D>("Art/sky_storm_background");
            ui = new UiFactory(Profile.accessibility.uiScale);
            view = new GameView(this, ui);
            ShowMenu();
            TryDebugCombatLaunch();
        }

        /// <summary>
        /// Development-build shortcut: `-debug-combat <enemy config id without the enemy_ prefix>` opens a paused battle (add `-debug-unpaused` to start it running, `-debug-damage` to pre-apply a hazard showcase)
        /// against the requested enemy on a post-tutorial profile so the combat screen can be inspected
        /// without keyboard automation. Ignored in release builds.
        /// </summary>
        /// <summary>
        /// `-debug-route [jumps]` opens the route map on a post-tutorial run after auto-resolving the
        /// given number of jumps, so storm bands, visited and blocked nodes can be inspected.
        /// </summary>
        private bool TryDebugRouteLaunch(string[] args)
        {
            var index = Array.IndexOf(args, "-debug-route");
            if (index < 0) return false;
            var jumps = index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) ? parsed : 0;
            Profile.tutorialSeen = true;
            Simulation = GameSimulation.NewRun(Profile, 41234);
            for (var jump = 0; jump < jumps && Simulation.State.phase == GamePhase.RouteMap; jump++)
            {
                var nodes = Simulation.State.routeNodes;
                var destination = nodes.Find(node => Simulation.CanTravelTo(node) && !RouteRules.IsHostile(node.encounterType))
                                  ?? nodes.Find(Simulation.CanTravelTo);
                if (destination == null) break;
                Simulation.TravelTo(destination.id);
                if (Simulation.State.phase == GamePhase.Encounter) Simulation.SkipEncounter();
                else if (Simulation.State.phase == GamePhase.Combat)
                {
                    Simulation.ApplyDamage(Simulation.State.enemyShip, ShipSystemType.AetherCore, 999f, true);
                    Simulation.SetPaused(false);
                    Simulation.Tick(0.1f);
                }
            }
            Screen = FrontendScreen.Game;
            previousPhase = Simulation.State.phase;
            view.ShowGamePhase();
            return true;
        }

        /// <summary>`-debug-event <id>` opens the given encounter on a post-tutorial run.</summary>
        private bool TryDebugEventLaunch(string[] args)
        {
            var index = Array.IndexOf(args, "-debug-event");
            if (index < 0) return false;
            var id = index + 1 < args.Length ? args[index + 1] : "burning_ferry";
            if (ContentCatalog.GetEncounter(id) == null) return false;
            Profile.tutorialSeen = true;
            Simulation = GameSimulation.NewRun(Profile, 41234);
            Simulation.State.phase = GamePhase.Encounter;
            Simulation.State.activeEncounterId = id;
            Screen = FrontendScreen.Game;
            previousPhase = Simulation.State.phase;
            view.ShowGamePhase();
            return true;
        }

        /// <summary>`-debug-port` opens the port after clearing the first gate on a post-tutorial run.</summary>
        private bool TryDebugPortLaunch(string[] args)
        {
            if (Array.IndexOf(args, "-debug-port") < 0) return false;
            Profile.tutorialSeen = true;
            Simulation = GameSimulation.NewRun(Profile, 41234);
            Simulation.State.resources.salvage = 40;
            Simulation.BeginCombat(2, true);
            Simulation.ApplyDamage(Simulation.State.enemyShip, ShipSystemType.AetherCore, 999f, true);
            Simulation.SetPaused(false);
            Simulation.Tick(0.1f);
            Screen = FrontendScreen.Game;
            previousPhase = Simulation.State.phase;
            view.ShowGamePhase();
            return true;
        }

        private void TryDebugCombatLaunch()
        {
            if (!Debug.isDebugBuild) return;
            var args = Environment.GetCommandLineArgs();
            if (TryDebugRouteLaunch(args)) return;
            if (TryDebugEventLaunch(args)) return;
            if (Array.IndexOf(args, "-debug-setup") >= 0) { ShowSetup(); return; }
            if (TryDebugPortLaunch(args)) return;
            var index = Array.IndexOf(args, "-debug-combat");
            if (index < 0) return;
            var wanted = index + 1 < args.Length && !args[index + 1].StartsWith("-") ? args[index + 1].ToLowerInvariant() : "cutter";
            var definition = ContentCatalog.GetEnemyDefinition("enemy_" + wanted);
            var tier = definition?.tier ?? 1;
            Profile.tutorialSeen = true;
            for (var seed = 1; seed < 4000; seed++)
            {
                Simulation = GameSimulation.NewRun(Profile, seed);
                if (definition != null) Simulation.State.regionIndex = definition.minRegion; // region-gated configs need their region
                Simulation.BeginCombat(tier, false);
                if (Simulation.State.enemyShip.id == "enemy_" + wanted) break;
            }
            if (Array.IndexOf(args, "-debug-damage") >= 0) DebugScenarios.ApplyDamageShowcase(Simulation.State);
            if (Array.IndexOf(args, "-debug-unpaused") >= 0) Simulation.SetPaused(false);
            Screen = FrontendScreen.Game;
            previousPhase = Simulation.State.phase;
            view.ShowGamePhase();
        }

        private void Update()
        {
            if (HandleKeyboardShortcuts()) return;
            if (Simulation == null || Screen != FrontendScreen.Game) return;
            if (Simulation.State.phase == GamePhase.Combat && Input.GetKeyDown(GetPauseKey()))
            {
                Simulation.TogglePause();
                PersistAndRefresh();
                return;
            }

            var before = Simulation.State.phase;
            Simulation.Tick(Time.unscaledDeltaTime * Profile.accessibility.combatSpeed);
            if (Simulation.State.phase != before)
            {
                RecordFirstExpeditionCompletion();
                saves.SaveRun(Simulation.State);
                previousPhase = Simulation.State.phase;
                view.ShowGamePhase();
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            // Nothing on the combat screen changes while paused; commands trigger their own refresh.
            if (Simulation.State.phase == GamePhase.Combat && !Simulation.State.isPaused && refreshTimer <= 0f)
            {
                refreshTimer = Profile.accessibility.reducedMotion ? 0.45f : 0.2f;
                view.ShowGamePhase();
            }
        }

        private bool HandleKeyboardShortcuts()
        {
            if (Screen == FrontendScreen.Menu)
            {
                if (Input.GetKeyDown(KeyCode.N)) { ShowSetup(); return true; }
                if (Input.GetKeyDown(KeyCode.L)) { ToggleLanguage(); return true; }
                if (Input.GetKeyDown(KeyCode.C) && saves.HasRun()) { ContinueRun(); return true; }
                return false;
            }

            if (Screen == FrontendScreen.Setup)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { StartRun(); return true; }
                if (Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return true; }
                return false;
            }

            if (Simulation == null) return false;
            switch (Simulation.State.phase)
            {
                case GamePhase.RouteMap:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        view.ConfirmRouteSelection();
                        return true;
                    }
                    for (var i = 0; i < 9; i++)
                    {
                        if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) continue;
                        var availableIndex = 0;
                        for (var n = 0; n < Simulation.State.routeNodes.Count; n++)
                        {
                            var node = Simulation.State.routeNodes[n];
                            if (!Simulation.CanTravelTo(node)) continue;
                            if (availableIndex++ == i) Travel(node.id);
                        }
                        return true;
                    }
                    break;

                case GamePhase.Port:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { view.ConfirmPort(); return true; }
                    break;

                case GamePhase.Encounter:
                    var encounter = Simulation.ActiveEncounter;
                    if (encounter == null) break;
                    var visibleIndex = 0;
                    for (var i = 0; i < encounter.choices.Count && visibleIndex < 9; i++)
                    {
                        if (encounter.choices[i].hidden) continue;
                        var slot = visibleIndex++;
                        if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + slot))) continue;
                        if (Simulation.CanChoose(encounter.choices[i])) ChooseEncounter(encounter.choices[i].id);
                        return true;
                    }
                    break;

                case GamePhase.Combat:
                    if (Input.GetKeyDown(KeyCode.F)) { Fire(ShipSystemType.Weapons); return true; }
                    if (Input.GetKeyDown(KeyCode.S)) { UseSupport(); return true; }
                    if (Input.GetKeyDown(KeyCode.R)) { EmergencyOrdnance(); return true; }
                    for (var i = 0; i < Simulation.State.squadrons.Count && i < 2; i++)
                    {
                        if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) continue;
                        LaunchSquadron(Simulation.State.squadrons[i].id, SquadronMission.Bombard, ShipSystemType.Weapons);
                        return true;
                    }
                    break;

                case GamePhase.Victory:
                case GamePhase.Defeat:
                    if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape)) { AbandonRun(); return true; }
                    break;
            }

            return false;
        }

        private KeyCode GetPauseKey()
        {
            return Enum.TryParse(Profile.accessibility.pauseKey, out KeyCode key) ? key : KeyCode.Space;
        }

        public void ShowMenu()
        {
            Screen = FrontendScreen.Menu;
            Simulation = null;
            LastCommandMessage = string.Empty;
            view.ShowMenu(saves.HasRun());
        }

        public void ShowSetup()
        {
            Screen = FrontendScreen.Setup;
            Simulation = null;
            LastCommandMessage = string.Empty;
            view.ShowSetup();
        }

        public void StartRun()
        {
            if (string.IsNullOrWhiteSpace(Profile.captainName)) Profile.captainName = L10n.Language == Language.Korean ? "아린" : "Arin";
            saves.SaveProfile(Profile);
            saves.ClearRun();
            var seed = Profile.tutorialSeen
                ? unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF))
                : GameSimulation.FirstExpeditionSeed;
            Simulation = GameSimulation.NewRun(Profile, seed);
            Screen = FrontendScreen.Game;
            previousPhase = Simulation.State.phase;
            saves.SaveRun(Simulation.State);
            view.ShowGamePhase();
        }

        public void ContinueRun()
        {
            var run = saves.LoadRun();
            if (run == null)
            {
                ShowMenu();
                return;
            }
            Simulation = new GameSimulation(run);
            Screen = FrontendScreen.Game;
            previousPhase = run.phase;
            view.ShowGamePhase();
        }

        public void SetCaptainName(string value)
        {
            Profile.captainName = value?.Trim() ?? string.Empty;
            saves.SaveProfile(Profile);
        }

        public void ToggleLanguage()
        {
            Profile.language = Profile.language == Language.Korean ? Language.English : Language.Korean;
            L10n.Language = Profile.language;
            saves.SaveProfile(Profile);
            if (Screen == FrontendScreen.Menu) view.ShowMenu(saves.HasRun());
            else if (Screen == FrontendScreen.Setup) view.ShowSetup();
            else view.ShowGamePhase();
        }

        public void CycleLineage()
        {
            Profile.captainLineage = Next(Profile.captainLineage);
            SaveSetupAndRefresh();
        }

        public void CycleSupport()
        {
            Profile.supportShip = Next(Profile.supportShip);
            SaveSetupAndRefresh();
        }

        public void CycleDifficulty()
        {
            Profile.difficulty = Next(Profile.difficulty);
            SaveSetupAndRefresh();
        }

        public void CycleFlagship()
        {
            var unlocked = UnlockRules.UnlockedFlagships(Profile);
            if (unlocked.Count == 0) return;
            var index = unlocked.IndexOf(Profile.flagshipId);
            Profile.flagshipId = unlocked[(index + 1) % unlocked.Count];
            SaveSetupAndRefresh();
        }

        public void ToggleAutoPause()
        {
            Profile.accessibility.autoPauseOnWarning = !Profile.accessibility.autoPauseOnWarning;
            SaveSetupAndRefresh();
        }

        public void CycleCombatSpeed()
        {
            var speed = Profile.accessibility.combatSpeed;
            Profile.accessibility.combatSpeed = speed < 0.75f ? 1f : speed < 1.5f ? 2f : 0.5f;
            SaveSetupAndRefresh();
        }

        public void CycleUiScale()
        {
            var scale = Profile.accessibility.uiScale;
            Profile.accessibility.uiScale = scale < 0.95f ? 1f : scale < 1.1f ? 1.15f : 0.9f;
            ui.SetScale(Profile.accessibility.uiScale);
            SaveSetupAndRefresh();
        }

        public void ToggleHighContrast()
        {
            Profile.accessibility.highContrast = !Profile.accessibility.highContrast;
            SaveSetupAndRefresh();
        }

        public void ToggleReducedMotion()
        {
            Profile.accessibility.reducedMotion = !Profile.accessibility.reducedMotion;
            SaveSetupAndRefresh();
        }

        public void CyclePauseKey()
        {
            Profile.accessibility.pauseKey = Profile.accessibility.pauseKey == "Space" ? "P" : "Space";
            SaveSetupAndRefresh();
        }

        private void SaveSetupAndRefresh()
        {
            saves.SaveProfile(Profile);
            view.ShowSetup();
        }

        public void Travel(string nodeId) => Apply(() => Simulation.TravelTo(nodeId));
        public void PurchaseModule(string moduleId) => Apply(() => Simulation.PurchaseModule(moduleId));
        public void DepartPort() => Apply(() => Simulation.DepartPort());
        public void ChooseEncounter(string choiceId) => Apply(() => Simulation.ChooseEncounter(choiceId));
        public void SkipEncounter() => Apply(() => Simulation.SkipEncounter());
        public void TogglePause() { Simulation.TogglePause(); PersistAndRefresh(); }
        public void ToggleCombatAutoPause()
        {
            if (Simulation == null) return;
            Profile.accessibility.autoPauseOnWarning = !Profile.accessibility.autoPauseOnWarning;
            Simulation.State.autoPauseOnWarning = Profile.accessibility.autoPauseOnWarning;
            saves.SaveProfile(Profile);
            PersistAndRefresh();
        }
        public void ChangePower(ShipSystemType type, int delta) => Apply(() => Simulation.Execute(new SetPowerCommand(type, delta)));
        public void Fire(ShipSystemType target) => Apply(() => Simulation.Execute(new FireWeaponCommand(target)));
        public void FireSlot(int slot, ShipSystemType target) => Apply(() => Simulation.FireWeapon(slot, target));
        public void PurchaseWeapon(string weaponId) => Apply(() => Simulation.PurchaseWeapon(weaponId));
        public void PurchaseWing(string wingId) => Apply(() => Simulation.PurchaseWing(wingId));
        public void ChangeAltitude(AltitudeBand altitude) => Apply(() => Simulation.Execute(new ChangeAltitudeCommand(altitude)));
        public void MoveCrew(string crewId, ShipSystemType room) => Apply(() => Simulation.Execute(new MoveCrewCommand(crewId, room)));
        public void Overcharge(ShipSystemType type) => Apply(() => Simulation.Execute(new OverchargeCommand(type)));
        public void LaunchSquadron(string squadronId, SquadronMission mission, ShipSystemType target) =>
            Apply(() => Simulation.Execute(new LaunchSquadronCommand(squadronId, mission, target)));
        public void UseSupport() => Apply(() => Simulation.UseSupportAbility());
        public void FieldRepair() => Apply(() => Simulation.FieldRepair());
        public void RefitSquadron() => Apply(() => Simulation.RefitSquadrons());
        public void EmergencyOrdnance() => Apply(() => Simulation.EmergencyOrdnanceAssembly());
        public void EmergencyAether() => Apply(() => Simulation.EmergencyAetherBurn());

        private void Apply(Func<CommandResult> action)
        {
            if (Simulation == null) return;
            var result = action();
            LastCommandMessage = result.messageKey;
            PersistAndRefresh();
        }

        private void PersistAndRefresh()
        {
            if (Simulation == null) return;
            RecordFirstExpeditionCompletion();
            saves.SaveRun(Simulation.State);
            previousPhase = Simulation.State.phase;
            view.ShowGamePhase();
        }

        private void RecordFirstExpeditionCompletion()
        {
            if (Simulation == null || Simulation.State.phase != GamePhase.Victory) return;
            UnlockRules.RecordVictory(Profile, Simulation.State);
            saves.SaveProfile(Profile);
        }

        public void AbandonRun()
        {
            saves.ClearRun();
            ShowMenu();
        }

        public void Quit()
        {
            Application.Quit();
        }

        private static T Next<T>(T value) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var index = Array.IndexOf(values, value);
            return values[(index + 1) % values.Length];
        }
    }
}
