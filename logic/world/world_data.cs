#nullable enable

using Godot;
using Sands.logic.tile;
using Sands.logic.world.generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sands.logic.world
{
    /// <summary>
    /// A segment of the world
    /// </summary>
    public class Chunk
    {
        private static HashSet<byte>? available_ids_default;

        private readonly HashSet<byte> available_ids;
        private readonly Dictionary<byte, string> local_ids;
        private readonly Dictionary<string, byte> id_reference;
        private readonly Dictionary<byte, uint> tile_quantities;
        private readonly byte[,,] blockMap;
        /// <summary>
        /// The size of one horizontal edge of a chunk
        /// </summary>
        public const byte EdgeLength = 32;

        private Chunk(HashSet<byte> available_ids, Dictionary<byte, string> local_ids, Dictionary<string, byte> id_reference, Dictionary<byte, uint> tile_quantities, byte[,,] blockMap) {
            this.available_ids = available_ids;
            this.local_ids = local_ids;
            this.id_reference = id_reference;
            this.tile_quantities = tile_quantities;
            this.blockMap = blockMap;
        }
        /// <summary>
        /// Creates an empty Chunk
        /// </summary>
        /// <param name="worldHeight">The height of the world in tiles</param>
        public Chunk(byte worldHeight)
        {
            if (available_ids_default == null) //Generate this list only once for all chunks
            {
                available_ids_default = new(byte.MaxValue);
                for (byte i = 1; i < byte.MaxValue; i++)
                    available_ids_default.Add(i);
                available_ids_default.Add(byte.MaxValue);
            }
            available_ids = new(available_ids_default);

            blockMap = new byte[EdgeLength, worldHeight, EdgeLength];
            local_ids = new(byte.MaxValue)
        {
            { 0, TileRegistry.AIR }
        };
            id_reference = new(byte.MaxValue)
            {
                { TileRegistry.AIR, 0 }
            };
            tile_quantities = new(byte.MaxValue) {
                { 0, (uint)EdgeLength*worldHeight*EdgeLength }
            };
        }
        /// <summary>
        /// Creates a chunk filled with the tile with the defined id
        /// </summary>
        /// <param name="id">The id of the tile to fill the chunk with</param>
        public Chunk(string id, byte worldHeight) : this(worldHeight)
        {
            local_ids[0] = id; //It's faster to just move the id than to place that entire region iteratively
            local_ids[1] = TileRegistry.AIR;
        }
        /// <summary>
        /// Gets the quantity of remaining local ids before a chunk cannot accept any more tile types
        /// </summary>
        /// <returns>The number of unused local ids</returns>
        public int GetRemainingIdCount()
        {
            return available_ids.Count;
        }
        /// <summary>
        /// Returns the tile id associated with the local id provided
        /// </summary>
        /// <param name="id">The local id to retrieve the tile id for</param>
        /// <returns>The tile id related to the local id. Null if that string is not found</returns>
        public string? GetTileIdOf(byte id) {
            if (local_ids.ContainsKey(id))
                return local_ids[id];
            else
                return null;
        }
        /// <summary>
        /// Returns what local numeric id is assigned to the tile provided.
        /// </summary>
        /// <param name="id">The id of the tile to get the local id of</param>
        /// <returns>The local numeric id related to the string. Returns null if that string is not found</returns>
        public byte? GetLocalIdOf(string id)
        {
            if (id_reference.ContainsKey(id))
                return id_reference[id];
            else
                return null;
        }
        /// <summary>
        /// Attempts to assign a local id to the provided tile id
        /// </summary>
        /// <param name="id">The tile id to assign a local id</param>
        /// <returns>The id given to the tile id. Returns <see langword="null"/> if none was given (No available ids, or string already had an id)</returns>
        public byte? GiveLocalId(string id)
        {
            if (!available_ids.Any())
                return null;
            if (!id_reference.ContainsKey(id))
            {
                byte value = available_ids.First();
                available_ids.Remove(value);
                id_reference[id] = value;
                local_ids[value] = id;
                return value;
            }

            return null;
        }
        /// <summary>
        /// Gets the string id of the tile located at the relative coordinates provided
        /// </summary>
        /// <param name="pos">The position to get the tile id of</param>
        /// <returns>The tile id at the position specified</returns>
        public string GetTileAt(Vector3I pos)
        {
            return local_ids[blockMap[pos.X, pos.Y, pos.Z]];
        }
        /// <summary>
        /// Finds the first instance of the tile in the map. Searches Z-Y-X
        /// </summary>
        /// <param name="id">The id of the tile to find</param>
        /// <returns>The coordinates of the first instance of that tile found</returns>
        public (int, int, int) GetTilePos(string tile)
        {
            byte? id_nullable = GetLocalIdOf(tile);
            if (id_nullable == null)
                return (-1, -1, -1);
            byte id = id_nullable.Value;

            for (int i = blockMap.GetLowerBound(0); i <= blockMap.GetUpperBound(0); i++)
                for (int j = blockMap.GetLowerBound(1); j <= blockMap.GetUpperBound(1); j++)
                    for (int k = blockMap.GetLowerBound(2); k <= blockMap.GetUpperBound(2); k++)
                        if (blockMap[i,j, k] == id)
                            return (i, j, k);
            return (-1, -1, -1);
        }
        /// <summary>
        /// Decreases the tracker for the amount of tiles in a chunk. 
        /// </summary>
        /// <param name="id">The id of the tile to decrease the count for</param>
        /// <returns>The amount of tiles remaining after decrement. 0 means there are no more of that tile</returns>
        private uint decreaseTile(byte id, uint count = 1) {
            uint after_decrement = tile_quantities[id] -= count;
            if (after_decrement == 0)
            {
                tile_quantities.Remove(id);
                string tile = local_ids[id];
                local_ids.Remove(id);
                id_reference.Remove(tile);
                available_ids.Add(id);
                return 0;
            }
            return after_decrement;
        }
        /// <summary>
        /// Increases the tracker for the amount of tiles in a chunk. 
        /// </summary>
        /// <param name="id">The id of the tile to increase the count for</param>
        /// <returns>The amount of tiles remaining after increment</returns>
        private uint increaseTile(byte id, uint count = 1)
        {
            return tile_quantities[id] += count;
        }
        /// <summary>
        /// Gets the amount of a certain tile within the chunk. Near zero cost
        /// </summary>
        /// <param name="id">The tile id of the tile to look for</param>
        /// <returns>The quantity of the specified tile within this chunk. 0 means it is not in the chunk</returns>
        public uint GetTileCount(string id)
        {
            if (id_reference.ContainsKey(id))
                return tile_quantities[id_reference[id]];
            else
                return 0;
        }
        /// <summary>
        /// Gets the amount of a certain tile within the chunk. Near zero cost
        /// </summary>
        /// <param name="id">The local id of the tile to look for</param>
        /// <returns>The quantity of the specified tile within this chunk. 0 means it is not in the chunk</returns>
        public uint GetTileCount(byte id)
        {
            if (tile_quantities.TryGetValue(id, out uint quantity))
                return quantity;
            else
                return 0;
        }
        /// <summary>
        /// Sets the tile at the specified location to the id specified
        /// </summary>
        /// <param name="pos">The location to alter</param>
        /// <param name="id">The id of the new tile</param>
        /// <returns><see langword="true"/> if the tile was succesfully set. Returns <see langword="false"/> if the chunk would have too many unique tile types after the operation</returns>
        public bool SetTileAt(Vector3I pos, string id)
        {
            byte? local_id = GetLocalIdOf(id) ?? GiveLocalId(id);

            if (local_id == null)
            {
                if (GetTileCount(blockMap[pos.X, pos.Y, pos.Z]) == 1)
                {
                    local_ids[blockMap[pos.X, pos.Y, pos.Z]] = id;
                    return true;
                } else
                    return false;
            }

            decreaseTile(blockMap[pos.X, pos.Y, pos.Z]);
            blockMap[pos.X, pos.Y, pos.Z] = (byte)local_id;
            increaseTile((byte)local_id);

            return true;
        }
        /// <summary>
        /// Sets a boxed region as a single type of tile. Optimised.
        /// </summary>
        /// <param name="start">The first corner of the selection</param>
        /// <param name="end">The diagonally opposite corner of the selection</param>
        /// <param name="id">The id of the tile to fill the region with</param>
        /// <returns><see langword="true"/> if the region was succesfully set. Returns <see langword="false"/> if the chunk would have too many unique tile types after the operation</returns>
        public bool SetRegion(Vector3I start, Vector3I end, string id)
        {
            byte? local_id_nullable = GetLocalIdOf(id) ?? GiveLocalId(id);

            //This is only good at 8 bit ids. Change this if ids get moved to 16 bit
            uint[] destroy_count = new uint[byte.MaxValue];

            Vector3I from = new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
            Vector3I to = new(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y), Math.Max(start.Z, end.Z));

            if (local_id_nullable == null)
            {
                //Must count tile loss first, to check if there is an id that can be simply replaced.
                for (int x = from.X; x < to.X; ++x)
                    for (int y = from.Y; y < to.Y; ++y)
                        for (int z = from.Z; z < to.Z; ++z)
                            ++destroy_count[blockMap[x, y, z]];
                byte best_id = 0;
                uint best_count = 0;

                for (byte i = 0; i < destroy_count.Length; ++i)
                {
                    uint to_destroy = destroy_count[i];
                    if (to_destroy > 0) {
                        uint count = GetTileCount(i);
                        if (count <= to_destroy && count > best_count)
                        {
                            best_count = count;
                            best_id = i;
                        }
                    }
                }
                if (best_count > 0)
                    local_ids[best_id] = id;
                else
                    return false; //Could not replace any of the tile types in the region

                for (int x = from.X; x < to.X; ++x)
                    for (int y = from.Y; y < to.Y; ++y)
                        for (int z = from.Z; z < to.Z; ++z)
                            blockMap[x, y, z] = best_id;

                for (byte i = 0; i < destroy_count.Length; ++i)
                    if (i != best_id && destroy_count[i] > 0)
                        decreaseTile(i, destroy_count[i]);
                tile_quantities[best_id] = (uint)((to.X - from.X) * (to.Y - from.Y) * (from.Z - from.Z));
                return true;
            } else
            {
                //Can update tile order live as there is a free id slot to use as buffer
                byte local_id = (byte)local_id_nullable;

                for (int x = from.X; x <= to.X; ++x)
                    for (int y = from.Y; y <= to.Y; ++y)
                        for (int z = from.Z; z <= to.Z; ++z)
                        {
                            if (blockMap[x, y, z] != local_id)
                            {
                                ++destroy_count[blockMap[x, y, z]];
                                blockMap[x, y, z] = local_id;
                            }
                        }

                for (byte i = 0; i < destroy_count.Length; ++i)
                    if (destroy_count[i] > 0)
                        decreaseTile(i, destroy_count[i]);

                return true;
            }
        }

        public void GetMap() {
            throw new NotImplementedException("High performance map iteration is not yet implemented");
        }

        /// <summary>
        /// Creates a new chunk with the exact same contents as this chunk
        /// </summary>
        /// <returns>A new chunk with identical contents</returns>
        public Chunk Clone() {
            HashSet<byte> ids = new(available_ids);
            Dictionary<byte, string> locals = new(local_ids);
            Dictionary<string, byte> refs = new(id_reference);
            Dictionary<byte, uint> quants = new(tile_quantities);
            byte[,,] map = new byte[blockMap.GetLength(0),blockMap.GetLength(1),blockMap.GetLength(2)];
            for (int x = 0; x < map.GetLength(0); ++x)
                for (int y = 0; y < map.GetLength(1); ++y)
                    for (int z = 0; z < map.GetLength(2); ++z)
                        map[x, y, z] = blockMap[x, y, z];


            return new(ids, locals, refs, quants, map);
        }
    }

    /// <summary>
    /// The data of a world
    /// </summary>
    public partial class WorldData
    {
        private readonly Dictionary<Vector2I, Chunk> worldData;
        /// <summary>
        /// The generator used for producing new chunks
        /// </summary>
        public IWorldGenerator WorldGenerator;
        /// <summary>
        /// The seed used for all randomisation on the seed
        /// </summary>
        public ulong Seed;
        /// <summary>
        /// The height of this world in tiles
        /// </summary>
        public byte WorldHeight;

        /// <summary>
        /// Build a world from premade chunk data
        /// </summary>
        /// <param name="worldData">The chunk data to load</param>
        public WorldData(Dictionary<Vector2I, Chunk> worldData, IWorldGenerator worldGenerator, byte worldHeight)
        {
            this.worldData = worldData;
            WorldGenerator = worldGenerator;
            WorldHeight = worldHeight;
        }

        /// <summary>
        /// Build a world from a data stream serving bytes. Does not close provided stream
        /// </summary>
        /// <param name="stream">The stream to read from. Assumes world data starts at current header position</param>
        public WorldData(Stream stream, IWorldGenerator worldGenerator, byte worldHeight)
        {
            throw new NotImplementedException("World data format is still WIP");
        }

        /// <summary>
        /// Generate a new world from the seed.
        /// </summary>
        /// <param name="seed">The seed to start the world with</param>
        public WorldData(ulong seed, IWorldGenerator worldGenerator, byte worldHeight)
        {
            Seed = seed;
            WorldGenerator = worldGenerator;
            worldData = new();
            WorldHeight = worldHeight;
        }

        private Chunk generateChunkAt(Vector2I pos)
        {
            Chunk newChunk = WorldGenerator.GenerateChunk(this, pos);
            worldData[pos] = newChunk;

            return newChunk;
        }

        /// <summary>
        /// Tries to get the chunk at the specified position. If none is found, a new chunk is generated
        /// </summary>
        /// <param name="pos">The chunk coordinates to request the chunk for</param>
        /// <returns>A populated chunk that resides at the specified coordinates. May be freshly generated</returns>
        public Chunk RequestChunkAt(Vector2I pos)
        {
            if (worldData.TryGetValue(pos, out var chunk))
                return chunk;
            else
                return generateChunkAt(pos);
        }
        /// <summary>
        /// Tries to get the chunk at the specified position.
        /// </summary>
        /// <param name="pos">The chunk coordinates to request the chunk for</param>
        /// <returns>A populated chunk that resides at the specified coordinates. May return <see langword="null"/> if the chunk has not been rendered yet</returns>
        public Chunk? GetChunkAt(Vector2I pos)
        {
            if (worldData.TryGetValue(pos, out var chunk))
                return chunk;
            else
                return null;
        }
    }
}
