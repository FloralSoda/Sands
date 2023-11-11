#nullable enable
using Godot;

namespace Sands.logic.world.generators
{
    public class RepetitionGenerator : IWorldGenerator
    {
        public Chunk ToRepeat;

        public RepetitionGenerator(Chunk ToRepeat)
        {
            this.ToRepeat = ToRepeat;
        }

        public Chunk GenerateChunk(WorldData data, Vector2I location)
        {
            return ToRepeat.Clone();
        }
    }
}
