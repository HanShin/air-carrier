using UnityEngine;

namespace AetherArk.Runtime
{
    /// <summary>Art-space cutaway bounds, independent of simulation room topology.</summary>
    public static class ShipArtLayout
    {
        // Bottom-left UV coordinates, measured against the complete sprite canvas.
        public static bool TryDeckArea(string id, out Rect area)
        {
            switch (id)
            {
                case "ship_vanguard": area = new Rect(0.245f, 0.33f, 0.53f, 0.35f); return true;
                case "ship_bastion": area = new Rect(0.26f, 0.32f, 0.52f, 0.37f); return true;
                case "ship_zephyr": area = new Rect(0.22f, 0.34f, 0.56f, 0.34f); return true;
                case "enemy_cutter": area = new Rect(0.23f, 0.33f, 0.51f, 0.34f); return true;
                case "enemy_cruiser": area = new Rect(0.265f, 0.34f, 0.47f, 0.35f); return true;
                case "enemy_warden": area = new Rect(0.275f, 0.38f, 0.435f, 0.27f); return true;
                default: area = new Rect(); return false;
            }
        }

        public static Rect FitSprite(Vector2 canvas, Vector2 source)
        {
            var scale = Mathf.Min(canvas.x / Mathf.Max(1f, source.x), canvas.y / Mathf.Max(1f, source.y));
            var width = source.x * scale;
            var height = source.y * scale;
            return new Rect((canvas.x - width) / 2f, (canvas.y - height) / 2f, width, height);
        }
    }
}
