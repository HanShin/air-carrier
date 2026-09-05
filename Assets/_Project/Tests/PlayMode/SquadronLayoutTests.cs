using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AetherArk.Tests
{
    public sealed class SquadronLayoutTests
    {
        private static readonly SquadronMission[] Missions = { SquadronMission.Intercept, SquadronMission.Bombard, SquadronMission.Escort, SquadronMission.Recon, SquadronMission.Assault };
        private GameController controller;
        private SaveService originalSaves;
        private string temporaryRoot;
        private string originalProfile;
        private Language originalLanguage;
        private static readonly FieldInfo SavesField = typeof(GameController).GetField("saves", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnitySetUp]
        public IEnumerator IsolatePersistence()
        {
            yield return null;
            controller = Object.FindFirstObjectByType<GameController>();
            Assert.That(controller, Is.Not.Null);
            originalSaves = (SaveService)SavesField.GetValue(controller);
            originalProfile = JsonUtility.ToJson(controller.Profile);
            originalLanguage = controller.L10n.Language;
            temporaryRoot = Path.Combine(Path.GetTempPath(), "aether-layout-test-" + Guid.NewGuid().ToString("N"));
            SavesField.SetValue(controller, new SaveService(temporaryRoot));
            controller.Profile.tutorialSeen = true;
            controller.Profile.campaignVictories = 1;
        }

        [UnityTearDown]
        public IEnumerator RestorePersistence()
        {
            if (controller != null && originalProfile != null)
            {
                controller.AbandonRun();
                JsonUtility.FromJsonOverwrite(originalProfile, controller.Profile);
                controller.L10n.Language = originalLanguage;
                SavesField.SetValue(controller, originalSaves);
            }
            if (temporaryRoot != null && Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllFlagshipsAndFourBayLayoutExposeEveryMissionWithoutOverlap()
        {
            foreach (var language in new[] { Language.Korean, Language.English })
            foreach (var flagship in new[] { "ship_vanguard", "ship_bastion", "ship_zephyr", "four_bay_fixture" })
            {
                controller.L10n.Language = language;
                controller.Profile.accessibility.highContrast = language == Language.English;
                StartBattle(flagship == "four_bay_fixture" ? "ship_zephyr" : flagship);
                var state = controller.Simulation.State;
                if (flagship == "four_bay_fixture")
                {
                    var fourth = JsonUtility.FromJson<SquadronState>(JsonUtility.ToJson(state.squadrons[2]));
                    fourth.id = "fourth_test_wing";
                    state.squadrons.Add(fourth);
                    RefreshPaused();
                }
                yield return null;
                Canvas.ForceUpdateCanvases();
                var panel = FindRect("SquadronPanel");
                var canvas = (RectTransform)panel.parent;
                AssertInside(canvas.rect, RelativeBounds(canvas, panel));
                var cards = new List<Rect>(); var buttons = new List<Rect>();
                foreach (var squadron in state.squadrons)
                {
                    var card = FindRect("SquadCard_" + squadron.id);
                    var cardBounds = RelativeBounds(panel, card);
                    AssertInside(panel.rect, cardBounds);
                    foreach (var other in cards) Assert.That(other.Overlaps(cardBounds), Is.False);
                    cards.Add(cardBounds);
                    foreach (var field in new[] { "SquadName_", "SquadStatus_", "SquadCost_", "SquadStrength_" })
                        AssertInside(card.rect, RelativeBounds(card, FindRect(field + squadron.id)));
                    AssertInside(panel.rect, RelativeBounds(panel, FindRect("SquadProgress_" + squadron.id)));
                    var fill = FindRect("SquadProgress_" + squadron.id + "Fill");
                    Assert.That(fill.rect.height, Is.GreaterThan(0f), "Bar insets must leave a visible mission gauge.");
                    Assert.That(fill.rect.width, Is.GreaterThan(0f), "Ready wings must show a full gauge.");
                    foreach (var mission in Missions)
                    {
                        var button = FindButton(squadron.id + "_" + mission);
                        Assert.That(button.interactable, Is.True);
                        var bounds = RelativeBounds(panel, (RectTransform)button.transform);
                        AssertInside(panel.rect, bounds);
                        if (state.squadrons.Count > 2) AssertInside(cardBounds, bounds);
                        foreach (var other in buttons) Assert.That(other.Overlaps(bounds), Is.False);
                        buttons.Add(bounds);
                        AssertRaycastHits(button, (RectTransform)button.transform);
                        AssertRaycastHits(button, FindRect("MissionIcon_" + squadron.id + "_" + mission));
                    }
                }
                Assert.That(buttons.Count, Is.EqualTo(state.squadrons.Count * 5));
                Assert.That(RelativeBounds(panel, FindRect("PlayerShip")).Overlaps(panel.rect), Is.False,
                    "Air-wing cards must not grow over the ship controls.");
            }
        }

        [UnityTest]
        public IEnumerator EnlargedUiKeepsAllThirdBayCommandsInsideTheWindow()
        {
            var scaler = Object.FindFirstObjectByType<CanvasScaler>();
            var originalResolution = scaler.referenceResolution;
            try
            {
                StartBattle(); yield return null;
                var panelBefore = FindRect("SquadronPanel");
                var stateBefore = JsonUtility.ToJson(controller.Simulation.State);
                scaler.referenceResolution = new Vector2(1920f / 1.15f, 1080f / 1.15f);
                yield return null;
                yield return null; // LateUpdate responds without a command/UI rebuild while combat remains paused.
                Canvas.ForceUpdateCanvases();
                var panel = FindRect("SquadronPanel");
                Assert.That(panel, Is.SameAs(panelBefore));
                Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(stateBefore));
                var canvas = (RectTransform)panel.parent;
                AssertInside(canvas.rect, RelativeBounds(canvas, panel));
                foreach (var mission in Missions)
                {
                    var button = FindButton(controller.Simulation.State.squadrons[2].id + "_" + mission);
                    AssertRaycastHits(button, (RectTransform)button.transform);
                }
            }
            finally { scaler.referenceResolution = originalResolution; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThirdWingLaunchesReconResumesAndReturnsToReady()
        {
            StartBattle(); yield return null;
            var state = controller.Simulation.State; var squadron = state.squadrons[2];
            state.resources.ordnance = 0; // Far Eyes is a zero-cost wing, including when magazines are empty.
            RefreshPaused(); yield return null;
            var ordnance = state.resources.ordnance;
            Assert.That(GameObject.Find("MissionShortcut_" + squadron.id + "_Recon").GetComponent<Text>().text, Is.EqualTo("[3]"));
            FindButton(squadron.id + "_Recon").onClick.Invoke(); yield return null;
            Assert.That(squadron.mission, Is.EqualTo(SquadronMission.Recon));
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Launching));
            Assert.That(state.resources.ordnance, Is.EqualTo(ordnance - GameSimulation.SquadronLaunchCost(squadron)));
            controller.ContinueRun(); yield return null;
            state = controller.Simulation.State; squadron = state.squadrons[2];
            Assert.That(state.squadrons.Count, Is.EqualTo(3));
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Launching));
            AssertAllDisabled(squadron);
            state.autoPauseOnWarning = false;
            state.weatherHazardTimer = state.enemySquadronCooldown = 1000f;
            foreach (var weapon in state.enemyShip.weaponSlots) weapon.cooldown = 1000f;
            foreach (var expected in new[] { SquadronStatus.OnMission, SquadronStatus.Recovering, SquadronStatus.Ready })
            {
                squadron.missionTimer = 0f;
                controller.Simulation.SetPaused(false);
                controller.Simulation.Tick(0.1f);
                RefreshPaused(); yield return null;
                Assert.That(squadron.status, Is.EqualTo(expected));
                Assert.That(GameObject.Find("SquadStatus_" + squadron.id).GetComponent<Text>().text, Does.Contain(controller.L10n.EnumName(expected)));
                if (expected != SquadronStatus.Ready) AssertAllDisabled(squadron);
                if (expected == SquadronStatus.OnMission)
                {
                    squadron.missionTimer = squadron.phaseDuration / 2f;
                    RefreshPaused(); yield return null;
                    var gauge = FindRect("SquadProgress_" + squadron.id);
                    var fill = FindRect("SquadProgress_" + squadron.id + "Fill");
                    Assert.That(fill.rect.width, Is.EqualTo((gauge.rect.width - 4f) * 0.5f).Within(0.1f));
                    Assert.That(fill.rect.height, Is.GreaterThan(0f));
                }
            }
            Assert.That(state.crew.Find(crew => crew.id == squadron.pilotCrewId).onSortie, Is.False);
            Assert.That(FindButton(squadron.id + "_Recon").interactable, Is.True);
            controller.LaunchSquadronShortcut(2); yield return null;
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Launching));
            Assert.That(squadron.mission, Is.EqualTo(SquadronMission.Recon));
        }

        [UnityTest]
        public IEnumerator ShortcutsKeepBombardUseSelectedTargetAndFollowPortReplacement()
        {
            StartBattle(); yield return null;
            FindButton("EnemySystem_Engines").onClick.Invoke(); yield return null;
            var state = controller.Simulation.State;
            for (var slot = 0; slot < 2; slot++)
            {
                controller.LaunchSquadronShortcut(slot); yield return null;
                Assert.That(state.squadrons[slot].mission, Is.EqualTo(SquadronMission.Bombard));
                Assert.That(state.squadrons[slot].targetSystem, Is.EqualTo(ShipSystemType.Engines));
            }
            controller.ContinueRun(); yield return null;
            Assert.That(GameObject.Find("EnemyTargetHint").GetComponent<Text>().text,
                Does.Contain(controller.L10n.T(controller.Simulation.State.enemyShip.GetSystem(ShipSystemType.Engines).displayKey)));

            StartBattle();
            state = controller.Simulation.State;
            state.phase = GamePhase.Port; state.resources.salvage = 100;
            var pilotId = state.squadrons[2].pilotCrewId;
            controller.PurchaseWing("storm_marines"); yield return null;
            Assert.That(state.squadrons[2].wingId, Is.EqualTo("storm_marines"));
            Assert.That(state.squadrons[2].pilotCrewId, Is.EqualTo(pilotId));
            controller.Simulation.BeginCombat(1, false); RefreshPaused(); yield return null;
            var third = state.squadrons[2];
            Assert.That(GameObject.Find("MissionShortcut_" + third.id + "_Recon"), Is.Null);
            Assert.That(GameObject.Find("MissionShortcut_" + third.id + "_Assault").GetComponent<Text>().text, Is.EqualTo("[3]"));
            controller.LaunchSquadronShortcut(2); yield return null;
            Assert.That(third.mission, Is.EqualTo(SquadronMission.Assault));
            Assert.That(third.status, Is.EqualTo(SquadronStatus.Launching));
        }

        [UnityTest]
        public IEnumerator ThirdBayShowsPilotDeckAndOrdnanceReasonsAndRejectsShortcuts()
        {
            foreach (var language in new[] { Language.Korean, Language.English })
            foreach (var key in new[] { "command.pilot_missing", "command.pilot_dead", "command.pilot_downed", "command.pilot_busy", "command.deck_unpowered", "command.no_ordnance" })
            {
                controller.L10n.Language = language;
                StartBattle();
                var state = controller.Simulation.State; var squadron = state.squadrons[2];
                var pilot = state.crew.Find(crew => crew.id == squadron.pilotCrewId);
                if (key == "command.pilot_missing") squadron.pilotCrewId = "missing";
                if (key == "command.pilot_dead") pilot.isDead = true;
                if (key == "command.pilot_downed") pilot.health = 0f;
                if (key == "command.pilot_busy") pilot.onSortie = true;
                if (key == "command.deck_unpowered") state.playerShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
                if (key == "command.no_ordnance")
                {
                    squadron.wingId = "thunder_bombers"; // Far Eyes costs zero; exercise an actual paid wing instead.
                    state.resources.ordnance = 0;
                }
                RefreshPaused(); yield return null;
                AssertAllDisabled(squadron);
                Assert.That(GameObject.Find("SquadStatus_" + squadron.id).GetComponent<Text>().text, Is.EqualTo(controller.L10n.T(key)));
                if (key == "command.deck_unpowered")
                {
                    Assert.That(GameObject.Find("DeckWarning"), Is.Not.Null);
                    Assert.That(GameObject.Find("TutorialHint"), Is.Null, "Deck warnings must not overlap tutorial text.");
                }
                var before = JsonUtility.ToJson(state);
                controller.LaunchSquadronShortcut(2); yield return null;
                Assert.That(controller.LastCommandMessage, Is.EqualTo(key));
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            }
        }

        [UnityTest]
        public IEnumerator InvalidSlotsAndAudioSettingsCannotDispatchSorties()
        {
            StartBattle(); yield return null;
            var before = JsonUtility.ToJson(controller.Simulation.State);
            foreach (var slot in new[] { -1, 3, 9 }) controller.LaunchSquadronShortcut(slot);
            Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(before));
            controller.ToggleAudioSettings(); yield return null;
            before = JsonUtility.ToJson(controller.Simulation.State);
            controller.LaunchSquadronShortcut(2);
            Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(before));
            controller.ToggleAudioSettings(); yield return null;
            controller.Simulation.State.phase = GamePhase.RouteMap;
            before = JsonUtility.ToJson(controller.Simulation.State);
            controller.LaunchSquadronShortcut(2);
            Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(before));
        }

        private void StartBattle(string flagship = "ship_zephyr")
        {
            controller.Profile.flagshipId = flagship;
            controller.StartRun(); controller.Simulation.BeginCombat(1, false);
            RefreshPaused();
        }

        private void RefreshPaused()
        {
            controller.Simulation.SetPaused(false); controller.TogglePause();
        }

        private static Button FindButton(string name)
        {
            var item = GameObject.Find(name);
            Assert.That(item, Is.Not.Null, name);
            return item.GetComponent<Button>();
        }

        private static RectTransform FindRect(string name)
        {
            var item = GameObject.Find(name);
            Assert.That(item, Is.Not.Null, name);
            return item.GetComponent<RectTransform>();
        }

        private static Rect RelativeBounds(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            var lower = parent.InverseTransformPoint(corners[0]); var upper = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
        }

        private static void AssertInside(Rect outer, Rect inner)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 0.1f));
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 0.1f));
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 0.1f));
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 0.1f));
        }

        private static void AssertRaycastHits(Button button, RectTransform surface)
        {
            var point = RectTransformUtility.WorldToScreenPoint(null, surface.TransformPoint(surface.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " / " + surface.name + " at " + point + " on " + Screen.width + "x" + Screen.height +
                " canvas " + Object.FindFirstObjectByType<CanvasScaler>().referenceResolution);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button), "Decorations must not intercept mission clicks.");
        }

        private static void AssertAllDisabled(SquadronState squadron)
        {
            foreach (var mission in Missions) Assert.That(FindButton(squadron.id + "_" + mission).interactable, Is.False);
        }
    }
}
