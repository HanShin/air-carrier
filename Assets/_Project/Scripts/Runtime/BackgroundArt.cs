using AetherArk.Content;

namespace AetherArk.Runtime
{
    /// <summary>Maps campaign state to Resources paths without coupling UI code to region ids.</summary>
    public static class BackgroundArt
    {
        public const string FallbackPath = "Art/sky_storm_background";
        public const string FinalePath = "Art/Backgrounds/throne_gate_finale";

        public static string ResourcePath(int regionIndex, bool isFinalBattle)
        {
            if (isFinalBattle) return FinalePath;
            if (regionIndex < 1) return FallbackPath;
            return "Art/Backgrounds/" + ContentCatalog.GetRegion(regionIndex).id;
        }
    }
}
