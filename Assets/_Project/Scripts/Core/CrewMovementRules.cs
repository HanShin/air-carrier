using System;
using System.Collections.Generic;
using AetherArk.Content;

namespace AetherArk.Core
{
    [Serializable]
    public sealed class CrewWaypoint
    {
        public float x, y;
        public ShipSystemType room;
        public CrewWaypoint(float x, float y, ShipSystemType room) { this.x = x; this.y = y; this.room = room; }
    }

    [Serializable]
    public sealed class CrewMovementState
    {
        public float x, y, speed, heading, distanceWalked;
        public ShipSystemType destination;
        public List<CrewWaypoint> path = new List<CrewWaypoint>();
    }

    public enum CrewActivity { Operating, Walking, Repairing, Extinguishing, Sealing, Fighting, Healing, Downed, Idle }

    /// <summary>Deck-cell coordinates, top to bottom. Adjoining rooms share a doorway at the midpoint of their shared edge.</summary>
    public static class CrewMovementRules
    {
        public const int RunVersion = 2;
        private const float Acceleration = 5f;

        public static bool Door(DeckTile a, DeckTile b, out float x, out float y)
        {
            x = y = 0f;
            if (a == null || b == null || a == b) return false;
            var low = Math.Max(a.row, b.row); var high = Math.Min(a.row + a.height, b.row + b.height);
            if (high > low && (a.column + a.width == b.column || b.column + b.width == a.column))
            { x = a.column + a.width == b.column ? b.column : a.column; y = (low + high) * 0.5f; return true; }
            low = Math.Max(a.column, b.column); high = Math.Min(a.column + a.width, b.column + b.width);
            if (high > low && (a.row + a.height == b.row || b.row + b.height == a.row))
            { x = (low + high) * 0.5f; y = a.row + a.height == b.row ? b.row : a.row; return true; }
            return false;
        }

        public static CrewWaypoint Station(DeckPlan plan, ShipSystemType room, int slot)
        {
            var tile = plan.GetTile(room);
            // Stable berths across redraws and roster changes; up to eight crew remain visible in one room.
            var column = slot % 3; var row = (slot / 3) % 3;
            return new CrewWaypoint(tile.column + tile.width * (0.22f + column * 0.28f),
                tile.row + tile.height * (0.40f + row * 0.23f), room);
        }

        public static void Ensure(RunState run)
        {
            if (run?.crew == null || run.playerShip == null) return;
            var plan = ContentCatalog.DeckPlanFor(run.playerShip);
            if (plan == null) return;
            for (var i = 0; i < run.crew.Count; i++)
            {
                var crew = run.crew[i];
                // JsonUtility may materialize an absent nested object as zeroes. V1 never had movement.
                if (run.schemaVersion >= RunVersion && crew.movement != null) continue;
                var point = Station(plan, crew.currentRoom, i);
                crew.movement = new CrewMovementState { x = point.x, y = point.y, destination = crew.currentRoom };
            }
            if (run.schemaVersion < RunVersion) run.schemaVersion = RunVersion;
        }

        public static List<CrewWaypoint> Path(DeckPlan plan, CrewState crew, ShipSystemType target, int slot)
        {
            var result = new List<CrewWaypoint>();
            if (plan?.GetTile(crew.currentRoom) == null || plan.GetTile(target) == null) return result;
            var queue = new Queue<ShipSystemType>();
            var parent = new Dictionary<ShipSystemType, ShipSystemType>();
            queue.Enqueue(crew.currentRoom); parent[crew.currentRoom] = crew.currentRoom;
            while (queue.Count > 0 && !parent.ContainsKey(target))
            {
                var room = queue.Dequeue();
                foreach (var tile in plan.tiles)
                {
                    if (parent.ContainsKey(tile.system) || !Door(plan.GetTile(room), tile, out _, out _)) continue;
                    parent[tile.system] = room; queue.Enqueue(tile.system);
                }
            }
            if (!parent.ContainsKey(target)) return result;
            var rooms = new List<ShipSystemType> { target };
            while (rooms[rooms.Count - 1] != crew.currentRoom) rooms.Add(parent[rooms[rooms.Count - 1]]);
            rooms.Reverse();
            for (var i = 1; i < rooms.Count; i++)
            {
                var from = plan.GetTile(rooms[i - 1]); var to = plan.GetTile(rooms[i]);
                // Convex room interiors keep the straight segments within walls; crossings are only at doors.
                Door(from, to, out var x, out var y);
                result.Add(new CrewWaypoint(x, y, to.system));
            }
            result.Add(Station(plan, target, slot));
            return result;
        }

