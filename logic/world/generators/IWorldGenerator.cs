#nullable enable
using Godot;

namespace Sands.logic.world.generators
{
    public interface IWorldGenerator
    {
        public Chunk GenerateChunk(WorldData data, Vector2I location);
    }
}
