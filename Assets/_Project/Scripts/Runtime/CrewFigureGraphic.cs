using System;
using AetherArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AetherArk.Runtime
{
    /// <summary>Articulated top-down crew. Simulation time, not UI lifetime, drives every pose.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CrewFigureGraphic : MaskableGraphic
    {
        public CrewState crew;
        public ShipState ship;
        public RunState run;
        public Vector2 gridOrigin, cellSize;
        public int rows;
        public bool selected, reducedMotion, highContrast;
        public CrewActivity CurrentActivity => CrewMovementRules.Activity(crew, ship);
        public float PoseClock => reducedMotion ? 0f : CurrentActivity == CrewActivity.Walking ? crew.movement.distanceWalked * 11f : (run?.combatElapsed ?? 0f) * 6f;
        private float lastClock = float.NaN;
        private CrewActivity lastActivity;
        private float angle, scale, center;

        public void RefreshPose()
        {
            if (crew?.movement == null) return;
            var m = crew.movement;
            var size = rectTransform.sizeDelta.x;
            rectTransform.anchoredPosition = new Vector2(gridOrigin.x + m.x * cellSize.x - size * 0.5f,
                gridOrigin.y + (rows - m.y) * cellSize.y - size * 0.5f);
            var clock = PoseClock;
            if (clock != lastClock || CurrentActivity != lastActivity)
            { lastClock = clock; lastActivity = CurrentActivity; SetVerticesDirty(); }
        }

        private void LateUpdate() { RefreshPose(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (crew == null || ship == null) return;
            scale = GetPixelAdjustedRect().width / 32f; center = GetPixelAdjustedRect().width * 0.5f;
            angle = crew.movement == null ? 0f : -crew.movement.heading;
            var activity = CurrentActivity;
            var walk = activity == CrewActivity.Walking;
            var down = activity == CrewActivity.Downed;
            var pulse = (float)Math.Sin(PoseClock);
            var stride = walk ? pulse * (crew.health < crew.maxHealth * 0.4f ? 2f : 3.2f) : 0f;
            var skin = ShipBlueprintView.LineageColor(crew.lineage);
            var coat = new Color(skin.r * 0.42f, skin.g * 0.48f, skin.b * 0.50f, 1);
            var dark = new Color(0.025f, 0.04f, 0.06f, 1);
            var light = new Color(0.8f, 0.88f, 0.85f, 1);
            Ellipse(mesh, 0, -1, 12, 8, new Color(0, 0, 0, 0.52f));
            if (selected || crew.isCaptain || highContrast)
                Ring(mesh, 13, selected ? Color.white : crew.isCaptain ? UiFactory.Brass : UiFactory.Aether);
            if (down)
            {
                angle = -0.7f;
                Limb(mesh, -9, -2, 3, 2, 7, coat); Ellipse(mesh, 8, 3, 4, 4, skin);
                Limb(mesh, -10, -4, -4, -7, 3, dark); Limb(mesh, -9, 1, -5, 5, 3, dark);
                angle = 0;
                Limb(mesh, 0, -4, 0, 4, 2.3f, UiFactory.Danger); Limb(mesh, -4, 0, 4, 0, 2.3f, UiFactory.Danger);
                return;
            }
            var width = crew.lineage == CrewLineage.Orc ? 1.22f : crew.lineage == CrewLineage.Dwarf ? 1.15f
                : crew.lineage == CrewLineage.Elf ? 0.83f : crew.lineage == CrewLineage.Goblin ? 0.78f : 1f;
            // Boots lead/follow alternately; shoulder and hand articulation use the opposite phase.
            Limb(mesh, -4, -4 * width, -8 + stride, -4 * width, 4, dark);
            Limb(mesh, -4, 4 * width, -8 - stride, 4 * width, 4, dark);
            Ellipse(mesh, -1, 0, 7, 7 * width, coat);
            Limb(mesh, -3, -5 * width, 1, -5 * width, 2, light);
            Limb(mesh, -3, 5 * width, 1, 5 * width, 2, light);
            var working = activity == CrewActivity.Repairing || activity == CrewActivity.Sealing || activity == CrewActivity.Operating || activity == CrewActivity.Fighting;
            var reach = working ? 7 + pulse * 1.4f : 1 - stride * 0.5f;
            Limb(mesh, 0, -6 * width, reach, -8 * width, 3.3f, coat);
            Limb(mesh, 0, 6 * width, walk ? 1 + stride * 0.5f : 6, 8 * width, 3.3f, coat);
            Ellipse(mesh, reach, -8 * width, 1.9f, 1.9f, skin);
            Ellipse(mesh, walk ? 1 + stride * 0.5f : 6, 8 * width, 1.9f, 1.9f, skin);
            Ellipse(mesh, 4, 0, crew.lineage == CrewLineage.Goblin ? 4.5f : 4, 4.4f, skin);
            // Helmet / hair cap and forward visor establish the facing direction even at overview scale.
            Ellipse(mesh, 2.5f, 0, 3.3f, 4.5f, dark);
            Limb(mesh, 5.7f, -2.5f, 5.7f, 2.5f, 1.5f, light);
            if (crew.lineage == CrewLineage.Elf || crew.lineage == CrewLineage.Goblin)
            { Limb(mesh, 3, -3, 5, -6.5f, 2, skin); Limb(mesh, 3, 3, 5, 6.5f, 2, skin); }
            if (crew.lineage == CrewLineage.Avian)
            { Limb(mesh, -3, -5, -8, -10, 2, skin); Limb(mesh, -3, 5, -8, 10, 2, skin); }
            if (crew.isCaptain) Limb(mesh, -1, -4, -1, 4, 1.7f, UiFactory.Brass);
            if (activity == CrewActivity.Extinguishing)
            {
                Limb(mesh, 2, -8, 9, -6, 4, UiFactory.Danger);
                for (var i = 0; i < 3; i++) Limb(mesh, 10, -6, 14 + i, -10 + i * 4 + pulse, 1.2f, new Color(0.6f, 0.96f, 1, 0.7f));
            }
            if (activity == CrewActivity.Repairing || activity == CrewActivity.Sealing)
            {
                Limb(mesh, reach, -8, reach + 4, -5, 1.5f, light);
                if (!reducedMotion && pulse > 0.45f)
                { Limb(mesh, 12, -5, 15, -3, 1, UiFactory.Brass); Limb(mesh, 12, -5, 13, -1, 1, UiFactory.Aether); }
            }
            if (activity == CrewActivity.Healing)
            { Limb(mesh, -1, -3, -1, 3, 2, UiFactory.Success); Limb(mesh, -4, 0, 2, 0, 2, UiFactory.Success); }
        }

        private Vector3 Point(float x, float y)
        {
            var cos = (float)Math.Cos(angle); var sin = (float)Math.Sin(angle);
            return new Vector3(center + (x * cos - y * sin) * scale, center + (x * sin + y * cos) * scale, 0);
        }
        private void Limb(VertexHelper mesh, float x, float y, float xx, float yy, float width, Color color)
        {
            var length = CrewMovementRules.Distance(x, y, xx, yy);
            if (length < 0.001f) return;
            var dx = -(yy - y) / length * width * 0.5f; var dy = (xx - x) / length * width * 0.5f;
            var start = mesh.currentVertCount;
            mesh.AddVert(Point(x + dx, y + dy), color, Vector2.zero); mesh.AddVert(Point(xx + dx, yy + dy), color, Vector2.zero);
            mesh.AddVert(Point(xx - dx, yy - dy), color, Vector2.zero); mesh.AddVert(Point(x - dx, y - dy), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
        private void Ellipse(VertexHelper mesh, float x, float y, float rx, float ry, Color color)
        {
            var start = mesh.currentVertCount;
            mesh.AddVert(Point(x, y), color, Vector2.zero);
            for (var i = 0; i <= 12; i++)
            {
                var a = i * Math.PI * 2 / 12;
                mesh.AddVert(Point(x + (float)Math.Cos(a) * rx, y + (float)Math.Sin(a) * ry), color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }
        private void Ring(VertexHelper mesh, float radius, Color color)
        {
            for (var i = 0; i < 20; i++)
            {
                var a = i * Math.PI * 2 / 20; var b = (i + 1) * Math.PI * 2 / 20;
                Limb(mesh, (float)Math.Cos(a) * radius, (float)Math.Sin(a) * radius, (float)Math.Cos(b) * radius, (float)Math.Sin(b) * radius, selected ? 1.4f : 0.8f, color);
            }
        }
    }
}
