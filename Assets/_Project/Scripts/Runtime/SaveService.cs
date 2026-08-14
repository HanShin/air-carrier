using System;
using System.IO;
using AetherArk.Core;
using UnityEngine;

namespace AetherArk.Runtime
{
    public sealed class SaveService
    {
        private const int CurrentProfileVersion = 1;
        private const int CurrentRunVersion = 1;
        private readonly string profilePath;
        private readonly string runPath;

        public SaveService(string rootPath = null)
        {
            var root = string.IsNullOrEmpty(rootPath) ? Application.persistentDataPath : rootPath;
            Directory.CreateDirectory(root);
            profilePath = Path.Combine(root, "profile.json");
            runPath = Path.Combine(root, "suspended_run.json");
        }

        public ProfileState LoadProfile()
        {
            var profile = Read<ProfileState>(profilePath) ?? new ProfileState();
            profile = MigrateProfile(profile);
            if (profile.accessibility == null) profile.accessibility = new AccessibilitySettings();
            if (profile.unlocks == null) profile.unlocks = new System.Collections.Generic.List<string>();
            return profile;
        }

        public RunState LoadRun()
        {
            var run = Read<RunState>(runPath);
            if (run == null) return null;
            run = MigrateRun(run);
            if (run.phase == GamePhase.Victory || run.phase == GamePhase.Defeat) return null;
            return run;
        }

        public bool HasRun()
        {
            return File.Exists(runPath) && LoadRun() != null;
        }

        public void SaveProfile(ProfileState profile)
        {
            if (profile == null) return;
            profile.schemaVersion = CurrentProfileVersion;
            AtomicWrite(profilePath, JsonUtility.ToJson(profile, true));
        }

        public void SaveRun(RunState run)
        {
            if (run == null || run.phase == GamePhase.Victory || run.phase == GamePhase.Defeat)
            {
                ClearRun();
                return;
            }
            run.schemaVersion = CurrentRunVersion;
            AtomicWrite(runPath, JsonUtility.ToJson(run, true));
        }

        public void ClearRun()
        {
            if (File.Exists(runPath)) File.Delete(runPath);
            var temp = runPath + ".tmp";
            if (File.Exists(temp)) File.Delete(temp);
            var backup = runPath + ".bak";
            if (File.Exists(backup)) File.Delete(backup);
        }

        private static T Read<T>(string path) where T : class
        {
            var source = File.Exists(path) ? path : path + ".bak";
            if (!File.Exists(source)) return null;
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(source));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read save '{source}': {exception.Message}");
                var backup = path + ".bak";
                if (source == backup || !File.Exists(backup)) return null;
                try { return JsonUtility.FromJson<T>(File.ReadAllText(backup)); }
                catch (Exception backupException)
                {
                    Debug.LogWarning($"Could not read backup save '{backup}': {backupException.Message}");
                    return null;
                }
            }
        }

        private static void AtomicWrite(string path, string contents)
        {
            var temp = path + ".tmp";
            File.WriteAllText(temp, contents);
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            var backup = path + ".bak";
            try
            {
                File.Replace(temp, path, backup, true);
            }
            catch (Exception exception) when (exception is PlatformNotSupportedException || exception is IOException)
            {
                File.Copy(path, backup, true);
                File.Delete(path);
                File.Move(temp, path);
            }
        }

        private static ProfileState MigrateProfile(ProfileState profile)
        {
            if (profile.schemaVersion < 1) profile.schemaVersion = 1;
            return profile;
        }

        private static RunState MigrateRun(RunState run)
        {
            if (run.schemaVersion < 1) run.schemaVersion = 1;
            return run;
        }
    }
}
