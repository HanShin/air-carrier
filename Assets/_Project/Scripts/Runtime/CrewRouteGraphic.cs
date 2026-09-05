using System;
using System.Collections.Generic;
using AetherArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AetherArk.Runtime
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CrewRouteGraphic : MaskableGraphic
    {
        public DeckPlan plan;
        public List<CrewState> crew;
        public string selectedCrewId;
        public Vector2 gridOrigin, cellSize;
        public RunState run;
        private float lastTime = float.NaN;
        private void LateUpdate()
        {
            var time = run?.combatElapsed ?? 0;
            if (time == lastTime) return;
            lastTime = time; SetVerticesDirty();
        }
        private Vector2 Map(float x, float y) => new Vector2(gridOrigin.x + x * cellSize.x, gridOrigin.y + (plan.rows - y) * cellSize.y);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (plan == null) return;
            for (var i = 0; i < plan.tiles.Count; i++)
                for (var j = i + 1; j < plan.tiles.Count; j++)
                {
                    if (!CrewMovementRules.Door(plan.tiles[i], plan.tiles[j], out var x, out var y)) continue;
                    var p = Map(x, y);
                    var horizontal = plan.tiles[i].row + plan.tiles[i].height == plan.tiles[j].row || plan.tiles[j].row + plan.tiles[j].height == plan.tiles[i].row;
                    var open = crew != null && crew.Exists(c => c.IsMoving && CrewMovementRules.Distance(c.movement.x, c.movement.y, x, y) < 0.28f);
                    var half = Math.Min(cellSize.x, cellSize.y) * 0.12f;
                    var delta = horizontal ? new Vector2(half, 0) : new Vector2(0, half);
                    Line(mesh, p - delta, p + delta, open ? 2f : 3.5f, open ? UiFactory.Aether : UiFactory.TextMuted);
                    if (open) Line(mesh, p - delta, p + delta, 1f, UiFactory.Ink);
                }
            var selected = crew?.Find(c => c.id == selectedCrewId);
            if (selected == null || !selected.IsMoving) return;
            var previous = Map(selected.movement.x, selected.movement.y);
            foreach (var point in selected.movement.path)
            {
                var next = Map(point.x, point.y);
                Line(mesh, previous, next, 1.5f, new Color(0.35f, 1, 0.88f, 0.7f)); previous = next;
            }
            Line(mesh, previous - new Vector2(5, 0), previous + new Vector2(5, 0), 2, UiFactory.Brass);
            Line(mesh, previous - new Vector2(0, 5), previous + new Vector2(0, 5), 2, UiFactory.Brass);
        }
        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
        {
            var length = (b - a).magnitude;
            if (length < 0.001f) return;
            var dx = -(b.y - a.y) / length * width * 0.5f; var dy = (b.x - a.x) / length * width * 0.5f;
            var start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(a.x + dx, a.y + dy, 0), color, Vector2.zero); mesh.AddVert(new Vector3(b.x + dx, b.y + dy, 0), color, Vector2.zero);
            mesh.AddVert(new Vector3(b.x - dx, b.y - dy, 0), color, Vector2.zero); mesh.AddVert(new Vector3(a.x - dx, a.y - dy, 0), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
