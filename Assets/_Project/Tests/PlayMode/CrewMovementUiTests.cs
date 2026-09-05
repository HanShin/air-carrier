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
    public sealed class CrewMovementUiTests
    {
        private GameController controller;
        private object originalSaves, originalStore;
        private string root;
        private static readonly FieldInfo Saves = typeof(GameController).GetField("saves", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Store = typeof(GameController).GetField("reproductionStore", BindingFlags.Instance | BindingFlags.NonPublic);
        private CrewFigureGraphic Figure(CrewState c) => GameObject.Find("CrewToken_" + c.id).GetComponent<CrewFigureGraphic>();
        private static void Click(string id) { GameObject.Find(id).GetComponent<Button>().onClick.Invoke(); }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            controller = Object.FindFirstObjectByType<GameController>();
            controller.ShowMenu();
            originalSaves = Saves.GetValue(controller); originalStore = Store.GetValue(controller);
            root = Path.Combine(Path.GetTempPath(), "aether-crew-ui-" + Guid.NewGuid().ToString("N"));
            Saves.SetValue(controller, new SaveService(root)); Store.SetValue(controller, null);
            controller.ReproductionSeed = "17000"; controller.ReproductionFlagship = "ship_zephyr";
            controller.StartSeededReproduction(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            controller.ReturnFromReproduction(); controller.ShowMenu();
            Saves.SetValue(controller, originalSaves); Store.SetValue(controller, originalStore);
            if (Directory.Exists(root)) Directory.Delete(root, true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoomClickQueuesWalkShowsRouteAndFixedStepUpdatesPosition()
        {
            var c = controller.Simulation.State.crew.Find(x => x.role == CrewRole.Engineer);
            Click("Crew_" + c.id); yield return null;
            var before = Figure(c).rectTransform.anchoredPosition;
            Click("Room_Bridge"); yield return null;
            Assert.That(c.IsMoving, Is.True);
            var route = GameObject.Find("CrewRoutes").GetComponent<CrewRouteGraphic>();
            Assert.That(route.selectedCrewId, Is.EqualTo(c.id)); Assert.That(route.raycastTarget, Is.False);
            Assert.That(Figure(c).rectTransform.anchoredPosition, Is.EqualTo(before));
            controller.StepReproduction(); yield return null;
            Assert.That(Figure(c).rectTransform.anchoredPosition, Is.Not.EqualTo(before));
            Assert.That(Figure(c).CurrentActivity, Is.EqualTo(CrewActivity.Walking));
            Assert.That(Figure(c).crew, Is.SameAs(c));
            foreach (var language in new[] { Language.Korean, Language.English })
            {
                controller.L10n.Language = language;
                controller.Fire(ShipSystemType.Weapons); yield return null;
                var label = GameObject.Find("Crew_" + c.id).GetComponentInChildren<Text>().text;
                Assert.That(label, Does.Contain(controller.L10n.T(controller.Simulation.State.playerShip.GetSystem(c.movement.destination).displayKey)));
                Assert.That(label, Does.Not.Contain("enum.shipsystemtype"));
            }
            Click("Crew_" + c.id); yield return null;
            Assert.That(GameObject.Find("CrewRoutes").GetComponent<CrewRouteGraphic>().selectedCrewId, Is.Null);
        }

        [UnityTest]
        public IEnumerator PoseUsesSimulationTimeSurvivesRebuildAndHonoursReducedMotion()
        {
            var c = controller.Simulation.State.crew.Find(x => x.role == CrewRole.Engineer);
            controller.MoveCrew(c.id, ShipSystemType.Bridge); controller.StepReproduction(); yield return null;
            var pose = Figure(c).PoseClock; var position = Figure(c).rectTransform.anchoredPosition;
            yield return null; yield return null;
            Assert.That(Figure(c).PoseClock, Is.EqualTo(pose)); Assert.That(Figure(c).rectTransform.anchoredPosition, Is.EqualTo(position));
            controller.Fire(ShipSystemType.Weapons); yield return null; // Any command rebuilds the screen, but not the walk.
            Assert.That(Figure(c).PoseClock, Is.EqualTo(pose)); Assert.That(Figure(c).rectTransform.anchoredPosition, Is.EqualTo(position));
            controller.Profile.accessibility.reducedMotion = true;
            controller.StepReproduction(); yield return null;
            Assert.That(Figure(c).PoseClock, Is.Zero);
            Assert.That(Figure(c).rectTransform.anchoredPosition, Is.Not.EqualTo(position), "Reduced motion must not skip simulation travel.");
        }

        [UnityTest]
        public IEnumerator AllEightCrewRemainVisibleSelectableAndInsideZoomedDeckInBothLanguages()
        {
            var state = controller.Simulation.State;
            while (state.crew.Count < 8)
            {
                var extra = JsonUtility.FromJson<CrewState>(JsonUtility.ToJson(state.crew[1]));
                extra.id = "extra_" + state.crew.Count; extra.isCaptain = false; state.crew.Add(extra);
            }
            foreach (var c in state.crew) { c.currentRoom = ShipSystemType.FlightDeck; c.movement = null; }
            CrewMovementRules.Ensure(state);
            foreach (var language in new[] { Language.Korean, Language.English })
            {
                controller.L10n.Language = language; controller.Profile.accessibility.highContrast = true;
                controller.Fire(ShipSystemType.Weapons); yield return null;
                Click("Crew_" + state.crew[0].id); yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (var c in state.crew)
                {
                    var figure = Figure(c); Assert.That(figure, Is.Not.Null);
                    var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, figure.rectTransform.TransformPoint(figure.rectTransform.rect.center)) };
                    var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0));
                    Assert.That(hits[0].gameObject, Is.EqualTo(figure.gameObject), "Door/path overlays must not consume crew clicks.");
                    var bounds = figure.transform.parent.GetComponent<RectTransform>().rect;
                    var p = figure.rectTransform.anchoredPosition;
                    Assert.That(p.x, Is.GreaterThanOrEqualTo(0)); Assert.That(p.y, Is.GreaterThanOrEqualTo(0));
                    Assert.That(p.x + figure.rectTransform.rect.width, Is.LessThanOrEqualTo(bounds.width));
                    Assert.That(p.y + figure.rectTransform.rect.height, Is.LessThanOrEqualTo(bounds.height));
                }
            }
        }

        [UnityTest]
        public IEnumerator WorkAndDownedPosesMatchRoomStateAndCannotMoveDownedCrew()
        {
            var state = controller.Simulation.State; var c = state.crew.Find(x => x.role == CrewRole.Engineer);
            var room = state.playerShip.GetRoom(c.currentRoom); var system = state.playerShip.GetSystem(c.currentRoom);
            system.damage = 20; yield return null;
            Assert.That(Figure(c).CurrentActivity, Is.EqualTo(CrewActivity.Repairing));
            room.fire = 10; yield return null;
            Assert.That(Figure(c).CurrentActivity, Is.EqualTo(CrewActivity.Extinguishing));
            c.health = 0; controller.Fire(ShipSystemType.Weapons); yield return null;
            Assert.That(Figure(c).CurrentActivity, Is.EqualTo(CrewActivity.Downed));
            Assert.That(Figure(c).GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Crew_" + c.id).GetComponent<Button>().interactable, Is.False);
        }
    }
}
