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
    public sealed class ReproductionUiTests
    {
        private GameController controller;
        private SaveService originalSaves;
        private object originalStore;
        private string originalProfile;
        private Language originalLanguage;
        private string root;
        private static readonly FieldInfo Saves = typeof(GameController).GetField("saves", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Store = typeof(GameController).GetField("reproductionStore", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo LaunchArguments = typeof(GameController).GetMethod("PrepareDevelopmentSession", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            controller = Object.FindFirstObjectByType<GameController>();
            originalSaves = (SaveService)Saves.GetValue(controller);
            originalStore = Store.GetValue(controller);
            originalProfile = JsonUtility.ToJson(controller.Profile);
            originalLanguage = controller.L10n.Language;
            root = Path.Combine(Path.GetTempPath(), "aether-repro-ui-" + Guid.NewGuid().ToString("N"));
            Saves.SetValue(controller, new SaveService(root)); Store.SetValue(controller, null);
            controller.ShowMenu();
            controller.Profile.tutorialSeen = true;
            controller.Profile.campaignVictories = 1;
            controller.Profile.flagshipId = "ship_vanguard";
            controller.ReproductionSeed = "17000";
            controller.ReproductionFlagship = "ship_zephyr";
            controller.ReproductionDifficulty = Difficulty.Standard;
            controller.ReproductionSnapshotPath = string.Empty;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controller != null && originalProfile != null)
            {
                if (controller.IsReproduction) controller.ReturnFromReproduction();
                controller.AbandonRun();
                JsonUtility.FromJsonOverwrite(originalProfile, controller.Profile);
                controller.L10n.Language = originalLanguage;
                Saves.SetValue(controller, originalSaves); Store.SetValue(controller, originalStore);
            }
            if (Directory.Exists(root)) Directory.Delete(root, true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LabHoldsBattleWithoutChangingPauseRngOrAllowingUnderlyingCommands()
        {
            StartNormalBattle(); yield return null;
            foreach (var paused in new[] { true, false })
            {
                controller.Simulation.SetPaused(paused);
                var before = JsonUtility.ToJson(controller.Simulation.State);
                Button("ReproductionTools").onClick.Invoke(); yield return null; yield return null;
                Assert.That(controller.ReproductionPanelOpen, Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button("ReproductionCapture").gameObject));
                Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(before));
                controller.Fire(ShipSystemType.Weapons);
                controller.LaunchSquadronShortcut(0);
                controller.TogglePause(); controller.ToggleAudioSettings();
                Assert.That(controller.AudioSettingsOpen, Is.False);
                Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(before));
                Button("ReproductionClose").onClick.Invoke();
                Assert.That(controller.Simulation.State.isPaused, Is.EqualTo(paused));
                controller.Simulation.SetPaused(true); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SeededSessionKeepsNormalStateProfileAndAllSaveBytesUntouched()
        {
            StartNormalBattle(); yield return null;
            controller.Simulation.SetPaused(false);
            controller.ToggleReproductionPanel();
            var normal = controller.Simulation; var normalProfile = controller.Profile;
            var stateBefore = JsonUtility.ToJson(normal.State); var profileBefore = JsonUtility.ToJson(normalProfile);
            var files = NormalSaveBytes();
            GameObject.Find("ReproductionSeed").GetComponent<InputField>().text = "-12345";
            Button("ReproductionStartBattle").onClick.Invoke(); yield return null;
            Assert.That(controller.IsReproduction, Is.True);
            Assert.That(controller.Simulation.State.seed, Is.EqualTo(-12345));
            Assert.That(controller.Simulation.State.playerShip.id, Is.EqualTo("ship_zephyr"));
            Assert.That(controller.Simulation.State.isFirstExpedition, Is.False);
            Assert.That(controller.Simulation.State.regionCount, Is.EqualTo(6));
            Assert.That(GameObject.Find("ShipName").GetComponent<Text>().text, Does.Contain("TEST · Seed -12345"));
            controller.LaunchSquadronShortcut(2); controller.StepReproduction();
            controller.Simulation.State.phase = GamePhase.Victory;
            controller.FieldRepair(); // Even outcome-triggered persistence cannot unlock the normal profile.
            controller.ReturnFromReproduction(); yield return null;
            Assert.That(controller.IsReproduction, Is.False);
            Assert.That(controller.ReproductionPanelOpen, Is.True);
            Assert.That(controller.Simulation, Is.SameAs(normal));
            Assert.That(controller.Profile, Is.SameAs(normalProfile));
            Assert.That(JsonUtility.ToJson(normal.State), Is.EqualTo(stateBefore));
            Assert.That(JsonUtility.ToJson(normalProfile), Is.EqualTo(profileBefore));
            AssertNormalFiles(files);
        }

        [UnityTest]
        public IEnumerator CaptureAndReloadRestoreBattleAndNeverRewriteSourceOrReplayAudio()
        {
            controller.StartSeededReproduction(true); yield return null;
            controller.Simulation.SetPaused(false);
            controller.ToggleReproductionPanel();
            var before = JsonUtility.ToJson(controller.Simulation.State);
            Button("ReproductionCapture").onClick.Invoke(); yield return null;
            Assert.That(controller.ReproductionMessageKey, Is.EqualTo("repro.saved"));
            var path = controller.ReproductionSnapshotPath;
            var bytes = File.ReadAllBytes(path);
            Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(before));
            var expected = JsonUtility.FromJson<RunState>(before); expected.isPaused = true;
            var cueCount = controller.Audio.PlayedCueCount;
            Button("ReproductionLoad").onClick.Invoke(); yield return null;
            Assert.That(controller.ReproductionMessageKey, Is.EqualTo("repro.loaded"));
            Assert.That(controller.ReproductionPanelOpen, Is.True);
            Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(JsonUtility.ToJson(expected)));
            Assert.That(controller.Audio.PlayedCueCount, Is.EqualTo(cueCount));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            Button("ReproductionStep").onClick.Invoke(); yield return null;
            Assert.That(controller.Simulation.State.combatElapsed, Is.EqualTo(expected.combatElapsed + 0.1f).Within(0.00001f));
            Assert.That(controller.Simulation.State.isPaused, Is.True);
            Button("ReproductionLoad").onClick.Invoke(); yield return null;
            Assert.That(JsonUtility.ToJson(controller.Simulation.State), Is.EqualTo(JsonUtility.ToJson(expected)));
        }

        [UnityTest]
        public IEnumerator NormalBattleCaptureIsReadOnlyAndLatestSelectionDoesNotLoad()
        {
            StartNormalBattle(); yield return null;
            controller.ToggleReproductionPanel();
            var normal = controller.Simulation; var before = JsonUtility.ToJson(normal.State); var files = NormalSaveBytes();
            Assert.That(Button("ReproductionCapture").interactable, Is.True);
            Assert.That(Button("ReproductionStep").interactable, Is.False);
            Button("ReproductionCapture").onClick.Invoke(); yield return null;
            var path = controller.ReproductionSnapshotPath;
            Assert.That(controller.IsReproduction, Is.False);
            controller.ReproductionSnapshotPath = string.Empty;
            Button("ReproductionLatest").onClick.Invoke(); yield return null;
            Assert.That(controller.ReproductionSnapshotPath, Is.EqualTo(path));
            Assert.That(controller.Simulation, Is.SameAs(normal));
            Assert.That(JsonUtility.ToJson(normal.State), Is.EqualTo(before));
            AssertNormalFiles(files);
        }

        [UnityTest]
        public IEnumerator InvalidSeedAndSnapshotKeepCurrentGameAndShowLocalizedErrors()
        {
            StartNormalBattle(); yield return null;
            controller.ToggleReproductionPanel();
            var normal = controller.Simulation; var before = JsonUtility.ToJson(normal.State); var files = NormalSaveBytes();
            foreach (var language in new[] { Language.Korean, Language.English })
            {
                controller.L10n.Language = language;
                controller.ReproductionSeed = "2147483648";
                controller.StartSeededReproduction(true); yield return null;
                AssertMessage("repro.invalid_seed");
                controller.ReproductionSnapshotPath = Path.Combine(root, "absent.json");
                controller.LoadReproductionSnapshot(); yield return null;
                AssertMessage("repro.snapshot_missing");
                Directory.CreateDirectory(Path.Combine(root, "imports"));
                controller.ReproductionSnapshotPath = Path.Combine(root, "imports", "bad.json");
                File.WriteAllText(controller.ReproductionSnapshotPath, "{}");
                controller.LoadReproductionSnapshot(); yield return null;
                AssertMessage("repro.invalid_snapshot");
                Assert.That(controller.IsReproduction, Is.False);
                Assert.That(controller.Simulation, Is.SameAs(normal));
                Assert.That(JsonUtility.ToJson(normal.State), Is.EqualTo(before));
                AssertNormalFiles(files);
            }
        }

        [UnityTest]
        public IEnumerator CommandLineSeedIsExactAndConflictingScenariosAreNotSilentlySearched()
        {
            StartNormalBattle(); yield return null;
            var normal = controller.Simulation; var files = NormalSaveBytes();
            Assert.That((bool)LaunchArguments.Invoke(controller, new object[] { new[] { "game" } }), Is.False);
            foreach (var args in new[]
            {
                new[] { "-debug-seed", "32838", "-debug-battle", "-debug-flagship", "ship_zephyr", "-debug-difficulty", "Harsh" },
                new[] { "-debug-seed", "-2147483648", "-debug-difficulty", "Standard" }
            })
            {
                Assert.That((bool)LaunchArguments.Invoke(controller, new object[] { args }), Is.True); yield return null;
                Assert.That(controller.IsReproduction, Is.True);
                Assert.That(controller.Simulation.State.seed.ToString(), Is.EqualTo(args[1]));
                Assert.That(controller.Simulation.State.regionCount, Is.EqualTo(6));
                Assert.That(controller.Simulation.State.isFirstExpedition, Is.False);
                controller.ReturnFromReproduction(); yield return null;
                Assert.That(controller.Simulation, Is.SameAs(normal)); AssertNormalFiles(files);
            }
            foreach (var args in new[]
            {
                new[] { "-debug-seed", "17000", "-debug-combat", "cutter" },
                new[] { "-debug-seed", "17000", "-debug-snapshot", "anything.json" },
                new[] { "-debug-battle" },
                new[] { "-debug-difficulty", "Story" }
            })
            {
                LaunchArguments.Invoke(controller, new object[] { args }); yield return null;
                AssertMessage("repro.invalid_arguments");
                controller.ReturnFromReproduction(); yield return null;
                Assert.That(controller.Simulation, Is.SameAs(normal)); AssertNormalFiles(files);
            }
        }

        [UnityTest]
        public IEnumerator SnapshotCommandLineAndLegacyDebugEntryBothUseIsolatedSaves()
        {
            StartNormalBattle(); yield return null;
            controller.ToggleReproductionPanel(); controller.CaptureReproductionSnapshot();
            var path = controller.ReproductionSnapshotPath;
            var bytes = File.ReadAllBytes(path); var normal = controller.Simulation; var files = NormalSaveBytes();
            LaunchArguments.Invoke(controller, new object[] { new[] { "-debug-snapshot", path, "-debug-english" } }); yield return null;
            Assert.That(controller.IsReproduction, Is.True);
            Assert.That(controller.ReproductionPanelOpen, Is.True);
            Assert.That(controller.Simulation.State.isPaused, Is.True);
            Assert.That(controller.L10n.Language, Is.EqualTo(Language.English));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            controller.ReturnFromReproduction(); yield return null;
            Assert.That(controller.Simulation, Is.SameAs(normal)); AssertNormalFiles(files);
            Assert.That((bool)LaunchArguments.Invoke(controller, new object[] { new[] { "-debug-combat", "cutter" } }), Is.False,
                "Legacy scenario construction continues only after save isolation is established.");
            Assert.That(controller.IsReproduction, Is.True);
            Assert.That(((SaveService)Saves.GetValue(controller)).RootPath, Is.EqualTo(Path.Combine(root, "reproduction", "session")));
            controller.ShowMenu(); controller.StartRun(); yield return null;
            AssertNormalFiles(files);
        }

        [UnityTest]
        public IEnumerator LabButtonsStayVisibleAndClickableInBothLanguages()
        {
            foreach (var language in new[] { Language.Korean, Language.English })
            {
                controller.L10n.Language = language;
                controller.ToggleReproductionPanel(); yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                var panel = GameObject.Find("ReproductionPanel").GetComponent<RectTransform>();
                var corners = new Vector3[4]; panel.GetWorldCorners(corners);
                Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(0));
                Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(0));
                Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width));
                Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.height));
                foreach (var id in new[] { "ReproductionStartCampaign", "ReproductionStartBattle", "ReproductionCapture", "ReproductionLoad", "ReproductionStep", "ReproductionClose", "ReproductionReturn", "ReproductionLatest" })
                {
                    var button = Button(id); var rect = (RectTransform)button.transform;
                    var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                    Assert.That(hits, Is.Not.Empty, id);
                    Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button), id);
                }
                controller.ToggleReproductionPanel(); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TestSaveContinuesInIsolationAndTerminalStepDoesNotDeleteCapture()
        {
            StartNormalBattle(); yield return null;
            var files = NormalSaveBytes();
            controller.StartSeededReproduction(true); yield return null;
            controller.CaptureReproductionSnapshot();
            var path = controller.ReproductionSnapshotPath;
            controller.Simulation.State.playerShip.hull = 0f;
            controller.StepReproduction(); yield return null;
            Assert.That(controller.Simulation.State.phase, Is.EqualTo(GamePhase.Defeat));
            Assert.That(File.Exists(path), Is.True);
            controller.LoadReproductionSnapshot(); yield return null;
            controller.ToggleReproductionPanel();
            controller.LaunchSquadronShortcut(2);
            controller.ShowMenu(); controller.ContinueRun(); yield return null;
            Assert.That(controller.IsReproduction, Is.True);
            Assert.That(controller.Simulation.State.squadrons[2].status, Is.EqualTo(SquadronStatus.Launching));
            AssertNormalFiles(files);
        }

        private void StartNormalBattle()
        {
            controller.StartRun(); controller.Simulation.BeginCombat(1, false);
            controller.TogglePause(); controller.TogglePause();
        }

        private Dictionary<string, byte[]> NormalSaveBytes()
        {
            var result = new Dictionary<string, byte[]>();
            foreach (var name in new[] { "profile.json", "profile.json.bak", "suspended_run.json", "suspended_run.json.bak" })
                if (File.Exists(Path.Combine(root, name))) result[name] = File.ReadAllBytes(Path.Combine(root, name));
            return result;
        }

        private void AssertNormalFiles(Dictionary<string, byte[]> before)
        {
            var after = NormalSaveBytes();
            Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
            foreach (var entry in before) Assert.That(after[entry.Key], Is.EqualTo(entry.Value), entry.Key);
        }

        private void AssertMessage(string key)
        {
            Assert.That(controller.ReproductionMessageKey, Is.EqualTo(key));
            Assert.That(GameObject.Find("ReproductionMessage").GetComponent<Text>().text, Does.StartWith(controller.L10n.T(key)));
            Assert.That(controller.L10n.T(key), Is.Not.EqualTo(key));
        }

        private static Button Button(string name)
        {
            var item = GameObject.Find(name); Assert.That(item, Is.Not.Null, name); return item.GetComponent<Button>();
        }
    }
}
