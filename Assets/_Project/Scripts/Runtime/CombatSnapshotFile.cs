using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AetherArk.Core;

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
        public AuditCheckpointMetadata audit;
    }

    // Optional provenance, inside the checksummed payload. This is not saved autoplay policy memory.
    [Serializable]
    public sealed class AuditCheckpointMetadata
    {
        public string producer;
        public string sourceSha256;
        public int battleOrdinal;
        public int completedTicks;
        public float fixedDelta;
        public string boundary;
        public string strategy;
        public string wingPolicy;
        public string forcedEnemy;
        public float combatCap;
    }

    /// <summary>Unity-independent envelope and atomic, non-overwriting publication shared with the audit CLI.</summary>
    public static class CombatSnapshotFile
    {
        public const string Format = "aether-ark-combat-snapshot";
        public const int FormatVersion = 1;
        public const long MaxSnapshotBytes = 8 * 1024 * 1024;

        public static string Publish(string directory, int seed, string payloadJson, string build, string runtime,
            Func<CombatSnapshot, string> serialize)
        {
            var now = DateTime.UtcNow;
            var snapshot = new CombatSnapshot
            {
                format = Format, formatVersion = FormatVersion,
                createdUtc = now.ToString("O", CultureInfo.InvariantCulture),
                simulationBuild = build, unityVersion = runtime,
                payloadJson = payloadJson, sha256 = Digest(payloadJson)
            };
            var bytes = Encoding.UTF8.GetBytes(serialize(snapshot));
            if (bytes.Length > MaxSnapshotBytes) throw new InvalidDataException("repro.invalid_snapshot");
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            var name = now.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture) + "-seed-" +
                seed.ToString(CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + ".json";
            var path = Path.Combine(directory, name);
            var temporary = path + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return path;
        }

        public static string Digest(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
