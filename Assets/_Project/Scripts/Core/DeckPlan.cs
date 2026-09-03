using System;
using System.Collections.Generic;

namespace AetherArk.Core
{
    [Serializable]
    public sealed class DeckTile
    {
        public ShipSystemType system;
        public int column;
        public int row;
        public int width = 1;
        public int height = 1;
    }

    [Serializable]
    public sealed class DeckPlan
    {
        public string shipId;
        public int columns;
        public int rows;
        public List<DeckTile> tiles = new List<DeckTile>();

        public DeckTile GetTile(ShipSystemType system)
        {
            return tiles.Find(tile => tile.system == system);
        }
    }
}
