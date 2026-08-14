using System.Collections;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AetherArk.Tests
{
    public sealed class CombatUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator CombatButtons_HaveWorkingCommandBindingsAndMouseInfrastructure()
        {
            yield return null;
            var controller = Object.FindFirstObjectByType<GameController>();
            Assert.That(controller, Is.Not.Null, "The runtime scene did not create a GameController.");

            controller.StartRun();
            controller.Simulation.BeginCombat(1, false);
            controller.TogglePause(); // Rebuild the view in active combat.
            yield return null;

            Assert.That(EventSystem.current, Is.Not.Null, "Mouse input requires an active EventSystem.");
            Assert.That(Object.FindFirstObjectByType<GraphicRaycaster>(), Is.Not.Null, "Mouse input requires a canvas GraphicRaycaster.");
            Assert.That(controller.Simulation.State.isPaused, Is.False);
            ActivateButton("Pause");
            yield return null;
            Assert.That(controller.Simulation.State.isPaused, Is.True, "Pause button command binding did not execute.");

            var autoPauseBefore = controller.Simulation.State.autoPauseOnWarning;
            ActivateButton("CombatAutoPause");
            yield return null;
            Assert.That(controller.Simulation.State.autoPauseOnWarning, Is.Not.EqualTo(autoPauseBefore));

            var weapons = controller.Simulation.State.playerShip.GetSystem(ShipSystemType.Weapons);
            var weaponPowerBefore = weapons.power;
            ActivateButton("PowerDown");
            yield return null;
            Assert.That(weapons.power, Is.EqualTo(weaponPowerBefore - 1));

            ActivateButton("PowerUp");
            yield return null;
            Assert.That(weapons.power, Is.EqualTo(weaponPowerBefore));

            ActivateButton("Fire");
            yield return null;
            Assert.That(controller.Simulation.State.playerWeaponCooldown, Is.GreaterThan(0f));

            var squadron = controller.Simulation.State.squadrons[0];
            ActivateButton(squadron.id + "_Bombard");
            yield return null;
            Assert.That(squadron.status, Is.EqualTo(SquadronStatus.Launching));

            var crew = controller.Simulation.State.crew.Find(item => item.IsActive && !item.onSortie);
            Assert.That(crew, Is.Not.Null);
            var destination = crew.currentRoom == ShipSystemType.Sensors ? ShipSystemType.Infirmary : ShipSystemType.Sensors;
            ActivateButton("Crew_" + crew.id);
            yield return null;
            ActivateButton("Room_" + destination);
            yield return null;
            Assert.That(crew.currentRoom, Is.EqualTo(destination), "Crew selection followed by a room click did not issue a move command.");

            controller.AbandonRun();
        }

        private static void ActivateButton(string objectName)
        {
            var target = GameObject.Find(objectName);
            Assert.That(target, Is.Not.Null, "Expected UI object was not rendered: " + objectName);
            var button = target.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, objectName + " is missing its Button component.");
            Assert.That(button.interactable, Is.True, objectName + " was unexpectedly disabled.");
            button.onClick.Invoke();
        }
    }
}
