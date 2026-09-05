using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using AetherArk.Content;
using AetherArk.Core;
using UnityEngine;

namespace AetherArk.Runtime
{
    [Serializable]
    public sealed class CombatSnapshot
    {
        public string format;
        public int formatVersion;
        public string createdUtc;
        public string simulationBuild;
        public string unityVersion;
        public string payloadJson;
        public string sha256;
    }

    [Serializable]
    public sealed class CombatSnapshotPayload
    {
        public RunState run;
        public ProfileState profile;
    }

    /// <summary>Immutable, checksummed combat captures. Never accesses the normal profile or suspended run.</summary>
    public sealed class ReproductionStore
    {
        public const string Format = "aether-ark-combat-snapshot";
        public const int FormatVersion = 1;
        public const long MaxSnapshotBytes = 8 * 1024 * 1024;
        public static string SimulationBuild => typeof(GameSimulation).Assembly.ManifestModule.ModuleVersionId.ToString("N");
        public string RootPath { get; }
        public string SnapshotDirectory => Path.Combine(RootPath, "snapshots");
        public string SessionDirectory => Path.Combine(RootPath, "session");

        public ReproductionStore(string rootPath) { RootPath = Path.GetFullPath(rootPath); }

        public static bool TrySeed(string text, out int seed) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);

        public static ProfileState SeedProfile(ProfileState source, string flagship, Difficulty difficulty)
        {
            if (source == null || string.IsNullOrEmpty(flagship) || ContentCatalog.GetFlagship(flagship) == null || !Enum.IsDefined(typeof(Difficulty), difficulty))
                throw new InvalidDataException("repro.invalid_config");
            var profile = JsonUtility.FromJson<ProfileState>(JsonUtility.ToJson(source));
            profile.captainName = "Audit Captain";
            profile.captainLineage = CrewLineage.Human;
            profile.supportShip = SupportShipType.Workshop;
            profile.tutorialSeen = true; // Seed 32838 is a full campaign, not an implicit tutorial.
            profile.campaignVictories = Math.Max(1, profile.campaignVictories);
            profile.flagshipId = flagship;
            profile.difficulty = difficulty;
            return profile;
        }

        public string Capture(RunState run, ProfileState profile)
        {
            Validate(new CombatSnapshotPayload { run = run, profile = profile });
            var snapshot = new CombatSnapshot
            {
                format = Format, formatVersion = FormatVersion,
                createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                simulationBuild = SimulationBuild, unityVersion = Application.unityVersion,
                payloadJson = JsonUtility.ToJson(new CombatSnapshotPayload { run = run, profile = profile })
            };
            snapshot.sha256 = Digest(snapshot.payloadJson);
            var json = JsonUtility.ToJson(snapshot, true);
            if (Encoding.UTF8.GetByteCount(json) > MaxSnapshotBytes) throw new InvalidDataException("repro.invalid_snapshot");
            Directory.CreateDirectory(SnapshotDirectory);
            var name = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture) + "-seed-" +
                run.seed.ToString(CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + ".json";
            var path = Path.Combine(SnapshotDirectory, name);
            var temporary = path + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                File.Move(temporary, path); // Same-directory atomic publish; never overwrites an existing capture.
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return path;
        }

        public CombatSnapshotPayload Load(string path, out bool differentBuild)
        {
            differentBuild = false;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new InvalidDataException("repro.snapshot_missing");
            if (new FileInfo(path).Length > MaxSnapshotBytes) throw new InvalidDataException("repro.invalid_snapshot");
            CombatSnapshot snapshot;
            CombatSnapshotPayload payload;
            try
            {
                snapshot = JsonUtility.FromJson<CombatSnapshot>(File.ReadAllText(path));
                if (snapshot == null || snapshot.format != Format) throw new InvalidDataException("repro.invalid_snapshot");
                if (snapshot.formatVersion != FormatVersion) throw new InvalidDataException("repro.unsupported_version");
                if (string.IsNullOrEmpty(snapshot.payloadJson) || string.IsNullOrEmpty(snapshot.sha256) ||
                    !string.Equals(snapshot.sha256, Digest(snapshot.payloadJson), StringComparison.Ordinal))
                    throw new InvalidDataException("repro.invalid_snapshot");
                payload = JsonUtility.FromJson<CombatSnapshotPayload>(snapshot.payloadJson);
            }
            catch (ArgumentException) { throw new InvalidDataException("repro.invalid_snapshot"); }
            Validate(payload);
            differentBuild = snapshot.simulationBuild != SimulationBuild || snapshot.unityVersion != Application.unityVersion || payload.run.schemaVersion < CrewMovementRules.RunVersion;
            CrewMovementRules.Ensure(payload.run);
            return payload; // Exact captured pause/RNG/timers; the UI pauses its own loaded copy for safe inspection.
        }

        public string LatestSnapshot()
        {
            if (!Directory.Exists(SnapshotDirectory)) return string.Empty;
            var paths = Directory.GetFiles(SnapshotDirectory, "*.json");
            Array.Sort(paths, StringComparer.Ordinal);
            return paths.Length == 0 ? string.Empty : paths[paths.Length - 1];
        }

