using AetherArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AetherArk.Runtime
{
    /// <summary>One UI mesh per room: recessed plating, edge lighting and functional fittings.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeckSurfaceGraphic : MaskableGraphic
    {
        public ShipSystemType systemType;
        public Color accent;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r = GetPixelAdjustedRect();
            var w = r.width;
            var h = r.height;
            var steel = new Color(0.36f, 0.42f, 0.44f, 0.3f);
            var shadow = new Color(0f, 0.01f, 0.025f, 0.8f);
            // Bevels, a recessed floor seam and a restrained powered wall strip.
            Quad(mesh, 1, h - 2, w - 2, 1, steel);
            Quad(mesh, 1, 1, 1, h - 2, steel);
            Quad(mesh, w - 2, 1, 1, h - 2, shadow);
            Quad(mesh, 1, 1, w - 2, 2, shadow);
            Quad(mesh, 4, h * 0.45f, w - 8, 1, new Color(0.45f, 0.51f, 0.54f, 0.13f));
            Quad(mesh, 4, h - 5, w - 8, 2, accent);
            if (h < 64f || w < 48f) return;

            // Fittings stay in the lower right; labels and crew have their own reserved zones.
            var x = Mathf.Max(4f, w - 25f);
            var y = 10f;
            switch (systemType)
            {
                case ShipSystemType.FlightDeck:
                    if (w < 70f || h < 65f) break;
                    Quad(mesh, w * 0.55f, 12, 2, h - 42, steel);
                    Quad(mesh, w * 0.84f, 12, 2, h - 42, steel);
                    for (var i = 0; i < 3; i++)
                        Quad(mesh, w * 0.69f, 14 + i * (h - 44) / 3f, 2, 5, accent);
                    break;
                case ShipSystemType.Infirmary:
                    Quad(mesh, x + 7, y, 5, 17, steel);
                    Quad(mesh, x + 1, y + 6, 17, 5, steel);
                    break;
                case ShipSystemType.AetherCore:
                case ShipSystemType.Ward:
                case ShipSystemType.LiftArray:
                    Quad(mesh, x, y, 19, 18, shadow);
                    Quad(mesh, x + 3, y + 3, 13, 12, steel);
                    Quad(mesh, x + 7, y + 5, 5, 8, accent);
                    break;
                default:
                    Quad(mesh, x, y, 19, 14, shadow);
                    Quad(mesh, x + 2, y + 8, 15, 4, accent);
                    Quad(mesh, x + 3, y + 3, 4, 2, steel);
                    Quad(mesh, x + 11, y + 3, 4, 2, steel);
                    break;
            }
        }

        private static void Quad(VertexHelper mesh, float x, float y, float w, float h, Color color)
        {
            if (w <= 0f || h <= 0f) return;
            var start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y, 0), color, Vector2.zero);
            mesh.AddVert(new Vector3(x, y + h, 0), color, Vector2.zero);
            mesh.AddVert(new Vector3(x + w, y + h, 0), color, Vector2.zero);
            mesh.AddVert(new Vector3(x + w, y, 0), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