        public static float WalkingSpeed(CrewState crew)
        {
            var speed = crew.lineage == CrewLineage.Dwarf ? 1.05f : crew.lineage == CrewLineage.Goblin ? 1.8f
                : crew.lineage == CrewLineage.Orc ? 1.25f : crew.lineage == CrewLineage.Avian ? 1.65f : 1.5f;
            return speed * (crew.health < crew.maxHealth * 0.4f ? 0.6f : 1f);
        }

        public static void Stop(CrewState crew)
        {
            if (crew.movement == null) return;
            crew.movement.path.Clear(); crew.movement.speed = 0f; crew.movement.destination = crew.currentRoom;
        }

        public static void Tick(CrewState crew, float dt)
        {
            if (!crew.IsActive) { Stop(crew); return; }
            if (!crew.IsMoving) return;
            var m = crew.movement;
            var remaining = 0f; var x = m.x; var y = m.y;
            foreach (var point in m.path) { remaining += Distance(x, y, point.x, point.y); x = point.x; y = point.y; }
            var desired = Math.Min(WalkingSpeed(crew), (float)Math.Sqrt(2f * Acceleration * remaining));
            m.speed = Math.Max(0.08f, m.speed + Math.Max(-Acceleration * dt, Math.Min(Acceleration * dt, desired - m.speed)));
            var travel = m.speed * dt;
            while (travel > 0f && m.path.Count > 0)
            {
                var point = m.path[0]; var length = Distance(m.x, m.y, point.x, point.y);
                var amount = Math.Min(length, travel);
                if (length > 0.00001f)
                {
                    var angle = (float)Math.Atan2(point.y - m.y, point.x - m.x);
                    var difference = (float)Math.Atan2(Math.Sin(angle - m.heading), Math.Cos(angle - m.heading));
                    m.heading += difference * Math.Min(1f, dt * 12f);
                    m.x += (point.x - m.x) * amount / length; m.y += (point.y - m.y) * amount / length;
                }
                travel -= amount; m.distanceWalked += amount;
                if (length <= amount + 0.00001f)
                { m.x = point.x; m.y = point.y; crew.currentRoom = point.room; m.path.RemoveAt(0); }
                else break;
            }
            if (!crew.IsMoving) m.speed = 0f;
        }

        public static float Distance(float x, float y, float xx, float yy) => (float)Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y));

        public static bool IsValid(CrewState crew, DeckPlan plan, bool required)
        {
            if (plan?.GetTile(crew.currentRoom) == null) return false;
            if (!required) return true; // V1 movement fields are discarded by migration, never interpreted as a route.
            var m = crew.movement;
            if (m == null) return !required;
            if (m.path == null || m.path.Count > 40 || m.speed < 0 || m.speed > 4 || m.distanceWalked < 0 || plan.GetTile(m.destination) == null) return false;
            var tile = plan.GetTile(crew.currentRoom);
            if (!Contains(tile, m.x, m.y)) return false;
            var room = crew.currentRoom;
            foreach (var point in m.path)
            {
                if (point == null || !Contains(plan.GetTile(point.room), point.x, point.y)) return false;
                if (point.room != room)
                {
                    if (!Door(plan.GetTile(room), plan.GetTile(point.room), out var dx, out var dy) || Distance(point.x, point.y, dx, dy) > 0.001f) return false;
                }
                room = point.room;
            }
            return m.path.Count == 0 || (room == m.destination && crew.IsActive);
        }

        private static bool Contains(DeckTile tile, float x, float y) => tile != null && x >= tile.column - 0.001f &&
            x <= tile.column + tile.width + 0.001f && y >= tile.row - 0.001f && y <= tile.row + tile.height + 0.001f;

        public static CrewActivity Activity(CrewState crew, ShipState ship)
        {
            if (crew.IsDowned || crew.isDead) return CrewActivity.Downed;
            if (crew.IsMoving) return CrewActivity.Walking;
            var room = ship.GetRoom(crew.currentRoom); var system = ship.GetSystem(crew.currentRoom);
            if (room.intruders > 0) return CrewActivity.Fighting;
            if (room.fire > 0.5f) return CrewActivity.Extinguishing;
            if (room.breach > 0.5f) return CrewActivity.Sealing;
            if (system.damage > 0.5f) return CrewActivity.Repairing;
            if (room.system == ShipSystemType.Infirmary && system.EffectivePower > 0 && crew.health < crew.maxHealth) return CrewActivity.Healing;
            return system.EffectivePower > 0 ? CrewActivity.Operating : CrewActivity.Idle;
        }

        public static void CompleteBetweenBattles(RunState run)
        {
            foreach (var crew in run.crew)
            {
                if (!crew.IsMoving) continue;
                if (crew.IsActive)
                {
                    var last = crew.movement.path[crew.movement.path.Count - 1];
                    crew.movement.x = last.x; crew.movement.y = last.y; crew.currentRoom = last.room;
                }
                Stop(crew);
            }
        }
    }
}
