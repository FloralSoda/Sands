#nullable enable

using System.Collections.Generic;

namespace Sands.logic.tile
{
    public static class TileRegistry
    {
        private static readonly Dictionary<string, Tile> register = new();

        public static readonly string AIR = RegisterTile("sands:air", new Tile());
        public static readonly string SANDBRICK = RegisterTile("sands:sandbrick", new Tile());

        public static string RegisterTile(string id, Tile tile)
        {
            register.Add(id, tile);
            return id;
        }
        public static Tile GetTile(string id)
        {
            return register[id];
        }
    }
}
