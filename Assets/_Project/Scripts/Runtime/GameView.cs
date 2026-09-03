using System;
using System.Collections.Generic;
using System.Text;
using AetherArk.Content;
using AetherArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AetherArk.Runtime
{
    public sealed class GameView
    {
        private readonly GameController controller;
        private readonly UiFactory ui;
        private readonly LocalizationService l10n;
        private string selectedCrewId;
        private ShipSystemType selectedPlayerSystem = ShipSystemType.Weapons;
        private ShipSystemType selectedEnemySystem = ShipSystemType.Weapons;

        private Color PanelColor => controller.Profile.accessibility.highContrast ? new Color(0.01f, 0.02f, 0.035f, 0.99f) : UiFactory.Panel;

        public GameView(GameController gameController, UiFactory factory)
        {
            controller = gameController;
            ui = factory;
            l10n = gameController.L10n;
        }

        public void ShowMenu(bool hasRun)
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.01f, 0.025f, 0.055f, 0.48f));
            ui.Text("Title", ui.Root, l10n.T("game.title"), 82, UiFactory.TextPrimary, TextAnchor.MiddleCenter,
                new Vector2(510f, 690f), new Vector2(900f, 120f), FontStyle.Bold);
            ui.Text("Subtitle", ui.Root, l10n.T("game.subtitle"), 24, UiFactory.Brass, TextAnchor.MiddleCenter,
                new Vector2(510f, 645f), new Vector2(900f, 50f));

            var panel = ui.PanelRect("MenuPanel", ui.Root, new Vector2(700f, 210f), new Vector2(520f, 390f), PanelColor);
            if (hasRun)
            {
                ui.Button("Continue", panel, l10n.T("menu.continue"), controller.ContinueRun,
                    new Vector2(60f, 278f), new Vector2(400f, 62f), UiFactory.Aether, UiFactory.Ink, 22);
            }
            ui.Button("NewRun", panel, "[N] " + l10n.T("menu.new_run"), controller.ShowSetup,
                new Vector2(60f, 196f), new Vector2(400f, 62f), UiFactory.Brass, UiFactory.Ink, 22);
            ui.Button("Language", panel, l10n.T("menu.language"), controller.ToggleLanguage,
                new Vector2(60f, 114f), new Vector2(400f, 62f));
            ui.Button("Quit", panel, l10n.T("menu.quit"), controller.Quit,
                new Vector2(60f, 32f), new Vector2(400f, 62f), new Color(0.22f, 0.13f, 0.16f, 0.95f));
            ui.Text("Version", ui.Root, "VERTICAL SLICE 0.2 · COMBAT UX", 15, UiFactory.TextMuted, TextAnchor.MiddleRight,
                new Vector2(1500f, 28f), new Vector2(360f, 30f));
        }

        public void ShowSetup()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.01f, 0.02f, 0.045f, 0.72f));
            ui.Text("SetupTitle", ui.Root, l10n.T("setup.title"), 46, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(190f, 955f), new Vector2(700f, 72f), FontStyle.Bold);

            var left = ui.PanelRect("IdentityPanel", ui.Root, new Vector2(180f, 170f), new Vector2(760f, 740f), PanelColor);
            ui.Text("CaptainLabel", left, l10n.T("setup.captain"), 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(52f, 642f), new Vector2(260f, 40f), FontStyle.Bold);
            var input = ui.Input("CaptainInput", left, controller.Profile.captainName, l10n.T("setup.captain"),
                new Vector2(52f, 580f), new Vector2(656f, 58f));
            input.onValueChanged.AddListener(controller.SetCaptainName);

            ui.Text("LineageLabel", left, l10n.T("setup.lineage"), 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(52f, 510f), new Vector2(260f, 40f), FontStyle.Bold);
            ui.Button("Lineage", left, "◀  " + l10n.EnumName(controller.Profile.captainLineage) + "  ▶", controller.CycleLineage,
                new Vector2(52f, 450f), new Vector2(656f, 56f));

            ui.Text("SupportLabel", left, l10n.T("setup.support"), 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(52f, 380f), new Vector2(260f, 40f), FontStyle.Bold);
            ui.Button("Support", left, "◀  " + l10n.EnumName(controller.Profile.supportShip) + "  ▶", controller.CycleSupport,
                new Vector2(52f, 320f), new Vector2(656f, 56f));

            ui.Text("DifficultyLabel", left, l10n.T("setup.difficulty"), 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(52f, 250f), new Vector2(260f, 40f), FontStyle.Bold);
            ui.Button("Difficulty", left, "◀  " + l10n.EnumName(controller.Profile.difficulty) + "  ▶", controller.CycleDifficulty,
                new Vector2(52f, 190f), new Vector2(656f, 56f));
            ui.Text("Warning", left, l10n.T("setup.warning"), 17, UiFactory.Danger, TextAnchor.MiddleCenter,
                new Vector2(52f, 110f), new Vector2(656f, 54f), FontStyle.Bold);

            var settings = ui.PanelRect("AccessibilityPanel", ui.Root, new Vector2(980f, 250f), new Vector2(760f, 660f), PanelColor);
            ui.Text("AccessTitle", settings, L("접근성·조작", "ACCESSIBILITY & CONTROLS"), 24, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(48f, 580f), new Vector2(620f, 50f), FontStyle.Bold);
            AddSettingButton(settings, 500f, L("경고 시 자동 정지", "Auto-pause on warning"), On(controller.Profile.accessibility.autoPauseOnWarning), controller.ToggleAutoPause);
            AddSettingButton(settings, 425f, L("전투 속도", "Combat speed"), controller.Profile.accessibility.combatSpeed.ToString("0.0") + "×", controller.CycleCombatSpeed);
            AddSettingButton(settings, 350f, L("UI 크기", "UI scale"), Mathf.RoundToInt(controller.Profile.accessibility.uiScale * 100f) + "%", controller.CycleUiScale);
            AddSettingButton(settings, 275f, L("고대비 패널", "High contrast panels"), On(controller.Profile.accessibility.highContrast), controller.ToggleHighContrast);
            AddSettingButton(settings, 200f, L("움직임 감소", "Reduced motion"), On(controller.Profile.accessibility.reducedMotion), controller.ToggleReducedMotion);
            AddSettingButton(settings, 125f, L("일시정지 키", "Pause key"), controller.Profile.accessibility.pauseKey, controller.CyclePauseKey);
            ui.Button("Language", settings, l10n.T("menu.language"), controller.ToggleLanguage,
                new Vector2(48f, 48f), new Vector2(664f, 54f));

            ui.Button("Back", ui.Root, "[Esc] " + l10n.T("setup.back"), controller.ShowMenu,
                new Vector2(180f, 76f), new Vector2(260f, 64f));
            ui.Button("Launch", ui.Root, "[Enter] " + l10n.T("setup.launch"), controller.StartRun,
                new Vector2(1240f, 76f), new Vector2(500f, 64f), UiFactory.Brass, UiFactory.Ink, 23);
        }

        private void AddSettingButton(Transform panel, float y, string label, string value, Action action)
        {
            ui.Text(label + "Label", panel, label, 18, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(48f, y), new Vector2(370f, 54f));
            ui.Button(label + "Value", panel, value, action, new Vector2(440f, y), new Vector2(272f, 54f));
        }

        public void ShowGamePhase()
        {
            if (controller.Simulation == null) return;
            switch (controller.Simulation.State.phase)
            {
                case GamePhase.RouteMap: ShowRoute(); break;
                case GamePhase.Encounter: ShowEncounter(); break;
                case GamePhase.Combat: ShowCombat(); break;
                case GamePhase.Victory: ShowEnding(true); break;
                case GamePhase.Defeat: ShowEnding(false); break;
            }
        }

        private void AddStatusBar()
        {
            var state = controller.Simulation.State;
            var bar = ui.PanelRect("StatusBar", ui.Root, new Vector2(0f, 990f), new Vector2(1920f, 90f), UiFactory.Ink);
            ui.Text("ShipName", bar, state.playerShip.displayName, 24, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(28f, 12f), new Vector2(300f, 66f), FontStyle.Bold);
            var resources = $"{l10n.T("ui.aether")} {state.resources.aether}    {l10n.T("ui.supplies")} {state.resources.supplies}    {l10n.T("ui.ordnance")} {state.resources.ordnance}    {l10n.T("ui.salvage")} {state.resources.salvage}";
            ui.Text("Resources", bar, resources, 19, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(340f, 12f), new Vector2(620f, 66f));
            var convoy = $"{l10n.T("ui.survivors")} {state.convoy.survivors:N0}    {l10n.T("ui.morale")} {state.convoy.morale}%    {l10n.EnumName(state.convoy.supportShip)}";
            ui.Text("Convoy", bar, convoy, 19, UiFactory.Aether, TextAnchor.MiddleRight,
                new Vector2(990f, 12f), new Vector2(760f, 66f), FontStyle.Bold);
            ui.Button("Menu", bar, l10n.T("ui.abandon"), controller.AbandonRun, new Vector2(1750f, 17f), new Vector2(140f, 56f),
                new Color(0.32f, 0.09f, 0.12f, 0.95f), UiFactory.TextPrimary, 14);
        }

        private void ShowRoute()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.02f, 0.03f, 0.07f, 0.58f));
            AddStatusBar();
            ui.Text("RouteTitle", ui.Root, l10n.T("ui.route_title"), 38, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(84f, 905f), new Vector2(600f, 62f), FontStyle.Bold);
            ui.Text("RouteHint", ui.Root, l10n.T("ui.route_hint"), 17, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(84f, 864f), new Vector2(1200f, 38f));

            var routePanel = ui.PanelRect("RoutePanel", ui.Root, new Vector2(70f, 190f), new Vector2(1780f, 650f), PanelColor);
            var state = controller.Simulation.State;
            var positions = new Dictionary<string, Vector2>();
            for (var i = 0; i < state.routeNodes.Count; i++) positions[state.routeNodes[i].id] = NodePosition(state.routeNodes[i]);

            for (var i = 0; i < state.routeNodes.Count; i++)
            {
                var node = state.routeNodes[i];
                for (var j = 0; j < node.connectedIds.Count; j++)
                {
                    if (positions.TryGetValue(node.connectedIds[j], out var target))
                        ui.Line(routePanel, positions[node.id] + new Vector2(70f, 36f), target + new Vector2(70f, 36f), 3f,
                            node.blocked ? new Color(0.35f, 0.12f, 0.25f, 0.5f) : new Color(0.35f, 0.56f, 0.62f, 0.52f));
                }
            }

            var availableNumber = 0;
            for (var i = 0; i < state.routeNodes.Count; i++)
            {
                var node = state.routeNodes[i];
                var available = controller.Simulation.CanTravelTo(node);
                var current = node.id == state.currentNodeId;
                var label = l10n.T(node.nameKey);
                if (available) label = $"[{++availableNumber}] " + label;
                if (current) label += "\n" + l10n.T("ui.current");
                else if (node.blocked) label += "\n" + l10n.T("ui.blocked");
                else label += $"\n{l10n.T("ui.cost", node.aetherCost.ToString())} · {l10n.EnumName(node.recommendedAltitude)}\n{l10n.T(ContentCatalog.GetWeather(node.weather).nameKey)}";

                var color = current ? UiFactory.Brass : node.blocked ? new Color(0.34f, 0.08f, 0.15f, 0.95f) : available ? new Color(0.08f, 0.38f, 0.42f, 0.98f) : UiFactory.PanelSoft;
                var localNode = node;
                var button = ui.Button("Node_" + node.id, routePanel, label, () => controller.Travel(localNode.id),
                    positions[node.id], new Vector2(140f, 74f), color, current ? UiFactory.Ink : UiFactory.TextPrimary, 14);
                button.interactable = available;
            }

            ui.Button("FieldRepair", ui.Root, l10n.T("ui.field_repair"), controller.FieldRepair,
                new Vector2(80f, 80f), new Vector2(350f, 70f));
            ui.Button("Refit", ui.Root, l10n.T("ui.refit"), controller.RefitSquadron,
                new Vector2(450f, 80f), new Vector2(350f, 70f));
            var supportLabel = l10n.T("ui.support_call");
            if (state.convoy.supportCooldown > 0) supportLabel += " — " + l10n.T("ui.cooldown", state.convoy.supportCooldown.ToString());
            var support = ui.Button("Support", ui.Root, supportLabel, controller.UseSupport,
                new Vector2(820f, 80f), new Vector2(420f, 70f), UiFactory.Violet);
            support.interactable = state.convoy.supportCooldown <= 0;
            AddLastReport(new Vector2(1270f, 70f), new Vector2(580f, 92f));
            if (!controller.Simulation.HasAffordableRoute())
            {
                ui.Button("EmergencyAether", ui.Root, l10n.T("ui.emergency_aether"), controller.EmergencyAether,
                    new Vector2(80f, 158f), new Vector2(520f, 34f), UiFactory.Danger, UiFactory.TextPrimary, 14);
            }
        }

        private static Vector2 NodePosition(RouteNodeState node)
        {
            return new Vector2(55f + node.column * 220f, 470f - node.lane * 170f);
        }

        private void ShowEncounter()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.01f, 0.02f, 0.05f, 0.78f));
            AddStatusBar();
            var encounter = controller.Simulation.ActiveEncounter;
            if (encounter == null)
            {
                controller.SkipEncounter();
                return;
            }

            var panel = ui.PanelRect("EncounterPanel", ui.Root, new Vector2(310f, 150f), new Vector2(1300f, 740f), PanelColor);
            ui.Text("EncounterTitle", panel, l10n.T(encounter.titleKey), 38, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(70f, 620f), new Vector2(1160f, 70f), FontStyle.Bold);
            ui.Text("EncounterBody", panel, l10n.T(encounter.bodyKey), 24, UiFactory.TextPrimary, TextAnchor.UpperLeft,
                new Vector2(70f, 390f), new Vector2(1160f, 210f));

            for (var i = 0; i < encounter.choices.Count; i++)
            {
                var choice = encounter.choices[i];
                var localChoice = choice;
                var button = ui.Button("Choice_" + choice.id, panel, $"[{i + 1}] " + l10n.T(choice.textKey), () => controller.ChooseEncounter(localChoice.id),
                    new Vector2(70f, 292f - i * 82f), new Vector2(1160f, 64f), i == 0 ? new Color(0.13f, 0.34f, 0.36f, 0.98f) : UiFactory.PanelSoft,
                    UiFactory.TextPrimary, 19);
                button.interactable = controller.Simulation.CanChoose(choice);
            }
            AddLastReport(new Vector2(540f, 52f), new Vector2(840f, 72f));
        }

        private void ShowCombat()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.015f, 0.025f, 0.06f, 0.66f));
            AddStatusBar();
            var state = controller.Simulation.State;

            var battleStrip = ui.PanelRect("BattleStrip", ui.Root, new Vector2(20f, 924f), new Vector2(1880f, 56f), new Color(0.025f, 0.045f, 0.08f, 0.96f));
            var status = state.isPaused ? l10n.T("ui.pause") : l10n.T("ui.running");
            ui.Text("BattleStatus", battleStrip, status, 22, state.isPaused ? UiFactory.Brass : UiFactory.Success, TextAnchor.MiddleLeft,
                new Vector2(18f, 4f), new Vector2(220f, 48f), FontStyle.Bold);
            var air = $"{l10n.T("ui.altitude")}: {l10n.EnumName(state.playerShip.altitude)}  |  {l10n.T("ui.weather")}: {l10n.T(ContentCatalog.GetWeather(state.currentWeather).nameKey)}";
            var centerText = string.IsNullOrEmpty(state.combatAlertKey) ? air : FormatAlert();
            var centerColor = string.IsNullOrEmpty(state.combatAlertKey) ? UiFactory.Aether : AlertColor(state.combatAlertSeverity);
            ui.Text("AirState", battleStrip, centerText, 20, centerColor, TextAnchor.MiddleCenter,
                new Vector2(330f, 4f), new Vector2(1180f, 48f), FontStyle.Bold);
            ui.Text("Timer", battleStrip, TimeSpan.FromSeconds(state.combatElapsed).ToString(@"mm\:ss"), 20, UiFactory.TextMuted, TextAnchor.MiddleRight,
                new Vector2(1640f, 4f), new Vector2(200f, 48f));

            BuildCrewColumn();
            BuildPlayerShipPanel();
            BuildCommandPanel();
            BuildEnemyPanel();
            BuildSquadronPanel();
        }

        private void BuildCrewColumn()
        {
            var state = controller.Simulation.State;
            var panel = ui.PanelRect("CrewColumn", ui.Root, new Vector2(20f, 270f), new Vector2(200f, 640f), PanelColor);
            ui.Text("CrewTitle", panel, l10n.T("ui.crew"), 18, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(14f, 596f), new Vector2(170f, 36f), FontStyle.Bold);
            for (var i = 0; i < state.crew.Count && i < 6; i++)
            {
                var crew = state.crew[i];
                var y = 494f - i * 98f;
                var health = crew.isDead ? L("사망", "DEAD") : crew.IsDowned ? L("구조 대기", "DOWNED") : crew.onSortie ? L("출격 중", "SORTIE") : $"HP {crew.health:0}/{crew.maxHealth:0}";
                var label = $"{(crew.isCaptain ? "★ " : "")}{crew.displayName}\n{crew.role}\n{health}";
                var selected = selectedCrewId == crew.id;
                var color = crew.isDead ? new Color(0.22f, 0.08f, 0.1f, 0.95f) : selected ? UiFactory.Violet : UiFactory.PanelSoft;
                var localCrew = crew;
                var button = ui.Button("Crew_" + crew.id, panel, label, () => SelectCrew(localCrew.id),
                    new Vector2(8f, y), new Vector2(184f, 92f), color, UiFactory.TextPrimary, 13);
                button.interactable = !crew.isDead && !crew.onSortie;
                var labelText = button.GetComponentInChildren<Text>();
                if (labelText != null)
                {
                    labelText.alignment = TextAnchor.MiddleLeft;
                    labelText.rectTransform.anchoredPosition = new Vector2(40f, 4f);
                    labelText.rectTransform.sizeDelta = new Vector2(140f, 84f);
                    labelText.resizeTextForBestFit = false;
                }
                var tokenColor = crew.isDead ? new Color(0.3f, 0.3f, 0.3f, 1f) : ShipBlueprintView.LineageColor(crew.lineage);
                ui.Circle("Portrait_" + crew.id, button.transform, new Vector2(8f, 34f), new Vector2(26f, 26f), tokenColor).raycastTarget = false;
                ui.Text("PortraitInitial_" + crew.id, button.transform, BlueprintRules.CrewInitial(crew.displayName), 13, UiFactory.Ink, TextAnchor.MiddleCenter,
                    new Vector2(8f, 34f), new Vector2(26f, 26f), FontStyle.Bold);
                ui.Bar("CrewHealth_" + crew.id, button.transform, crew.maxHealth <= 0f ? 0f : crew.health / crew.maxHealth,
                    new Vector2(8f, 6f), new Vector2(168f, 5f), crew.health < crew.maxHealth * 0.35f ? UiFactory.Danger : UiFactory.Success);
            }
        }

        private void BuildPlayerShipPanel()
        {
            var state = controller.Simulation.State;
            var ship = state.playerShip;
            var panel = ui.PanelRect("PlayerShip", ui.Root, new Vector2(232f, 270f), new Vector2(700f, 640f), PanelColor);
            ui.Text("PlayerTitle", panel, ship.displayName, 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(18f, 596f), new Vector2(230f, 36f), FontStyle.Bold);
            AddDefenseBar(panel, "Ward", l10n.T("ui.ward"), ship.ward, ship.maxWard, 262f, 614f, UiFactory.Aether);
            AddDefenseBar(panel, "Armor", l10n.T("ui.armor"), ship.armor, ship.maxArmor, 408f, 614f, UiFactory.Brass);
            AddDefenseBar(panel, "Hull", l10n.T("ui.hull"), ship.hull, ship.maxHull, 554f, 614f, UiFactory.Success);
            var powerFree = Math.Max(0, ship.coreOutput - ship.AllocatedPower());
            ui.Text("PowerSummary", panel, $"{l10n.T("ui.available_power")} {powerFree}/{ship.coreOutput}  ·  {l10n.T("ui.instability")} {ship.instability:0}%", 13,
                ship.instability >= 70f ? UiFactory.Danger : UiFactory.TextMuted, TextAnchor.MiddleRight, new Vector2(262f, 576f), new Vector2(420f, 24f));
            ui.Bar("InstabilityBar", panel, ship.instability / 100f, new Vector2(554f, 570f), new Vector2(128f, 5f), ship.instability >= 70f ? UiFactory.Danger : UiFactory.Violet);

            var forecast = $"{l10n.T("ui.incoming_fire")} {state.enemyWeaponCooldown:0.0}s   ·   {l10n.T("ui.incoming_airstrike")} {state.enemySquadronCooldown:0.0}s   ·   {l10n.T("ui.weather_hazard")} {state.weatherHazardTimer:0.0}s";
            ui.Text("ThreatForecast", panel, forecast, 13, NextThreat(state) <= 3f ? UiFactory.Danger : UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(18f, 552f), new Vector2(664f, 24f), NextThreat(state) <= 3f ? FontStyle.Bold : FontStyle.Normal);

            var hint = string.IsNullOrEmpty(selectedCrewId) ? L("방을 클릭하면 전력 조절 대상이 됩니다", "Click a room to select it for power control")
                : L("이동할 방을 클릭하십시오", "Click the room to move the selected crew");
            ui.Text("BlueprintHint", panel, hint, 12, string.IsNullOrEmpty(selectedCrewId) ? UiFactory.TextMuted : UiFactory.Aether, TextAnchor.MiddleLeft,
                new Vector2(18f, 10f), new Vector2(664f, 20f));

            ShipBlueprintView.Draw(ui, l10n, panel, ship, ContentCatalog.GetDeckPlan(ship.id), new Vector2(16f, 32f), new Vector2(668f, 516f), new BlueprintOptions
            {
                roomNamePrefix = "Room_",
                selectedSystem = selectedPlayerSystem,
                onRoomClick = SelectRoom,
                crew = state.crew,
                selectedCrewId = selectedCrewId,
                onCrewClick = SelectCrew,
                reducedMotion = controller.Profile.accessibility.reducedMotion,
                highContrast = controller.Profile.accessibility.highContrast,
                showAllocatedPower = true
            });
        }

        private void SelectRoom(ShipSystemType room)
        {
            selectedPlayerSystem = room;
            if (!string.IsNullOrEmpty(selectedCrewId))
            {
                controller.MoveCrew(selectedCrewId, room);
                selectedCrewId = null;
            }
            else ShowCombat();
        }

        private void SelectCrew(string crewId)
        {
            selectedCrewId = selectedCrewId == crewId ? null : crewId;
            ShowCombat();
        }

        private void BuildCommandPanel()
        {
            var state = controller.Simulation.State;
            var ship = state.playerShip;
            var system = ship.GetSystem(selectedPlayerSystem) ?? ship.GetSystem(ShipSystemType.Weapons);
            var panel = ui.PanelRect("CommandPanel", ui.Root, new Vector2(944f, 270f), new Vector2(300f, 640f), PanelColor);
            var pauseKey = controller.Profile.accessibility.pauseKey;
            ui.Button("Pause", panel, $"[{pauseKey}] " + (state.isPaused ? l10n.T("ui.resume") : l10n.T("ui.pause_button")), controller.TogglePause,
                new Vector2(18f, 568f), new Vector2(264f, 54f), state.isPaused ? UiFactory.Brass : UiFactory.Violet, state.isPaused ? UiFactory.Ink : UiFactory.TextPrimary, 17);

            ui.Button("CombatAutoPause", panel, state.autoPauseOnWarning ? l10n.T("ui.auto_pause_on") : l10n.T("ui.auto_pause_off"), controller.ToggleCombatAutoPause,
                new Vector2(18f, 532f), new Vector2(264f, 30f), state.autoPauseOnWarning ? new Color(0.18f, 0.35f, 0.32f, 0.98f) : UiFactory.PanelSoft,
                state.autoPauseOnWarning ? UiFactory.Aether : UiFactory.TextMuted, 12);

            ui.Text("SelectedSystem", panel, l10n.T(system.displayKey), 21, UiFactory.Aether, TextAnchor.MiddleCenter,
                new Vector2(18f, 492f), new Vector2(264f, 36f), FontStyle.Bold);
            var powerDown = ui.Button("PowerDown", panel, "−", () => controller.ChangePower(system.type, -1), new Vector2(18f, 439f), new Vector2(58f, 48f));
            ui.Text("PowerValue", panel, $"{l10n.T("ui.power")} {system.power}/{system.maxPower}\n{ship.AllocatedPower()}/{ship.coreOutput}", 16,
                UiFactory.TextPrimary, TextAnchor.MiddleCenter, new Vector2(82f, 439f), new Vector2(136f, 48f));
            var powerUp = ui.Button("PowerUp", panel, "+", () => controller.ChangePower(system.type, 1), new Vector2(224f, 439f), new Vector2(58f, 48f));
            powerDown.interactable = system.type != ShipSystemType.AetherCore && system.power > 0;
            powerUp.interactable = system.type != ShipSystemType.AetherCore && system.power < system.maxPower && ship.AllocatedPower() < ship.coreOutput;
            var resonatorPresent = state.crew.Exists(crew => crew.role == CrewRole.Resonator && crew.IsActive && crew.currentRoom == system.type);
            var overcharge = ui.Button("Overcharge", panel, l10n.T("ui.overcharge"), () => controller.Overcharge(system.type),
                new Vector2(18f, 380f), new Vector2(264f, 46f), UiFactory.Violet);
            overcharge.interactable = system.maxPower > 0 && system.overchargeSeconds <= 0f && resonatorPresent;

            var fire = ui.Button("Fire", panel, "[F] " + l10n.T("ui.fire") + $"\n{state.playerWeaponCooldown:0.0}s", () => controller.Fire(selectedEnemySystem),
                new Vector2(18f, 310f), new Vector2(264f, 58f), new Color(0.48f, 0.18f, 0.12f, 0.98f));
            var weapons = ship.GetSystem(ShipSystemType.Weapons);
            fire.interactable = state.playerWeaponCooldown <= 0f && weapons != null && weapons.EffectivePower > 0;
            ui.Text("AltitudeLabel", panel, l10n.T("ui.altitude"), 18, UiFactory.Brass, TextAnchor.MiddleCenter,
                new Vector2(18f, 267f), new Vector2(264f, 32f), FontStyle.Bold);
            var low = ui.Button("Low", panel, l10n.T("ui.low"), () => controller.ChangeAltitude(AltitudeBand.Low), new Vector2(18f, 215f), new Vector2(82f, 44f),
                state.playerShip.altitude == AltitudeBand.Low ? UiFactory.Aether : UiFactory.PanelSoft, state.playerShip.altitude == AltitudeBand.Low ? UiFactory.Ink : UiFactory.TextPrimary, 14);
            var medium = ui.Button("Medium", panel, l10n.T("ui.medium"), () => controller.ChangeAltitude(AltitudeBand.Medium), new Vector2(109f, 215f), new Vector2(82f, 44f),
                state.playerShip.altitude == AltitudeBand.Medium ? UiFactory.Aether : UiFactory.PanelSoft, state.playerShip.altitude == AltitudeBand.Medium ? UiFactory.Ink : UiFactory.TextPrimary, 14);
            var high = ui.Button("High", panel, l10n.T("ui.high"), () => controller.ChangeAltitude(AltitudeBand.High), new Vector2(200f, 215f), new Vector2(82f, 44f),
                state.playerShip.altitude == AltitudeBand.High ? UiFactory.Aether : UiFactory.PanelSoft, state.playerShip.altitude == AltitudeBand.High ? UiFactory.Ink : UiFactory.TextPrimary, 14);
            var lift = ship.GetSystem(ShipSystemType.LiftArray);
            var canChangeAltitude = state.altitudeCooldown <= 0f && lift != null && lift.EffectivePower > 0;
            low.interactable = canChangeAltitude && ship.altitude != AltitudeBand.Low;
            medium.interactable = canChangeAltitude && ship.altitude != AltitudeBand.Medium;
            high.interactable = canChangeAltitude && ship.altitude != AltitudeBand.High;

            var supportText = l10n.T("ui.support_call");
            if (state.convoy.supportCooldown > 0) supportText += "\n" + l10n.T("ui.cooldown", state.convoy.supportCooldown.ToString());
            var support = ui.Button("Support", panel, supportText, controller.UseSupport,
                new Vector2(18f, 142f), new Vector2(264f, 58f), new Color(0.18f, 0.28f, 0.46f, 0.98f), UiFactory.TextPrimary, 15);
            support.interactable = state.convoy.supportCooldown <= 0;
            if (state.resources.ordnance <= 0)
            {
                ui.Button("EmergencyOrdnance", panel, "[R] " + l10n.T("ui.emergency_ordnance"), controller.EmergencyOrdnance,
                    new Vector2(18f, 84f), new Vector2(264f, 46f), UiFactory.Danger, UiFactory.TextPrimary, 13);
                AddCombatLog(panel, new Vector2(18f, 10f), new Vector2(264f, 64f));
            }
            else AddCombatLog(panel, new Vector2(18f, 10f), new Vector2(264f, 120f));
        }

        private void BuildEnemyPanel()
        {
            var state = controller.Simulation.State;
            var ship = state.enemyShip;
            var panel = ui.PanelRect("EnemyShip", ui.Root, new Vector2(1256f, 270f), new Vector2(644f, 640f), PanelColor);
            ui.Text("EnemyTitle", panel, l10n.T("ui.enemy"), 18, UiFactory.Danger, TextAnchor.MiddleLeft,
                new Vector2(18f, 596f), new Vector2(608f, 36f), FontStyle.Bold);
            var shipName = string.IsNullOrEmpty(ship.nameKey) ? ship.displayName : l10n.T(ship.nameKey);
            ui.Text("EnemyName", panel, shipName, 20, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(18f, 560f), new Vector2(300f, 36f), FontStyle.Bold);
            AddDefenseBar(panel, "EnemyWard", l10n.T("ui.ward"), ship.ward, ship.maxWard, 318f, 580f, UiFactory.Aether);
            AddDefenseBar(panel, "EnemyArmor", l10n.T("ui.armor"), ship.armor, ship.maxArmor, 318f, 556f, UiFactory.Brass);
            AddDefenseBar(panel, "EnemyHull", l10n.T("ui.hull"), ship.hull, ship.maxHull, 472f, 580f, UiFactory.Danger);
            var target = ship.GetSystem(selectedEnemySystem);
            ui.Text("EnemyTargetHint", panel, l10n.T("ui.mission_target", target != null ? l10n.T(target.displayKey) : string.Empty), 12, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(18f, 10f), new Vector2(608f, 20f), FontStyle.Bold);

            ShipBlueprintView.Draw(ui, l10n, panel, ship, ContentCatalog.GetDeckPlan(ship.id), new Vector2(16f, 32f), new Vector2(612f, 512f), new BlueprintOptions
            {
                roomNamePrefix = "EnemySystem_",
                selectedSystem = selectedEnemySystem,
                onRoomClick = system => { selectedEnemySystem = system; controller.Simulation.State.selectedEnemySystem = system; ShowCombat(); },
                crew = null,
                reducedMotion = controller.Profile.accessibility.reducedMotion,
                highContrast = controller.Profile.accessibility.highContrast,
                showAllocatedPower = false
            });
        }

        private void BuildSquadronPanel()
        {
            var state = controller.Simulation.State;
            var panel = ui.PanelRect("SquadronPanel", ui.Root, new Vector2(20f, 20f), new Vector2(1880f, 232f), PanelColor);
            ui.Text("SquadronTitle", panel, l10n.T("ui.squadrons"), 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(18f, 184f), new Vector2(300f, 36f), FontStyle.Bold);
            ui.Text("InterceptCount", panel, $"{l10n.T("ui.intercept")}: {state.interceptCharges}", 17, UiFactory.Aether, TextAnchor.MiddleRight,
                new Vector2(1540f, 184f), new Vector2(310f, 36f));
            var tutorialHint = CombatTutorialHint(state);
            if (!string.IsNullOrEmpty(tutorialHint))
            {
                ui.Text("TutorialHint", panel, tutorialHint, 16, UiFactory.Aether, TextAnchor.MiddleCenter,
                    new Vector2(360f, 184f), new Vector2(1120f, 36f), FontStyle.Bold);
            }

            for (var i = 0; i < state.squadrons.Count; i++)
            {
                var squadron = state.squadrons[i];
                var y = 100f - i * 82f;
                var status = l10n.EnumName(squadron.status);
                var missionName = squadron.mission == SquadronMission.None ? string.Empty : " · " + l10n.EnumName(squadron.mission);
                var targetName = squadron.mission == SquadronMission.Bombard || squadron.mission == SquadronMission.Assault
                    ? " · " + l10n.T("ui.mission_target", l10n.T(state.enemyShip.GetSystem(squadron.targetSystem).displayKey)) : string.Empty;
                var label = $"{l10n.T(squadron.displayKey)}\n{status}{missionName}{targetName} · {squadron.strength}/{squadron.maxStrength}";
                ui.Text("SquadLabel_" + squadron.id, panel, label, 16, squadron.status == SquadronStatus.Destroyed ? UiFactory.Danger : UiFactory.TextPrimary,
                    TextAnchor.MiddleLeft, new Vector2(18f, y), new Vector2(320f, 70f), FontStyle.Bold);
                var missions = new[] { SquadronMission.Intercept, SquadronMission.Bombard, SquadronMission.Escort, SquadronMission.Recon, SquadronMission.Assault };
                var keys = new[] { "ui.intercept", "ui.bombard", "ui.escort", "ui.recon", "ui.assault" };
                for (var m = 0; m < missions.Length; m++)
                {
                    var mission = missions[m];
                    var localSquad = squadron.id;
                    var shortcut = mission == SquadronMission.Bombard && i < 2 ? $"[{i + 1}] " : string.Empty;
                    var button = ui.Button($"{squadron.id}_{mission}", panel, shortcut + l10n.T(keys[m]), () => controller.LaunchSquadron(localSquad, mission, selectedEnemySystem),
                        new Vector2(350f + m * 244f, y + 8f), new Vector2(224f, 54f), m == 1 ? new Color(0.43f, 0.18f, 0.11f, 0.98f) : UiFactory.PanelSoft,
                        UiFactory.TextPrimary, 15);
                    var deck = state.playerShip.GetSystem(ShipSystemType.FlightDeck);
                    button.interactable = squadron.CanLaunch && state.resources.ordnance >= squadron.ordnanceCost && deck != null && deck.EffectivePower > 0;
                }
                var progress = squadron.status == SquadronStatus.Ready ? 1f : squadron.status == SquadronStatus.Destroyed ? 0f :
                    squadron.phaseDuration <= 0f ? 0f : 1f - squadron.missionTimer / squadron.phaseDuration;
                var progressColor = squadron.status == SquadronStatus.Recovering ? UiFactory.Success : squadron.status == SquadronStatus.Destroyed ? UiFactory.Danger : UiFactory.Aether;
                ui.Bar("SquadProgress_" + squadron.id, panel, progress, new Vector2(350f, y + 1f), new Vector2(1200f, 5f), progressColor);
            }
        }

        private void AddDefenseBar(Transform parent, string id, string label, float value, float max, float x, float y, Color color)
        {
            ui.Text(id + "Text", parent, $"{label} {value:0}/{max:0}", 12, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(x, y), new Vector2(170f, 18f), FontStyle.Bold);
            ui.Bar(id + "Bar", parent, max <= 0f ? 0f : value / max, new Vector2(x, y - 5f), new Vector2(160f, 6f), color);
        }

        private float NextThreat(RunState state)
        {
            return (float)Math.Min(state.enemyWeaponCooldown, Math.Min(state.enemySquadronCooldown, state.weatherHazardTimer));
        }

        private Color AlertColor(AlertSeverity severity)
        {
            return severity == AlertSeverity.Critical ? UiFactory.Danger : severity == AlertSeverity.Warning ? UiFactory.Brass : UiFactory.Aether;
        }

        private string FormatAlert()
        {
            var state = controller.Simulation.State;
            var argument = state.combatAlertArgument;
            if (Enum.TryParse(argument, out ShipSystemType system))
            {
                var systemState = state.playerShip.GetSystem(system) ?? state.enemyShip?.GetSystem(system);
                if (systemState != null) argument = l10n.T(systemState.displayKey);
            }
            else if (!string.IsNullOrEmpty(argument) && argument.StartsWith("squadron.", StringComparison.Ordinal)) argument = l10n.T(argument);
            var alert = l10n.T(state.combatAlertKey, argument);
            return state.combatAlertPausedBattle ? l10n.T("ui.paused_by_warning") + "\n" + alert : alert;
        }

        private void AddCombatLog(Transform parent, Vector2 position, Vector2 size)
        {
            var logs = controller.Simulation.State.combatLog;
            var builder = new StringBuilder();
            var start = Math.Max(0, logs.Count - 4);
            for (var i = start; i < logs.Count; i++)
            {
                if (builder.Length > 0) builder.Append('\n');
                builder.Append("› ").Append(FormatLog(logs[i]));
            }
            ui.Text("CombatLog", parent, builder.ToString(), 12, UiFactory.TextMuted, TextAnchor.LowerLeft, position, size);
        }

        private string CombatTutorialHint(RunState state)
        {
            if (!state.hasFiredWeapon) return l10n.T("tutorial.fire");
            if (!state.hasLaunchedSquadron) return l10n.T("tutorial.squadron");
            var damagedRoom = state.playerShip.rooms.Exists(room => room.fire > 1f || room.breach > 1f || state.playerShip.GetSystem(room.system).damage > 1f);
            if (damagedRoom && !state.hasMovedCrew) return l10n.T("tutorial.crew");
            if (!state.hasChangedPower) return l10n.T("tutorial.power");
            return string.Empty;
        }

        private void AddLastReport(Vector2 position, Vector2 size)
        {
            var logs = controller.Simulation.State.combatLog;
            var text = logs.Count > 0 ? FormatLog(logs[logs.Count - 1]) : string.Empty;
            if (!string.IsNullOrEmpty(controller.LastCommandMessage) && controller.LastCommandMessage != "command.ok")
                text = l10n.T(controller.LastCommandMessage) + (string.IsNullOrEmpty(text) ? "" : "\n" + text);
            ui.Text("LastReport", ui.Root, l10n.T("ui.last_report") + "\n" + text, 14, UiFactory.TextMuted, TextAnchor.MiddleLeft, position, size);
        }

        private string FormatLog(CombatLogEntry entry)
        {
            var argument = entry.argument;
            if (Enum.TryParse(argument, out ShipSystemType system))
            {
                var state = controller.Simulation.State.playerShip.GetSystem(system) ?? controller.Simulation.State.enemyShip?.GetSystem(system);
                if (state != null) argument = l10n.T(state.displayKey);
            }
            else if (Enum.TryParse(argument, out AltitudeBand altitude)) argument = l10n.EnumName(altitude);
            else if (!string.IsNullOrEmpty(argument) && argument.StartsWith("squadron.", StringComparison.Ordinal)) argument = l10n.T(argument);
            return l10n.T(entry.key, argument);
        }

        private void ShowEnding(bool victory)
        {
            ui.Clear();
            ui.Background(controller.Background, victory ? new Color(0.02f, 0.08f, 0.09f, 0.56f) : new Color(0.08f, 0.01f, 0.04f, 0.72f));
            var state = controller.Simulation.State;
            var panel = ui.PanelRect("EndingPanel", ui.Root, new Vector2(410f, 230f), new Vector2(1100f, 620f), PanelColor);
            ui.Text("EndingTitle", panel, victory ? l10n.T("ui.victory_title") : l10n.T("ui.defeat_title"), 48,
                victory ? UiFactory.Aether : UiFactory.Danger, TextAnchor.MiddleCenter, new Vector2(80f, 470f), new Vector2(940f, 90f), FontStyle.Bold);
            var body = victory ? l10n.T("ui.victory_body") : l10n.EnumName(state.defeatReason);
            ui.Text("EndingBody", panel, body, 25, UiFactory.TextPrimary, TextAnchor.MiddleCenter,
                new Vector2(110f, 260f), new Vector2(880f, 190f));
            var summary = $"{l10n.T("ui.survivors")} {state.convoy.survivors:N0}    {l10n.T("ui.morale")} {state.convoy.morale}%    {l10n.T("ui.salvage")} {state.resources.salvage}";
            ui.Text("Summary", panel, summary, 21, UiFactory.Brass, TextAnchor.MiddleCenter,
                new Vector2(110f, 190f), new Vector2(880f, 60f), FontStyle.Bold);
            ui.Button("NewExpedition", panel, l10n.T("ui.new_expedition"), controller.ShowSetup,
                new Vector2(170f, 70f), new Vector2(760f, 66f), victory ? UiFactory.Aether : UiFactory.Brass, UiFactory.Ink, 22);
        }

        private string L(string korean, string english)
        {
            return l10n.Language == Language.Korean ? korean : english;
        }

        private string On(bool value)
        {
            if (l10n.Language == Language.Korean) return value ? "켜짐" : "꺼짐";
            return value ? "ON" : "OFF";
        }
    }
}
