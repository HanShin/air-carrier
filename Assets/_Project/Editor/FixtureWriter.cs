using System.IO;
using AetherArk.Core;
using AetherArk.Runtime;
using UnityEditor;
using UnityEngine;

namespace AetherArk.Editor
{
    /// <summary>
    /// Writes genuine JsonUtility save files for the current schema into the EditMode fixture folder.
    /// Run once per schema version (Unity -executeMethod AetherArk.Editor.FixtureWriter.WriteSaveFixtures);
    /// the committed files then guard every later migration.
    /// </summary>
    public static class FixtureWriter
    {
        [MenuItem("Aether Ark/Write Save Fixtures")]
        public static void WriteSaveFixtures()
        {
            var root = Path.GetFullPath("Assets/_Project/Tests/EditMode/Fixtures/v3");
            Directory.CreateDirectory(root);
            if (File.Exists(Path.Combine(root, "profile.json")) || File.Exists(Path.Combine(root, "suspended_run.json")))
            {
                Debug.Log("Fixtures already exist; preserving the committed snapshot: " + root);
                return;
            }
            var service = new SaveService(root);

            var profile = new ProfileState
            {
                captainName = "Fixture Captain",
                captainLineage = CrewLineage.Dwarf,
                difficulty = Difficulty.Standard,
                supportShip = SupportShipType.Pathfinder,
                language = Language.English,
                tutorialSeen = true
            };
            profile.accessibility.combatSpeed = 1.25f;
            profile.accessibility.highContrast = true;
            profile.audio.musicVolume = 0.3f;
            profile.audio.effectsVolume = 0.8f;
            profile.audio.muted = true;

            var run = GameSimulation.NewRun(profile, 424242).State;
            // A mid-campaign snapshot: second region, a few jumps in, resources spent, one downed crew member, a damaged room.
            run.regionIndex = 2;
            run.totalTravelCount = 9;
            run.travelCount = 2;
            run.stormColumn = -1;
            run.currentNodeId = "n2_1";
            run.resources.aether = 11;
            run.resources.salvage = 37;
            run.playerShip.hull = 21f;
            run.playerShip.GetSystem(ShipSystemType.Weapons).damage = 30f;
            run.playerShip.GetRoom(ShipSystemType.Weapons).fire = 12f;
            run.crew[4].health = 0f;
            run.crew[4].downedSeconds = 3f;
            run.squadrons[0].strength = 2;

            // V3 fixtures contain run schema 2: a real, paused mid-walk battle for future migrations.
            var simulation = new GameSimulation(run);
            simulation.BeginCombat(1, false);
            simulation.MoveCrew(run.crew.Find(c => c.role == CrewRole.Engineer).id, ShipSystemType.Weapons);
            simulation.SetPaused(false);
            for (var i = 0; i < 3; i++) simulation.Tick(0.1f);
            simulation.SetPaused(true);

            service.SaveProfile(profile);
            service.SaveRun(run);
            new ReproductionStore(root).Capture(run, profile);
            Debug.Log("Save fixtures written to " + root);
            AssetDatabase.Refresh();
        }
    }
}
