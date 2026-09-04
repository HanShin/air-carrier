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
        private string selectedRouteNodeId;
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
                new Vector2(52f, 262f), new Vector2(260f, 40f), FontStyle.Bold);
            ui.Button("Difficulty", left, "◀  " + l10n.EnumName(controller.Profile.difficulty) + "  ▶", controller.CycleDifficulty,
                new Vector2(52f, 210f), new Vector2(656f, 50f));

            var flagshipId = UnlockRules.ResolveFlagship(controller.Profile);
            var flagship = ContentCatalog.GetFlagship(flagshipId);
            var unlocked = UnlockRules.UnlockedFlagships(controller.Profile);
            ui.Text("FlagshipLabel", left, l10n.T("setup.flagship"), 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(52f, 168f), new Vector2(260f, 36f), FontStyle.Bold);
            var flagshipButton = ui.Button("Flagship", left, (unlocked.Count > 1 ? "◀  " : "") + l10n.T(flagship.nameKey) + (unlocked.Count > 1 ? "  ▶" : ""), controller.CycleFlagship,
                new Vector2(52f, 120f), new Vector2(656f, 46f));
            flagshipButton.interactable = unlocked.Count > 1 && controller.Profile.tutorialSeen;
            var lockHint = !controller.Profile.tutorialSeen ? l10n.T("setup.flagship_locked", l10n.T("unlock.tutorial"))
                : unlocked.Count < ContentCatalog.FlagshipIds().Count ? l10n.T("setup.flagship_locked", l10n.T("unlock.campaign")) : string.Empty;
            ui.Text("FlagshipDesc", left, l10n.T(flagship.descriptionKey) + (lockHint.Length > 0 ? "\n" + lockHint : string.Empty), 13, UiFactory.TextMuted, TextAnchor.UpperLeft,
                new Vector2(52f, 62f), new Vector2(656f, 54f));
            ui.Text("Warning", left, l10n.T("setup.warning"), 14, UiFactory.Danger, TextAnchor.MiddleCenter,
                new Vector2(52f, 22f), new Vector2(656f, 36f), FontStyle.Bold);

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
                case GamePhase.Port: ShowPort(); break;
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

        private const float MapWidth = 1300f;
        private const float MapHeight = 590f;

        private static Vector2 NodeCenter(RouteNodeState node)
        {
            return new Vector2(110f + node.column * 154f, 470f - node.lane * 180f);
        }

        private static float ColumnBoundary(int column)
        {
            return column < 0 ? 0f : 110f + column * 154f + 77f;
        }

        public void ConfirmRouteSelection()
        {
            var simulation = controller.Simulation;
            if (simulation == null || simulation.State.phase != GamePhase.RouteMap) return;
            var node = simulation.State.routeNodes.Find(item => item.id == selectedRouteNodeId);
            if (node != null && simulation.CanTravelTo(node)) controller.Travel(node.id);
        }

        private void SelectRouteNode(string nodeId)
        {
            if (selectedRouteNodeId == nodeId)
            {
                ConfirmRouteSelection();
                return;
            }
            selectedRouteNodeId = nodeId;
            ShowRoute();
        }

        private Color NodeFill(RouteNodeState node, bool current, bool available)
        {
            if (current) return UiFactory.Brass;
            if (node.blocked) return new Color(0.34f, 0.08f, 0.15f, 0.98f);
            if (available) return new Color(0.08f, 0.38f, 0.42f, 0.98f);
            if (node.visited) return new Color(0.2f, 0.22f, 0.26f, 0.98f);
            return new Color(0.07f, 0.1f, 0.15f, 0.98f);
        }

        private void ShowRoute()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.02f, 0.03f, 0.07f, 0.58f));
            AddStatusBar();
            var state = controller.Simulation.State;
            if (!string.IsNullOrEmpty(selectedRouteNodeId) && !state.routeNodes.Exists(node => node.id == selectedRouteNodeId && !node.blocked))
                selectedRouteNodeId = null;

            var region = ContentCatalog.GetRegion(state.regionIndex);
            var regionLabel = state.regionCount > 1 ? $"{l10n.T("ui.region", $"{state.regionIndex}/{state.regionCount}")} — {l10n.T(region.nameKey)}" : l10n.T(region.nameKey);
            ui.Text("RouteTitle", ui.Root, l10n.T("ui.route_title"), 34, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(84f, 900f), new Vector2(700f, 56f), FontStyle.Bold);
            ui.Text("RegionTitle", ui.Root, regionLabel, 20, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(790f, 900f), new Vector2(700f, 56f), FontStyle.Bold);
            ui.Text("RouteHint", ui.Root, l10n.T("ui.route_select_hint") + "  " + l10n.T("ui.route_hint"), 15, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(84f, 862f), new Vector2(1500f, 34f));

            var map = ui.PanelRect("RoutePanel", ui.Root, new Vector2(70f, 250f), new Vector2(MapWidth, MapHeight), PanelColor);
            DrawStormFront(map, state);

            var centers = new Dictionary<string, Vector2>();
            for (var i = 0; i < state.routeNodes.Count; i++) centers[state.routeNodes[i].id] = NodeCenter(state.routeNodes[i]);

            for (var i = 0; i < state.routeNodes.Count; i++)
            {
                var node = state.routeNodes[i];
                var isCurrent = node.id == state.currentNodeId;
                for (var j = 0; j < node.connectedIds.Count; j++)
                {
                    var target = state.routeNodes.Find(item => item.id == node.connectedIds[j]);
                    if (target == null || !centers.ContainsKey(target.id)) continue;
                    var highlighted = isCurrent && controller.Simulation.CanTravelTo(target);
                    var color = highlighted ? new Color(0.92f, 0.68f, 0.27f, 0.9f)
                        : node.blocked || target.blocked ? new Color(0.45f, 0.1f, 0.18f, 0.45f)
                        : new Color(0.35f, 0.56f, 0.62f, 0.35f);
                    ui.Line(map, centers[node.id], centers[target.id], highlighted ? 3f : 2f, color);
                }
            }

            var availableNumber = 0;
            for (var i = 0; i < state.routeNodes.Count; i++)
            {
                var node = state.routeNodes[i];
                var available = controller.Simulation.CanTravelTo(node);
                var current = node.id == state.currentNodeId;
                var selected = node.id == selectedRouteNodeId;
                var center = centers[node.id];
                var diameter = node.encounterType == EncounterType.Gate ? 76f : 64f;
                var half = diameter / 2f;

                if (selected) ui.Circle("SelectRing_" + node.id, map, center - new Vector2(half + 6f, half + 6f), new Vector2(diameter + 12f, diameter + 12f), Color.white).raycastTarget = false;
                var ringColor = current ? Color.white : available ? UiFactory.Brass : node.encounterType == EncounterType.Gate ? UiFactory.Brass
                    : node.encounterType == EncounterType.EliteBattle ? UiFactory.Danger : new Color(0.2f, 0.26f, 0.32f, 0.9f);
                ui.Circle("Ring_" + node.id, map, center - new Vector2(half + 3f, half + 3f), new Vector2(diameter + 6f, diameter + 6f), ringColor).raycastTarget = false;
                if (node.encounterType == EncounterType.EliteBattle || node.encounterType == EncounterType.Gate)
                {
                    ui.Circle("RingGap_" + node.id, map, center - new Vector2(half, half), new Vector2(diameter, diameter), UiFactory.Ink).raycastTarget = false;
                    ui.Circle("Ring2_" + node.id, map, center - new Vector2(half - 3f, half - 3f), new Vector2(diameter - 6f, diameter - 6f), ringColor).raycastTarget = false;
                }
                var inner = node.encounterType == EncounterType.EliteBattle || node.encounterType == EncounterType.Gate ? diameter - 12f : diameter;
                var localNode = node;
                var button = ui.CircleButton("Node_" + node.id, map, center - new Vector2(inner / 2f, inner / 2f), new Vector2(inner, inner),
                    NodeFill(node, current, available), () => SelectRouteNode(localNode.id));
                button.interactable = !node.blocked;

                var glyphColor = current ? UiFactory.Ink : node.encounterType == EncounterType.Gate ? UiFactory.Brass
                    : RouteRules.IsHostile(node.encounterType) ? UiFactory.Danger : UiFactory.TextPrimary;
                if (node.blocked || node.visited && !current) glyphColor = new Color(glyphColor.r, glyphColor.g, glyphColor.b, 0.55f);
                ui.Text("Glyph_" + node.id, map, RouteRules.Glyph(node.encounterType), 26, glyphColor, TextAnchor.MiddleCenter,
                    center - new Vector2(half, half), new Vector2(diameter, diameter), FontStyle.Bold);

                if (available)
                {
                    var badgePosition = center + new Vector2(half - 14f, half - 14f);
                    ui.Circle("Badge_" + node.id, map, badgePosition, new Vector2(24f, 24f), UiFactory.Brass).raycastTarget = false;
                    ui.Text("BadgeText_" + node.id, map, (++availableNumber).ToString(), 13, UiFactory.Ink, TextAnchor.MiddleCenter,
                        badgePosition, new Vector2(24f, 24f), FontStyle.Bold);
                }

                var nameColor = current ? UiFactory.Brass : node.blocked ? new Color(0.7f, 0.35f, 0.4f, 1f) : node.visited ? UiFactory.TextMuted : UiFactory.TextPrimary;
                ui.Text("NodeName_" + node.id, map, l10n.T(node.nameKey), 13, nameColor, TextAnchor.MiddleCenter,
                    center - new Vector2(80f, half + 24f), new Vector2(160f, 20f), FontStyle.Bold);
                var detail = current ? l10n.T("ui.current") : node.blocked ? l10n.T("ui.blocked") : node.visited ? l10n.T("ui.visited")
                    : $"{l10n.T("ui.cost", node.aetherCost.ToString())} · {l10n.T(ContentCatalog.GetWeather(node.weather).nameKey)}";
                ui.Text("NodeDetail_" + node.id, map, detail, 11, current ? UiFactory.Brass : UiFactory.TextMuted, TextAnchor.MiddleCenter,
                    center - new Vector2(80f, half + 42f), new Vector2(160f, 18f));
            }

            DrawRouteLegend();
            DrawRoutePreview(state);

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

        private void DrawStormFront(RectTransform map, RunState state)
        {
            var stormEdge = ColumnBoundary(state.stormColumn);
            var nextColumn = RouteRules.NextStormColumn(state);
            var nextEdge = ColumnBoundary(nextColumn);
            if (nextColumn > state.stormColumn && nextEdge > stormEdge)
            {
                var warn = ui.PanelRect("StormNext", map, new Vector2(stormEdge, 0f), new Vector2(nextEdge - stormEdge, MapHeight), new Color(0.85f, 0.55f, 0.15f, 0.08f));
                warn.GetComponent<Image>().raycastTarget = false;
                ui.Text("StormNextLabel", map, l10n.T("ui.storm_next"), 12, new Color(0.95f, 0.7f, 0.3f, 0.9f), TextAnchor.MiddleCenter,
                    new Vector2(stormEdge, 6f), new Vector2(nextEdge - stormEdge, 20f), FontStyle.Bold);
            }
            if (state.stormColumn < 0) return;
            var band = ui.PanelRect("StormBand", map, Vector2.zero, new Vector2(stormEdge, MapHeight), new Color(0.55f, 0.06f, 0.14f, 0.3f));
            band.GetComponent<Image>().raycastTarget = false;
            var reduced = controller.Profile.accessibility.reducedMotion;
            for (var y = 14f; y < MapHeight; y += 34f)
                ui.Rotated("StormTooth", map, new Vector2(stormEdge, y), new Vector2(reduced ? 18f : 24f, reduced ? 18f : 24f), 45f, new Color(0.65f, 0.08f, 0.16f, 0.35f));
            ui.Text("StormLabel", map, l10n.T("ui.storm_front"), 15, UiFactory.Danger, TextAnchor.MiddleCenter,
                new Vector2(Math.Max(0f, stormEdge - 180f), MapHeight - 34f), new Vector2(Math.Min(180f, stormEdge), 26f), FontStyle.Bold);
        }

        private void DrawRouteLegend()
        {
            var types = new[] { EncounterType.Battle, EncounterType.EliteBattle, EncounterType.Rescue, EncounterType.Salvage,
                EncounterType.Trade, EncounterType.Checkpoint, EncounterType.Storm, EncounterType.Gate };
            ui.Text("LegendTitle", ui.Root, l10n.T("ui.legend"), 12, UiFactory.TextMuted, TextAnchor.MiddleLeft, new Vector2(84f, 208f), new Vector2(60f, 30f), FontStyle.Bold);
            for (var i = 0; i < types.Length; i++)
            {
                var x = 150f + i * 150f;
                var color = RouteRules.IsHostile(types[i]) ? UiFactory.Danger : types[i] == EncounterType.Gate ? UiFactory.Brass : UiFactory.TextPrimary;
                ui.Circle("LegendDot_" + i, ui.Root, new Vector2(x, 210f), new Vector2(26f, 26f), new Color(0.08f, 0.16f, 0.22f, 0.98f)).raycastTarget = false;
                ui.Text("LegendGlyph_" + i, ui.Root, RouteRules.Glyph(types[i]), 14, color, TextAnchor.MiddleCenter, new Vector2(x, 210f), new Vector2(26f, 26f), FontStyle.Bold);
                ui.Text("LegendName_" + i, ui.Root, l10n.T(RouteRules.NameKey(types[i])), 12, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                    new Vector2(x + 32f, 208f), new Vector2(116f, 30f));
            }
        }

        private void DrawRoutePreview(RunState state)
        {
            var panel = ui.PanelRect("RoutePreview", ui.Root, new Vector2(1390f, 250f), new Vector2(460f, MapHeight), PanelColor);
            var node = state.routeNodes.Find(item => item.id == selectedRouteNodeId);
            var current = controller.Simulation.CurrentNode;
            var currentName = current != null ? l10n.T(current.nameKey) : string.Empty;
            ui.Text("PreviewCurrent", panel, $"{l10n.T("ui.current")}: {currentName}   ·   {l10n.T("ui.aether")} {state.resources.aether}", 13, UiFactory.TextMuted,
                TextAnchor.MiddleLeft, new Vector2(20f, 14f), new Vector2(420f, 24f));
            if (node == null)
            {
                ui.Text("PreviewEmpty", panel, l10n.T("ui.route_preview_empty"), 22, UiFactory.TextMuted, TextAnchor.MiddleCenter,
                    new Vector2(20f, 330f), new Vector2(420f, 50f), FontStyle.Bold);
                ui.Text("PreviewHint", panel, l10n.T("ui.route_select_hint"), 15, UiFactory.TextMuted, TextAnchor.UpperCenter,
                    new Vector2(30f, 250f), new Vector2(400f, 70f));
                var installed = new StringBuilder();
                for (var i = 0; i < state.installedModules.Count; i++)
                {
                    var module = ContentCatalog.GetModule(state.installedModules[i]);
                    if (module == null) continue;
                    if (installed.Length > 0) installed.Append('\n');
                    installed.Append("◆ ").Append(l10n.T(module.nameKey));
                }
                ui.Text("PreviewModules", panel, l10n.T("ui.port_installed", state.installedModules.Count.ToString()) + "\n" + (installed.Length > 0 ? installed.ToString() : l10n.T("ui.port_none")),
                    13, UiFactory.TextMuted, TextAnchor.UpperLeft, new Vector2(24f, 60f), new Vector2(412f, 150f));
                return;
            }

            var available = controller.Simulation.CanTravelTo(node);
            var isCurrent = node.id == state.currentNodeId;
            var fill = NodeFill(node, isCurrent, available);
            ui.Circle("PreviewRing", panel, new Vector2(20f, MapHeight - 92f), new Vector2(72f, 72f), available ? UiFactory.Brass : new Color(0.2f, 0.26f, 0.32f, 0.9f)).raycastTarget = false;
            ui.Circle("PreviewFill", panel, new Vector2(24f, MapHeight - 88f), new Vector2(64f, 64f), fill).raycastTarget = false;
            ui.Text("PreviewGlyph", panel, RouteRules.Glyph(node.encounterType), 28, isCurrent ? UiFactory.Ink : RouteRules.IsHostile(node.encounterType) ? UiFactory.Danger : UiFactory.TextPrimary,
                TextAnchor.MiddleCenter, new Vector2(24f, MapHeight - 88f), new Vector2(64f, 64f), FontStyle.Bold);
            ui.Text("PreviewName", panel, l10n.T(node.nameKey), 24, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(108f, MapHeight - 70f), new Vector2(340f, 40f), FontStyle.Bold);
            var status = isCurrent ? l10n.T("ui.current") : node.visited ? l10n.T("ui.visited") : available ? L("이동 가능", "Reachable")
                : state.resources.aether < node.aetherCost && current != null && current.connectedIds.Contains(node.id) ? l10n.T("ui.unaffordable")
                : L("현재 위치에서 연결되지 않음", "Not connected from here");
            ui.Text("PreviewStatus", panel, status, 14, available ? UiFactory.Success : UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(108f, MapHeight - 98f), new Vector2(340f, 26f), FontStyle.Bold);

            var weather = ContentCatalog.GetWeather(node.weather);
            var accuracy = weather.accuracyModifier >= 0f ? $"+{weather.accuracyModifier * 100f:0}%" : $"{weather.accuracyModifier * 100f:0}%";
            var lines = new[]
            {
                (l10n.T("ui.aether"), $"{node.aetherCost} / {state.resources.aether}", state.resources.aether < node.aetherCost ? UiFactory.Danger : UiFactory.TextPrimary),
                (l10n.T("ui.weather"), $"{l10n.T(weather.nameKey)}  ·  {l10n.T("ui.accuracy_mod", accuracy)}  ·  {l10n.T("ui.ward_mod", weather.wardRegenModifier.ToString("0.0"))}", UiFactory.TextPrimary),
                (l10n.T("ui.altitude"), l10n.T("ui.recommended") + " " + l10n.EnumName(node.recommendedAltitude)
                    + (node.recommendedAltitude != state.playerShip.altitude ? $"   ({l10n.T("ui.current")}: {l10n.EnumName(state.playerShip.altitude)})" : string.Empty),
                    node.recommendedAltitude != state.playerShip.altitude ? UiFactory.Brass : UiFactory.TextPrimary)
            };
            for (var i = 0; i < lines.Length; i++)
            {
                var y = MapHeight - 150f - i * 58f;
                ui.Text("PreviewLabel_" + i, panel, lines[i].Item1, 12, UiFactory.TextMuted, TextAnchor.MiddleLeft, new Vector2(24f, y + 22f), new Vector2(400f, 20f), FontStyle.Bold);
                ui.Text("PreviewValue_" + i, panel, lines[i].Item2, 15, lines[i].Item3, TextAnchor.MiddleLeft, new Vector2(24f, y - 4f), new Vector2(416f, 26f));
            }

            var threatKey = node.encounterType == EncounterType.Gate ? "ui.threat_gate" : node.encounterType == EncounterType.EliteBattle ? "ui.threat_elite"
                : node.encounterType == EncounterType.Battle ? "ui.threat_battle" : "ui.threat_safe";
            var threatBox = ui.PanelRect("PreviewThreat", panel, new Vector2(20f, 116f), new Vector2(420f, 120f),
                RouteRules.IsHostile(node.encounterType) ? new Color(0.32f, 0.08f, 0.1f, 0.85f) : new Color(0.06f, 0.16f, 0.18f, 0.85f));
            threatBox.GetComponent<Image>().raycastTarget = false;
            ui.Text("PreviewThreatText", threatBox, l10n.T(threatKey), 15, RouteRules.IsHostile(node.encounterType) ? UiFactory.Danger : UiFactory.Aether,
                TextAnchor.MiddleLeft, new Vector2(18f, 10f), new Vector2(384f, 100f), FontStyle.Bold);

            var depart = ui.Button("Depart", panel, "[Enter] " + l10n.T("ui.depart"), ConfirmRouteSelection,
                new Vector2(20f, 48f), new Vector2(420f, 58f), available ? UiFactory.Brass : UiFactory.PanelSoft, available ? UiFactory.Ink : UiFactory.TextMuted, 19);
            depart.interactable = available;
        }

        public void ConfirmPort()
        {
            var simulation = controller.Simulation;
            if (simulation != null && simulation.State.phase == GamePhase.Port) controller.DepartPort();
        }

        private void ShowPort()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.02f, 0.05f, 0.08f, 0.66f));
            AddStatusBar();
            var state = controller.Simulation.State;
            var region = ContentCatalog.GetRegion(state.regionIndex);
            var panel = ui.PanelRect("PortPanel", ui.Root, new Vector2(160f, 120f), new Vector2(1600f, 800f), PanelColor);
            ui.Text("PortTitle", panel, l10n.T("ui.port_title") + "  —  " + l10n.T("ui.region", $"{state.regionIndex}/{state.regionCount}") + " · " + l10n.T(region.nameKey), 32,
                UiFactory.Brass, TextAnchor.MiddleLeft, new Vector2(50f, 720f), new Vector2(1500f, 56f), FontStyle.Bold);
            ui.Text("PortBody", panel, l10n.T("ui.port_body"), 18, UiFactory.TextPrimary, TextAnchor.UpperLeft,
                new Vector2(50f, 640f), new Vector2(1500f, 70f));
            ui.Text("PortSalvage", panel, $"{l10n.T("ui.salvage")} {state.resources.salvage}   ·   {l10n.T("ui.port_installed", $"{state.installedModules.Count}/{state.playerShip.moduleSlots}")}", 20,
                UiFactory.Aether, TextAnchor.MiddleLeft, new Vector2(50f, 590f), new Vector2(1500f, 40f), FontStyle.Bold);

            ui.Text("OffersTitle", panel, l10n.T("ui.port_offers"), 18, UiFactory.Brass, TextAnchor.MiddleLeft, new Vector2(50f, 540f), new Vector2(600f, 36f), FontStyle.Bold);
            var offers = controller.Simulation.PortOffers();
            var slotsFull = state.installedModules.Count >= state.playerShip.moduleSlots;
            for (var i = 0; i < offers.Count; i++)
            {
                var module = ContentCatalog.GetModule(offers[i]);
                if (module == null) continue;
                var x = 50f + i * 500f;
                var card = ui.PanelRect("Offer_" + module.id, panel, new Vector2(x, 260f), new Vector2(480f, 270f), UiFactory.PanelSoft);
                card.GetComponent<Image>().raycastTarget = false;
                ui.Outline("OfferEdge_" + module.id, panel, new Vector2(x, 260f), new Vector2(480f, 270f), 1f, new Color(0.3f, 0.36f, 0.42f, 0.9f));
                ui.Text("OfferName_" + module.id, card, l10n.T(module.nameKey), 22, UiFactory.TextPrimary, TextAnchor.MiddleLeft, new Vector2(20f, 214f), new Vector2(440f, 40f), FontStyle.Bold);
                ui.Text("OfferMeta_" + module.id, card, $"{l10n.EnumName(module.category)}  ·  {l10n.T("ui.tier", module.tier.ToString())}", 14, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                    new Vector2(20f, 184f), new Vector2(440f, 26f));
                ui.Text("OfferDesc_" + module.id, card, l10n.T(module.descriptionKey), 16, UiFactory.Aether, TextAnchor.UpperLeft, new Vector2(20f, 90f), new Vector2(440f, 90f));
                var affordable = state.resources.salvage >= module.cost;
                var localId = module.id;
                var buy = ui.Button("Buy_" + module.id, card, $"{l10n.T("ui.buy")}  ·  {l10n.T("ui.salvage")} {module.cost}", () => controller.PurchaseModule(localId),
                    new Vector2(20f, 20f), new Vector2(440f, 54f), affordable && !slotsFull ? UiFactory.Brass : UiFactory.PanelSoft, affordable && !slotsFull ? UiFactory.Ink : UiFactory.TextMuted, 17);
                buy.interactable = affordable && !slotsFull;
                if (slotsFull) ui.Text("OfferFull_" + module.id, card, l10n.T("ui.slots_full"), 13, UiFactory.Danger, TextAnchor.MiddleRight, new Vector2(20f, 184f), new Vector2(440f, 26f), FontStyle.Bold);
            }

            var list = new StringBuilder();
            for (var i = 0; i < state.installedModules.Count; i++)
            {
                var module = ContentCatalog.GetModule(state.installedModules[i]);
                if (module == null) continue;
                if (list.Length > 0) list.Append("     ");
                list.Append("◆ ").Append(l10n.T(module.nameKey));
            }
            ui.Text("InstalledList", panel, l10n.T("ui.port_installed", state.installedModules.Count.ToString()) + ": " + (list.Length > 0 ? list.ToString() : l10n.T("ui.port_none")), 13,
                UiFactory.TextMuted, TextAnchor.MiddleLeft, new Vector2(50f, 76f), new Vector2(1000f, 24f));

            ui.Text("PortWeaponsTitle", panel, l10n.T("ui.port_weapons"), 16, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(50f, 250f), new Vector2(400f, 30f), FontStyle.Bold);
            var weaponOffers = controller.Simulation.PortWeaponOffers();
            for (var i = 0; i < weaponOffers.Count && i < 2; i++)
            {
                var weapon = ContentCatalog.GetWeapon(weaponOffers[i]);
                if (weapon == null) continue;
                var x = 50f + i * 500f;
                var card = ui.PanelRect("WeaponOffer_" + weapon.id, panel, new Vector2(x, 128f), new Vector2(480f, 116f), UiFactory.PanelSoft);
                card.GetComponent<Image>().raycastTarget = false;
                ui.Text("WeaponOfferName_" + weapon.id, card, l10n.T(weapon.nameKey), 18, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                    new Vector2(18f, 80f), new Vector2(300f, 30f), FontStyle.Bold);
                ui.Text("WeaponOfferMeta_" + weapon.id, card, $"{l10n.EnumName(weapon.family)}  ·  {l10n.T("ui.tier", weapon.tier.ToString())}  ·  {l10n.T("ui.weapon_power", weapon.powerCost.ToString())}  ·  {weapon.damage:0.0} / {weapon.cooldown:0.0}s", 12,
                    UiFactory.TextMuted, TextAnchor.MiddleLeft, new Vector2(18f, 58f), new Vector2(440f, 22f));
                ui.Text("WeaponOfferDesc_" + weapon.id, card, l10n.T(weapon.descriptionKey), 13, UiFactory.Aether, TextAnchor.UpperLeft,
                    new Vector2(18f, 30f), new Vector2(440f, 28f));
                var slots = state.weaponSlots;
                var replaces = slots.Count >= state.playerShip.weaponHardpoints && slots.Count > 0 ? ContentCatalog.GetWeapon(slots[slots.Count - 1].weaponId) : null;
                var label = $"{l10n.T("ui.buy")}  ·  {l10n.T("ui.salvage")} {weapon.cost}" + (replaces != null ? $"   ({l10n.T("ui.replaces", l10n.T(replaces.nameKey))})" : string.Empty);
                var localId = weapon.id;
                var buy = ui.Button("BuyWeapon_" + weapon.id, card, label, () => controller.PurchaseWeapon(localId),
                    new Vector2(18f, 4f), new Vector2(444f, 24f), UiFactory.Brass, UiFactory.Ink, 12);
                buy.interactable = state.resources.salvage >= weapon.cost;
            }
            var wingOffers = controller.Simulation.PortWingOffers();
            if (wingOffers.Count > 0)
            {
                var wing = ContentCatalog.GetWing(wingOffers[0]);
                ui.Text("PortWingTitle", panel, l10n.T("ui.port_wings"), 16, UiFactory.Brass, TextAnchor.MiddleLeft,
                    new Vector2(1050f, 250f), new Vector2(400f, 30f), FontStyle.Bold);
                var card = ui.PanelRect("WingOffer_" + wing.id, panel, new Vector2(1050f, 128f), new Vector2(480f, 116f), UiFactory.PanelSoft);
                card.GetComponent<Image>().raycastTarget = false;
                ui.Text("WingOfferName_" + wing.id, card, l10n.T(wing.nameKey), 18, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                    new Vector2(18f, 80f), new Vector2(300f, 30f), FontStyle.Bold);
                ui.Text("WingOfferMeta_" + wing.id, card, $"{l10n.EnumName(wing.type)}  ·  {l10n.T("ui.tier", wing.tier.ToString())}  ·  {string.Format(l10n.T("ui.wing_meta"), wing.strength, wing.ordnanceCost)}", 12,
                    UiFactory.TextMuted, TextAnchor.MiddleLeft, new Vector2(18f, 58f), new Vector2(440f, 22f));
                ui.Text("WingOfferDesc_" + wing.id, card, l10n.T(wing.descriptionKey), 13, UiFactory.Aether, TextAnchor.UpperLeft,
                    new Vector2(18f, 30f), new Vector2(440f, 28f));
                var sameBay = state.squadrons.Find(sq => (ContentCatalog.GetWing(sq.wingId)?.type ?? sq.type) == wing.type);
                var replacedWing = sameBay != null ? ContentCatalog.GetWing(sameBay.wingId) : state.squadrons.Count >= state.playerShip.wingBays && state.squadrons.Count > 0 ? ContentCatalog.GetWing(state.squadrons[state.squadrons.Count - 1].wingId) : null;
                var wingLabel = $"{l10n.T("ui.buy")}  ·  {l10n.T("ui.salvage")} {wing.cost}" + (replacedWing != null ? $"   ({l10n.T("ui.replaces", l10n.T(replacedWing.nameKey))})" : string.Empty);
                var localWing = wing.id;
                var buyWing = ui.Button("BuyWing_" + wing.id, card, wingLabel, () => controller.PurchaseWing(localWing),
                    new Vector2(18f, 4f), new Vector2(444f, 24f), UiFactory.Brass, UiFactory.Ink, 12);
                buyWing.interactable = state.resources.salvage >= wing.cost;
            }
            var carried = new StringBuilder();
            for (var i = 0; i < state.squadrons.Count; i++)
            {
                if (carried.Length > 0) carried.Append("  ·  ");
                carried.Append(l10n.T(state.squadrons[i].displayKey)).Append(" ").Append(state.squadrons[i].maxStrength);
            }

            var mounted = new StringBuilder();
            for (var i = 0; i < state.weaponSlots.Count; i++)
            {
                var weapon = ContentCatalog.GetWeapon(state.weaponSlots[i].weaponId);
                if (mounted.Length > 0) mounted.Append("  ·  ");
                mounted.Append(weapon == null ? state.weaponSlots[i].weaponId : l10n.T(weapon.nameKey));
            }
            ui.Text("PortMounted", panel, $"{l10n.T("ui.weapons_title")}: {mounted}     ·     {l10n.T("ui.wing_bays")}: {carried}", 13, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(50f, 100f), new Vector2(1100f, 24f));

            var depart = ui.Button("DepartPort", panel, l10n.T("ui.port_depart"), ConfirmPort, new Vector2(1150f, 40f), new Vector2(400f, 70f), UiFactory.Brass, UiFactory.Ink, 20);
            depart.interactable = true;
            AddLastReport(new Vector2(210f, 130f), new Vector2(900f, 60f));
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

            var slot = 0;
            for (var i = 0; i < encounter.choices.Count; i++)
            {
                var choice = encounter.choices[i];
                if (choice.hidden) continue;
                var localChoice = choice;
                var label = $"[{slot + 1}] " + l10n.T(choice.textKey);
                if (choice.successChance < 1f) label += $"   ({l10n.T("ui.chance", Mathf.RoundToInt(choice.successChance * 100f).ToString())})";
                var button = ui.Button("Choice_" + choice.id, panel, label, () => controller.ChooseEncounter(localChoice.id),
                    new Vector2(70f, 292f - slot * 82f), new Vector2(1160f, 64f),
                    choice.successChance < 1f ? new Color(0.36f, 0.22f, 0.1f, 0.98f) : slot == 0 ? new Color(0.13f, 0.34f, 0.36f, 0.98f) : UiFactory.PanelSoft,
                    UiFactory.TextPrimary, 19);
                button.interactable = controller.Simulation.CanChoose(choice);
                slot++;
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
                new Vector2(18f, 8f), new Vector2(664f, 20f));
            AddRoomDetail(panel, "PlayerRoomDetail", ship, ship.GetSystem(selectedPlayerSystem), state.crew, true, new Vector2(16f, 30f), new Vector2(668f, 64f));

            ShipBlueprintView.Draw(ui, l10n, panel, ship, ContentCatalog.GetDeckPlan(ship.id), new Vector2(16f, 98f), new Vector2(668f, 450f), new BlueprintOptions
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
                new Vector2(18f, 410f), new Vector2(264f, 28f), UiFactory.Violet, UiFactory.TextPrimary, 13);
            overcharge.interactable = system.maxPower > 0 && system.overchargeSeconds <= 0f && resonatorPresent;

            var weapons = ship.GetSystem(ShipSystemType.Weapons);
            var anyReady = false;
            for (var slot = 0; slot < state.weaponSlots.Count && slot < 3; slot++)
            {
                var mount = state.weaponSlots[slot];
                var weapon = ContentCatalog.GetWeapon(mount.weaponId);
                var y = 344f - slot * 34f;
                var powered = controller.Simulation.IsWeaponPowered(slot);
                var hasOrdnance = weapon == null || weapon.ordnancePerShot <= state.resources.ordnance;
                var ready = powered && mount.cooldown <= 0f && hasOrdnance && weapon != null;
                anyReady |= ready;
                var status = weapon == null ? l10n.T("ui.empty_hardpoint") : !powered ? l10n.T("ui.unpowered_weapon") : !hasOrdnance ? l10n.T("ui.no_ordnance")
                    : mount.cooldown > 0f ? $"{mount.cooldown:0.0}s" : l10n.T("ui.fire");
                var name = weapon == null ? l10n.T("ui.empty_hardpoint") : l10n.T(weapon.nameKey);
                var localSlot = slot;
                var button = ui.Button("Weapon_" + slot, panel, $"{name}  ·  {status}", () => controller.FireSlot(localSlot, selectedEnemySystem),
                    new Vector2(18f, y), new Vector2(264f, 30f), ready ? new Color(0.48f, 0.18f, 0.12f, 0.98f) : powered ? UiFactory.PanelSoft : new Color(0.16f, 0.16f, 0.18f, 0.95f),
                    ready ? UiFactory.TextPrimary : UiFactory.TextMuted, 12);
                button.interactable = ready;
                if (weapon != null)
                {
                    var progress = weapon.cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(mount.cooldown / weapon.cooldown);
                    ui.Bar("WeaponCooldown_" + slot, panel, progress, new Vector2(22f, y + 1f), new Vector2(256f, 3f), powered ? UiFactory.Brass : UiFactory.TextMuted);
                }
            }
            var fire = ui.Button("Fire", panel, "[F] " + l10n.T("ui.fire_all"), () => controller.Fire(selectedEnemySystem),
                new Vector2(18f, 380f), new Vector2(264f, 26f), new Color(0.48f, 0.18f, 0.12f, 0.98f), UiFactory.TextPrimary, 13);
            fire.interactable = anyReady && weapons != null && weapons.EffectivePower > 0;
            ui.Text("AltitudeLabel", panel, l10n.T("ui.altitude"), 16, UiFactory.Brass, TextAnchor.MiddleCenter,
                new Vector2(18f, 250f), new Vector2(264f, 26f), FontStyle.Bold);
            var low = ui.Button("Low", panel, l10n.T("ui.low"), () => controller.ChangeAltitude(AltitudeBand.Low), new Vector2(18f, 210f), new Vector2(82f, 38f),
                state.playerShip.altitude == AltitudeBand.Low ? UiFactory.Aether : UiFactory.PanelSoft, state.playerShip.altitude == AltitudeBand.Low ? UiFactory.Ink : UiFactory.TextPrimary, 14);
            var medium = ui.Button("Medium", panel, l10n.T("ui.medium"), () => controller.ChangeAltitude(AltitudeBand.Medium), new Vector2(109f, 210f), new Vector2(82f, 38f),
                state.playerShip.altitude == AltitudeBand.Medium ? UiFactory.Aether : UiFactory.PanelSoft, state.playerShip.altitude == AltitudeBand.Medium ? UiFactory.Ink : UiFactory.TextPrimary, 14);
            var high = ui.Button("High", panel, l10n.T("ui.high"), () => controller.ChangeAltitude(AltitudeBand.High), new Vector2(200f, 210f), new Vector2(82f, 38f),
                state.playerShip.altitude == AltitudeBand.High ? UiFactory.Aether : UiFactory.PanelSoft, state.playerShip.altitude == AltitudeBand.High ? UiFactory.Ink : UiFactory.TextPrimary, 14);
            var lift = ship.GetSystem(ShipSystemType.LiftArray);
            var canChangeAltitude = state.altitudeCooldown <= 0f && lift != null && lift.EffectivePower > 0;
            low.interactable = canChangeAltitude && ship.altitude != AltitudeBand.Low;
            medium.interactable = canChangeAltitude && ship.altitude != AltitudeBand.Medium;
            high.interactable = canChangeAltitude && ship.altitude != AltitudeBand.High;

            var supportText = l10n.T("ui.support_call");
            if (state.convoy.supportCooldown > 0) supportText += "\n" + l10n.T("ui.cooldown", state.convoy.supportCooldown.ToString());
            var support = ui.Button("Support", panel, supportText, controller.UseSupport,
                new Vector2(18f, 150f), new Vector2(264f, 52f), new Color(0.18f, 0.28f, 0.46f, 0.98f), UiFactory.TextPrimary, 14);
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
                new Vector2(18f, 8f), new Vector2(608f, 20f), FontStyle.Bold);
            AddRoomDetail(panel, "EnemyRoomDetail", ship, target, null, false, new Vector2(16f, 30f), new Vector2(612f, 64f));

            ShipBlueprintView.Draw(ui, l10n, panel, ship, ContentCatalog.GetDeckPlan(ship.id), new Vector2(16f, 98f), new Vector2(612f, 446f), new BlueprintOptions
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

        private static string MissionGlyph(SquadronMission mission)
        {
            switch (mission)
            {
                case SquadronMission.Intercept: return "▲";
                case SquadronMission.Bombard: return "▼";
                case SquadronMission.Escort: return "◆";
                case SquadronMission.Recon: return "◇";
                case SquadronMission.Assault: return "■";
                default: return "●";
            }
        }

        private void BuildSquadronPanel()
        {
            var state = controller.Simulation.State;
            var panel = ui.PanelRect("SquadronPanel", ui.Root, new Vector2(20f, 20f), new Vector2(1880f, 232f), PanelColor);
            ui.Text("SquadronTitle", panel, l10n.T("ui.squadrons"), 18, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(18f, 190f), new Vector2(300f, 32f), FontStyle.Bold);
            ui.Text("InterceptCount", panel, $"▲ {l10n.T("ui.intercept")} {state.interceptCharges}   ·   {l10n.T("ui.ordnance")} {state.resources.ordnance}", 15, UiFactory.Aether, TextAnchor.MiddleRight,
                new Vector2(1400f, 190f), new Vector2(460f, 32f), FontStyle.Bold);
            var tutorialHint = CombatTutorialHint(state);
            if (!string.IsNullOrEmpty(tutorialHint))
            {
                ui.Text("TutorialHint", panel, tutorialHint, 15, UiFactory.Aether, TextAnchor.MiddleCenter,
                    new Vector2(340f, 190f), new Vector2(1040f, 32f), FontStyle.Bold);
            }

            var deck = state.playerShip.GetSystem(ShipSystemType.FlightDeck);
            var deckReady = deck != null && deck.EffectivePower > 0;
            var missions = new[] { SquadronMission.Intercept, SquadronMission.Bombard, SquadronMission.Escort, SquadronMission.Recon, SquadronMission.Assault };
            var keys = new[] { "ui.intercept", "ui.bombard", "ui.escort", "ui.recon", "ui.assault" };
            for (var i = 0; i < state.squadrons.Count && i < 2; i++)
            {
                var squadron = state.squadrons[i];
                var y = 104f - i * 86f;
                var destroyed = squadron.status == SquadronStatus.Destroyed;
                var busy = squadron.status == SquadronStatus.Launching || squadron.status == SquadronStatus.OnMission || squadron.status == SquadronStatus.Recovering;

                // Slot card: glyph, name, status line, strength pips, ordnance cost.
                var cardColor = destroyed ? new Color(0.22f, 0.08f, 0.1f, 0.95f) : busy ? new Color(0.09f, 0.2f, 0.28f, 0.95f) : UiFactory.PanelSoft;
                var card = ui.PanelRect("SquadCard_" + squadron.id, panel, new Vector2(18f, y), new Vector2(340f, 76f), cardColor);
                card.GetComponent<Image>().raycastTarget = false;
                ui.Outline("SquadCardEdge_" + squadron.id, panel, new Vector2(18f, y), new Vector2(340f, 76f), 1f,
                    destroyed ? UiFactory.Danger : busy ? UiFactory.Aether : new Color(0.3f, 0.36f, 0.42f, 0.9f));
                var typeGlyph = squadron.type == SquadronType.Interceptor ? "▲" : squadron.type == SquadronType.Bomber ? "▼" : "◆";
                ui.Text("SquadGlyph_" + squadron.id, card, typeGlyph, 30, destroyed ? UiFactory.Danger : UiFactory.Brass, TextAnchor.MiddleCenter,
                    new Vector2(6f, 10f), new Vector2(48f, 56f), FontStyle.Bold);
                ui.Text("SquadName_" + squadron.id, card, l10n.T(squadron.displayKey), 15, destroyed ? UiFactory.Danger : UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                    new Vector2(58f, 44f), new Vector2(200f, 26f), FontStyle.Bold);
                var strengthPips = new StringBuilder();
                for (var p = 0; p < squadron.maxStrength; p++) strengthPips.Append(p < squadron.strength ? '●' : '○');
                ui.Text("SquadStrength_" + squadron.id, card, strengthPips.ToString(), 13, squadron.strength <= 1 ? UiFactory.Danger : UiFactory.Aether, TextAnchor.MiddleRight,
                    new Vector2(250f, 44f), new Vector2(84f, 26f), FontStyle.Bold);
                var statusLine = l10n.EnumName(squadron.status);
                if (squadron.mission != SquadronMission.None && busy) statusLine += " · " + MissionGlyph(squadron.mission) + " " + l10n.EnumName(squadron.mission);
                if ((squadron.mission == SquadronMission.Bombard || squadron.mission == SquadronMission.Assault) && busy)
                    statusLine += " → " + l10n.T(state.enemyShip.GetSystem(squadron.targetSystem).displayKey);
                ui.Text("SquadStatus_" + squadron.id, card, statusLine, 12, busy ? UiFactory.Aether : UiFactory.TextMuted, TextAnchor.MiddleLeft,
                    new Vector2(58f, 20f), new Vector2(276f, 22f));
                ui.Text("SquadCost_" + squadron.id, card, $"{l10n.T("ui.ordnance")} {squadron.ordnanceCost}", 11,
                    state.resources.ordnance >= squadron.ordnanceCost ? UiFactory.TextMuted : UiFactory.Danger, TextAnchor.MiddleLeft,
                    new Vector2(58f, 4f), new Vector2(200f, 18f));

                // Mission gauge across the card bottom.
                var progress = squadron.status == SquadronStatus.Ready ? 1f : destroyed ? 0f :
                    squadron.phaseDuration <= 0f ? 0f : 1f - squadron.missionTimer / squadron.phaseDuration;
                var progressColor = squadron.status == SquadronStatus.Recovering ? UiFactory.Success : destroyed ? UiFactory.Danger : UiFactory.Aether;
                ui.Bar("SquadProgress_" + squadron.id, panel, progress, new Vector2(18f, y - 4f), new Vector2(340f, 4f), progressColor);

                // Mission slots.
                for (var m = 0; m < missions.Length; m++)
                {
                    var mission = missions[m];
                    var localSquad = squadron.id;
                    var shortcut = mission == SquadronMission.Bombard && i < 2 ? $"[{i + 1}] " : string.Empty;
                    var label = $"{MissionGlyph(mission)}  {shortcut}{l10n.T(keys[m])}";
                    var color = mission == SquadronMission.Bombard ? new Color(0.43f, 0.18f, 0.11f, 0.98f)
                        : mission == SquadronMission.Assault ? new Color(0.36f, 0.14f, 0.3f, 0.98f)
                        : mission == SquadronMission.Intercept || mission == SquadronMission.Escort ? new Color(0.09f, 0.24f, 0.3f, 0.98f)
                        : UiFactory.PanelSoft;
                    var button = ui.Button($"{squadron.id}_{mission}", panel, label, () => controller.LaunchSquadron(localSquad, mission, selectedEnemySystem),
                        new Vector2(376f + m * 298f, y + 8f), new Vector2(284f, 60f), color, UiFactory.TextPrimary, 16);
                    button.interactable = squadron.CanLaunch && state.resources.ordnance >= squadron.ordnanceCost && deckReady;
                }
            }
            if (!deckReady)
                ui.Text("DeckWarning", panel, L("비행갑판 전력 없음 — 발진 불가", "Flight deck unpowered — launches blocked"), 13, UiFactory.Danger, TextAnchor.MiddleLeft,
                    new Vector2(376f, 190f), new Vector2(600f, 32f), FontStyle.Bold);
        }

        private void AddRoomDetail(Transform parent, string id, ShipState ship, ShipSystemState system, List<CrewState> crew, bool allocatedPower,
            Vector2 position, Vector2 size)
        {
            var strip = ui.PanelRect(id, parent, position, size, UiFactory.PanelSoft);
            strip.GetComponent<Image>().raycastTarget = false;
            if (system == null) return;
            var room = ship.GetRoom(system.type);
            var condition = BlueprintRules.Classify(system);
            var conditionText = condition == RoomCondition.Disabled ? l10n.T("ui.disabled")
                : condition == RoomCondition.Unpowered ? l10n.T("ui.unpowered")
                : condition == RoomCondition.Damaged ? L("손상", "DAMAGED") : L("정상", "NOMINAL");
            var conditionColor = condition == RoomCondition.Operational ? UiFactory.Success : condition == RoomCondition.Unpowered ? UiFactory.TextMuted : UiFactory.Danger;
            var powerText = system.maxPower > 0
                ? $"{l10n.T("ui.power")} {(allocatedPower ? system.power : system.EffectivePower)}/{system.maxPower}"
                : l10n.T("system.core");
            ui.Text(id + "Name", strip, l10n.T(system.displayKey), 15, UiFactory.Aether, TextAnchor.MiddleLeft,
                new Vector2(12f, size.y - 30f), new Vector2(200f, 26f), FontStyle.Bold);
            ui.Text(id + "Condition", strip, conditionText, 13, conditionColor, TextAnchor.MiddleLeft,
                new Vector2(160f, size.y - 30f), new Vector2(140f, 26f), FontStyle.Bold);
            ui.Text(id + "Stats", strip, $"{l10n.T("ui.integrity")} {system.Integrity * 100f:0}%   ·   {powerText}", 13, UiFactory.TextPrimary, TextAnchor.MiddleRight,
                new Vector2(size.x - 372f, size.y - 30f), new Vector2(360f, 26f));

            var hazards = new StringBuilder();
            if (room != null)
            {
                if (room.fire > 1f) hazards.Append("▲ ").Append(l10n.T("ui.fire_short")).Append(' ').Append(room.fire.ToString("0")).Append("   ");
                if (room.breach > 1f) hazards.Append("◇ ").Append(l10n.T("ui.breach_short")).Append(' ').Append(room.breach.ToString("0")).Append("   ");
                if (room.intruders > 0) hazards.Append("■ ").Append(l10n.T("ui.intruders", room.intruders.ToString())).Append("   ");
                hazards.Append("O₂ ").Append(room.oxygen.ToString("0")).Append('%');
            }
            var hazardous = room != null && (room.fire > 10f || room.breach > 10f || room.oxygen < 30f || room.intruders > 0);
            ui.Text(id + "Hazards", strip, hazards.ToString(), 12, hazardous ? UiFactory.Danger : UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(12f, 6f), new Vector2(300f, 24f), hazardous ? FontStyle.Bold : FontStyle.Normal);

            if (crew != null)
            {
                var names = new StringBuilder();
                for (var i = 0; i < crew.Count; i++)
                {
                    if (crew[i].currentRoom != system.type || crew[i].isDead || crew[i].onSortie) continue;
                    if (names.Length > 0) names.Append(", ");
                    names.Append(crew[i].displayName);
                    if (crew[i].IsDowned) names.Append(" (!)");
                }
                ui.Text(id + "Crew", strip, names.Length > 0 ? L("승무원", "Crew") + ": " + names : L("배치된 승무원 없음", "No crew posted"), 12,
                    names.Length > 0 ? UiFactory.TextPrimary : UiFactory.TextMuted, TextAnchor.MiddleRight, new Vector2(size.x - 372f, 6f), new Vector2(360f, 24f));
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
            var argument = TranslateArgument(state.combatAlertArgument);
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
            return l10n.T(entry.key, TranslateArgument(entry.argument));
        }

        /// <summary>
        /// Log and alert arguments are raw identifiers: a system or altitude enum name, a localization key
        /// (squadron., module., ship.) or a plain value such as a number or a crew name. Numeric strings must
        /// not be parsed as enums, or "2" would become the second ship system.
        /// </summary>
        private string TranslateArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument) || char.IsDigit(argument[0]) || argument[0] == '-') return argument;
            if (Enum.TryParse(argument, out ShipSystemType system) && Enum.IsDefined(typeof(ShipSystemType), system))
            {
                var state = controller.Simulation.State.playerShip.GetSystem(system) ?? controller.Simulation.State.enemyShip?.GetSystem(system);
                if (state != null) return l10n.T(state.displayKey);
            }
            if (Enum.TryParse(argument, out AltitudeBand altitude) && Enum.IsDefined(typeof(AltitudeBand), altitude)) return l10n.EnumName(altitude);
            var translated = l10n.T(argument);
            return translated != argument ? translated : argument;
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
