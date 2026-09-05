using System.Collections;
using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AetherArk.Tests
{
    public sealed class SquadronPilotUiTests
    {
        private static readonly SquadronMission[] Missions = { SquadronMission.Intercept, SquadronMission.Bombard, SquadronMission.Escort, SquadronMission.Recon, SquadronMission.Assault };

        [UnityTest]
        public IEnumerator PilotReasonsDisableAllMissionButtonsInBothLanguages()
        {
            yield return null;
            var controller = Object.FindFirstObjectByType<GameController>();
            var language = controller.L10n.Language;
            try
            {
                controller.StartRun(); controller.Simulation.BeginCombat(1, false);
                var state = controller.Simulation.State; var squadron = state.squadrons[0];
                var pilot = state.crew.Find(c => c.id == squadron.pilotCrewId);
                var keys = new[] { "command.pilot_missing", "command.pilot_dead", "command.pilot_downed", "command.pilot_busy" };
                foreach (var lang in new[] { Language.Korean, Language.English })
                foreach (var key in keys)
                {
                    controller.L10n.Language = lang;
                    squadron.pilotCrewId = pilot.id;
                    pilot.health = pilot.maxHealth; pilot.isDead = false; pilot.onSortie = false;
                    if (key == "command.pilot_missing") squadron.pilotCrewId = "absent_pilot";
                    if (key == "command.pilot_dead") pilot.isDead = true;
                    if (key == "command.pilot_downed") pilot.health = 0f;
                    if (key == "command.pilot_busy") pilot.onSortie = true;
                    RefreshPaused(controller);
                    yield return null;
                    foreach (var mission in Missions)
                    {
                        var button = GameObject.Find(squadron.id + "_" + mission).GetComponent<Button>();
                        Assert.That(button.interactable, Is.False, key + " / " + mission);
                    }
                    Assert.That(GameObject.Find("SquadStatus_" + squadron.id).GetComponent<Text>().text, Is.EqualTo(controller.L10n.T(key)));
                    var before = JsonUtility.ToJson(state);
                    // An event arriving after the UI was built still has to pass the command-layer guard.
                    GameObject.Find(squadron.id + "_Bombard").GetComponent<Button>().onClick.Invoke();
                    Assert.That(controller.LastCommandMessage, Is.EqualTo(key));
                    Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
                    yield return null;
                }
            }
            finally { controller.L10n.Language = language; controller.AbandonRun(); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HealingEnablesLaunchAndLocksAnotherWingSharingThePilot()
        {
            yield return null;
            var controller = Object.FindFirstObjectByType<GameController>();
            try
            {
                controller.StartRun(); controller.Simulation.BeginCombat(1, false);
                var state = controller.Simulation.State; var first = state.squadrons[0]; var other = state.squadrons[1];
                var pilot = state.crew.Find(c => c.id == first.pilotCrewId);
                other.pilotCrewId = pilot.id;
                pilot.health = 0f;
                RefreshPaused(controller); yield return null;
                Assert.That(GameObject.Find(first.id + "_Intercept").GetComponent<Button>().interactable, Is.False);
                state.convoy.supportShip = SupportShipType.Hospital;
                controller.UseSupport(); yield return null;
                var launch = GameObject.Find(first.id + "_Intercept").GetComponent<Button>();
                Assert.That(launch.interactable, Is.True);
                Assert.That(GameObject.Find("SquadCost_" + first.id).GetComponent<Text>().text, Does.Contain(pilot.displayName));
                launch.onClick.Invoke(); yield return null;
                Assert.That(pilot.onSortie, Is.True);
                foreach (var mission in Missions) Assert.That(GameObject.Find(other.id + "_" + mission).GetComponent<Button>().interactable, Is.False);
                Assert.That(GameObject.Find("SquadStatus_" + other.id).GetComponent<Text>().text, Is.EqualTo(controller.L10n.T("command.pilot_busy")));
            }
            finally { controller.AbandonRun(); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CardsAndButtonsUseTheSameAuthoritativeOrdnanceCost()
        {
            yield return null;
            var controller = Object.FindFirstObjectByType<GameController>();
            try
            {
                controller.StartRun(); controller.Simulation.BeginCombat(1, false);
                var state = controller.Simulation.State; var wing = state.squadrons[0];
                wing.wingId = "thunder_bombers"; wing.ordnanceCost = 0; state.resources.ordnance = 2;
                RefreshPaused(controller); yield return null;
                Assert.That(GameObject.Find("SquadCost_" + wing.id).GetComponent<Text>().text, Does.Contain(controller.L10n.T("ui.ordnance") + " 3"));
                foreach (var mission in Missions) Assert.That(GameObject.Find(wing.id + "_" + mission).GetComponent<Button>().interactable, Is.False);
                state.resources.ordnance = 3;
                RefreshPaused(controller); yield return null;
                Assert.That(GameObject.Find(wing.id + "_Bombard").GetComponent<Button>().interactable, Is.True);
            }
            finally { controller.AbandonRun(); }
            yield return null;
        }

        private static void RefreshPaused(GameController controller)
        {
            controller.Simulation.SetPaused(false);
            controller.TogglePause();
        }
    }
}
