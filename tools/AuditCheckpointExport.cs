using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using AetherArk.Core;
using AetherArk.Runtime;

internal sealed class AuditCheckpointExport
{
    public string Directory;
    public AuditCheckpointMetadata Metadata;
    public string CapturedPath { get; private set; }

    public bool TryCapture(RunState run, ProfileState profile, int battle, int completedTicks)
    {
        if (battle != Metadata.battleOrdinal || completedTicks != Metadata.completedTicks || run.phase != GamePhase.Combat) return false;
        var payload = new CombatSnapshotPayload { run = run, profile = profile, audit = Metadata };
        // Do not pause, clone via GameSimulation, issue commands, or consume any game RNG here.
        CapturedPath = CombatSnapshotFile.Publish(Directory, run.seed, AuditFieldJson.Serialize(payload),
            typeof(GameSimulation).Assembly.ManifestModule.ModuleVersionId.ToString("N"),
            "headless-mono/" + Environment.Version, snapshot => AuditFieldJson.Serialize(snapshot));
        return true;
    }
}

// The state contracts use public serializable fields, not properties or dictionaries. Write exactly those fields
// with numeric enums and round-trip floating point precision for Unity JsonUtility. Reject unsupported additions.
internal static class AuditFieldJson
{
    public static string Serialize(object value)
    {
        var output = new StringBuilder();
        Write(output, value);
        return output.ToString();
    }

    private static void Write(StringBuilder output, object value)
    {
        if (value == null) { output.Append("null"); return; }
        if (value is string text) { Quote(output, text); return; }
        if (value is bool boolean) { output.Append(boolean ? "true" : "false"); return; }
        var type = value.GetType();
        if (type.IsEnum) { output.Append(Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture)); return; }
        if (value is float single)
        {
            if (float.IsNaN(single) || float.IsInfinity(single)) throw new InvalidDataException("Non-finite audit state");
            output.Append(single.ToString("R", CultureInfo.InvariantCulture)); return;
        }
        if (value is int || value is uint || value is long || value is ulong || value is byte || value is short)
        { output.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
        if (value is IList list)
        {
            output.Append('[');
            for (var index = 0; index < list.Count; index++) { if (index > 0) output.Append(','); Write(output, list[index]); }
            output.Append(']'); return;
        }
        if (!type.IsDefined(typeof(SerializableAttribute), false) || type.IsPrimitive || value is IDictionary)
            throw new InvalidDataException("Unsupported audit JSON type: " + type.FullName);
        output.Append('{');
        var first = true;
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.IsNotSerialized || field.IsInitOnly) continue;
            if (!first) output.Append(','); first = false;
            Quote(output, field.Name); output.Append(':'); Write(output, field.GetValue(value));
        }
        output.Append('}');
    }

    private static void Quote(StringBuilder output, string text)
    {
        output.Append('"');
        foreach (var character in text)
        {
            if (character == '"' || character == '\\') output.Append('\\').Append(character);
            else if (character < 32 || char.IsSurrogate(character)) output.Append("\\u").Append(((int)character).ToString("x4"));
            else output.Append(character);
        }
        output.Append('"');
    }
}