        public static bool Step(GameSimulation simulation)
        {
            if (simulation == null || simulation.State.phase != GamePhase.Combat || !simulation.State.isPaused) return false;
            simulation.SetPaused(false);
            simulation.Tick(0.1f);
            simulation.SetPaused(true);
            return true;
        }

        private static string Digest(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void Validate(CombatSnapshotPayload payload)
        {
            var run = payload?.run; var profile = payload?.profile;
            if (run == null || profile == null) throw new InvalidDataException("repro.invalid_snapshot");
            if ((run.schemaVersion != 1 && run.schemaVersion != CrewMovementRules.RunVersion) || profile.schemaVersion != 2) throw new InvalidDataException("repro.unsupported_version");
            if (run.phase != GamePhase.Combat) throw new InvalidDataException("repro.combat_only");
            if (run.resources == null || run.convoy == null || run.random == null || run.crew == null || run.squadrons == null ||
                run.routeNodes == null || run.weaponSlots == null || run.installedModules == null || run.combatLog == null ||
                profile.accessibility == null || profile.audio == null ||
                !Enum.IsDefined(typeof(Difficulty), run.difficulty) || !Enum.IsDefined(typeof(WeatherType), run.currentWeather))
                throw new InvalidDataException("repro.invalid_snapshot");
            ValidateShip(run.playerShip); ValidateShip(run.enemyShip);
            if (run.random.route == 0 || run.random.combat == 0 || run.random.events == 0 ||
                run.routeNodes.Exists(node => node == null || node.connectedIds == null) || run.combatLog.Exists(entry => entry == null || string.IsNullOrEmpty(entry.key)))
                throw new InvalidDataException("repro.invalid_snapshot");
            ValidateFinite(payload);
            var ids = new HashSet<string>();
            var captain = false;
            foreach (var crew in run.crew)
            {
                if (crew == null || string.IsNullOrEmpty(crew.id) || !ids.Add(crew.id)) throw new InvalidDataException("repro.invalid_snapshot");
                if (!CrewMovementRules.IsValid(crew, ContentCatalog.DeckPlanFor(run.playerShip), run.schemaVersion >= 2))
                    throw new InvalidDataException("repro.invalid_snapshot");
                captain |= crew.isCaptain;
            }
            if (!captain) throw new InvalidDataException("repro.invalid_snapshot");
            ids.Clear();
            foreach (var squadron in run.squadrons)
            {
                if (squadron == null || string.IsNullOrEmpty(squadron.id) || !ids.Add(squadron.id) ||
                    !Enum.IsDefined(typeof(SquadronStatus), squadron.status) || !Enum.IsDefined(typeof(SquadronMission), squadron.mission) ||
                    ((squadron.mission == SquadronMission.Bombard || squadron.mission == SquadronMission.Assault) && run.enemyShip.GetSystem(squadron.targetSystem) == null))
                    throw new InvalidDataException("repro.invalid_snapshot");
            }
            if (run.enemyShip.GetSystem(run.selectedEnemySystem) == null) throw new InvalidDataException("repro.invalid_snapshot");
            foreach (var weapon in run.weaponSlots)
                if (weapon == null || ContentCatalog.GetWeapon(weapon.weaponId) == null) throw new InvalidDataException("repro.invalid_snapshot");
        }

        private static void ValidateShip(ShipState ship)
        {
            if (ship == null || string.IsNullOrEmpty(ship.id) || ship.systems == null || ship.rooms == null || ship.weaponSlots == null)
                throw new InvalidDataException("repro.invalid_snapshot");
            var systemCount = Enum.GetValues(typeof(ShipSystemType)).Length;
            if (ship.systems.Count != systemCount || ship.rooms.Count != systemCount) throw new InvalidDataException("repro.invalid_snapshot");
            foreach (ShipSystemType type in Enum.GetValues(typeof(ShipSystemType)))
                if (ship.systems.FindAll(system => system != null && system.type == type).Count != 1 ||
                    ship.rooms.FindAll(room => room != null && room.system == type).Count != 1)
                    throw new InvalidDataException("repro.invalid_snapshot");
            if (ship.systems.Exists(system => system == null) || ship.rooms.Exists(room => room == null))
                throw new InvalidDataException("repro.invalid_snapshot");
            foreach (var weapon in ship.weaponSlots)
                if (weapon == null || ContentCatalog.GetWeapon(weapon.weaponId) == null) throw new InvalidDataException("repro.invalid_snapshot");
        }

        private static void ValidateFinite(object value)
        {
            if (value == null || value is string) return;
            if (value is float number && (float.IsNaN(number) || float.IsInfinity(number)))
                throw new InvalidDataException("repro.invalid_snapshot");
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum) return;
            if (value is IEnumerable list) { foreach (var item in list) ValidateFinite(item); return; }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) ValidateFinite(field.GetValue(value));
        }
    }
}
