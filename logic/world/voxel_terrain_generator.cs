#nullable enable

using Godot;
using Sands.logic.tile;
using Sands.logic.world;
using Sands.logic.world.generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[Tool]
public partial class voxel_terrain_generator : MeshInstance3D
{
	//When C# supports annotations, change these descriptions to match those
	[ExportGroup("Buttons")]
	[Export(hintString: "Click this to update the terrain model based on the current world type")]
	public bool Update = false;
	[ExportGroup("World Settings")]
	[Export(hintString: "Tells the terrain builder to automatically hide faces opposite the camera")]
	public bool AssumeIsometric = true;
	[Export(hintString: "How far across the screen horizontally the world should load, in chunks. Used for extreme aspect ratios")]
	public byte XRenderDistance = 1;
	[Export(hintString: "How far across the screen vertically the world should load, in chunks. Used for extreme aspect ratios")] 
	public byte YRenderDistance = 1;
	[Export(hintString: "The maximum height of the current world, in tiles. The player can go above this, but tiles cannot")]
	public byte WorldHeight = 255;
	private World _world = World.Debug;
	[Export(hintString: "The world generator used.")]
	public World World { set
		{
			_world = value;
            if (value == World.Debug)
                LoadDebugMap();
            else
                Debug.WriteLine("Overworld is not yet implemented. Sorry for the inconvenience!");
        }
		get { return _world; }
	}

	float MinHeight = 0;
	float MaxHeight = 1;

	public Vector2I playerChunk = new(0,0);
	public Vector3 playerLocal = new(0,0,0);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	public void LoadDebugMap()
	{
		Debug.WriteLine("Generating Parent Chunk");
		Chunk simple = new(WorldHeight);
		Debug.WriteLine("Filling Region");
		simple.SetRegion(new(0, WorldHeight / 2, 0), new(Chunk.EdgeLength - 1, WorldHeight / 2, Chunk.EdgeLength - 1), TileRegistry.SANDBRICK);
		simple.SetRegion(new(10, (WorldHeight / 2) + 1, 10), new(Chunk.EdgeLength - 10, (WorldHeight / 2) + 1, Chunk.EdgeLength - 10), TileRegistry.SANDBRICK);
		Debug.WriteLine($"Pos: {simple.GetTilePos(TileRegistry.SANDBRICK)}");
		Debug.WriteLine("Producing Generator");
		RepetitionGenerator gen = new(simple);
		Debug.WriteLine("Building World");

		WorldData.SetWorld(new WorldData((ulong)DateTime.Now.Ticks, gen, WorldHeight));
	}

	public void OnPlayerMove(Vector3 new_location) {
		playerLocal = new(new_location.X % Chunk.EdgeLength, new_location.Y, new_location.Z % Chunk.EdgeLength);
		Vector2I newChunk = new((int)Math.Floor(new_location.X / Chunk.EdgeLength), (int)Math.Floor(new_location.Z / Chunk.EdgeLength));
		if (playerChunk != newChunk)
		{
			playerChunk = newChunk;
			GenerateMesh();
		}
	}

	public void Update_Shader()
	{
		ShaderMaterial material = (ShaderMaterial)GetActiveMaterial(0);
		material.SetShaderParameter("min_height", MinHeight);
		material.SetShaderParameter("max_height", MaxHeight);
	}

	public void RenderMesh(HashSet<Vector3I> verts)
	{
		Debug.WriteLine($"Building world ({verts.Count} Vertices)");
		//How to construct a world with just a field of vertices?

        SurfaceTool surface_tool = new();
        surface_tool.Begin(Mesh.PrimitiveType.Triangles);

		Vector3I Right = new(0, 0, 1);
		Vector3I Down = new(1, 0, 0);
		Vector3I DownRight = new(1, 0, 1);
		Vector3I In = new(0, -1, 0);

		Dictionary<Vector3I, int> Loaded = new();

		int load_vertex(Vector3I vertex, Axis axis) {
			if (!Loaded.ContainsKey(vertex))
			{
				switch (axis)
				{
					case Axis.Top:
                        surface_tool.SetUV(new(vertex.X, vertex.Z));
                        break;
					case Axis.Left:
						surface_tool.SetUV(new(vertex.Y, vertex.Z));
						break;
					case Axis.Right:
						surface_tool.SetUV(new(vertex.Y, vertex.X));
						break;
				}
				
				surface_tool.AddVertex(vertex);
				int idx = Loaded.Count;
				Loaded.Add(vertex, idx);
				return idx;
			}
			else
				return Loaded[vertex];
		}

		foreach (Vector3I vertex in verts) {
			//Gather the 4 vertices that make a face
			Vector3I top_left = vertex;
			Vector3I top_right = vertex + Right;
			Vector3I bottom_left = vertex + Down;
			Vector3I bottom_right = vertex + DownRight;
			Vector3I top_right_down = vertex + Right + In;
			Vector3I bottom_right_down = vertex + DownRight + In;
			Vector3I bottom_left_down = vertex + Down + In;

			//We know top_left exists. We check the others

			//Top face
			if (verts.Contains(top_right) && verts.Contains(bottom_left) && verts.Contains(bottom_right)) {
				//Load vertices
				int top_left_idx = load_vertex(top_left, Axis.Top);
				int top_right_idx = load_vertex(top_right, Axis.Top);
				int bottom_left_idx = load_vertex(bottom_left, Axis.Top); 
				int bottom_right_idx = load_vertex(bottom_right, Axis.Top);

				//Tri 1
				surface_tool.AddIndex(top_left_idx);
				surface_tool.AddIndex(bottom_right_idx);
				surface_tool.AddIndex(top_right_idx);
				//Tri 2
				surface_tool.AddIndex(top_left_idx);
				surface_tool.AddIndex(bottom_left_idx);
				surface_tool.AddIndex(bottom_right_idx);
			}
			//Left face
			if (verts.Contains(top_right) && verts.Contains(top_right_down) && verts.Contains(bottom_right) && verts.Contains(bottom_right_down))
			{
				//Load vertices
				int top_right_idx = load_vertex(top_right, Axis.Left);
				int bottom_right_idx = load_vertex(bottom_right, Axis.Left);
				int top_right_down_idx = load_vertex(top_right_down, Axis.Left);
				int bottom_right_down_idx = load_vertex(bottom_right_down, Axis.Left);

				//Tri 1
				surface_tool.AddIndex(top_right_idx);
				surface_tool.AddIndex(top_right_down_idx);
				surface_tool.AddIndex(bottom_right_idx);
				//Tri 2
				surface_tool.AddIndex(bottom_right_idx);
				surface_tool.AddIndex(top_right_down_idx);
				surface_tool.AddIndex(bottom_right_down_idx);
			}
			//Right face
			if (verts.Contains(bottom_right) && verts.Contains(bottom_right_down) && verts.Contains(bottom_left) && verts.Contains(bottom_left_down))
			{
				//Load vertices
				int bottom_right_idx = load_vertex(bottom_right, Axis.Right);
				int bottom_right_down_idx = load_vertex(bottom_right_down, Axis.Right);
				int bottom_left_idx = load_vertex(bottom_left, Axis.Right);
				int bottom_left_down_idx = load_vertex(bottom_left_down, Axis.Right);

				//Tri 1
				surface_tool.AddIndex(bottom_right_idx);
                surface_tool.AddIndex(bottom_left_idx);
                surface_tool.AddIndex(bottom_left_down_idx);
				//Tri 2
				surface_tool.AddIndex(bottom_right_idx);
				surface_tool.AddIndex(bottom_left_down_idx);
				surface_tool.AddIndex(bottom_right_down_idx);
			}
		}

        surface_tool.GenerateNormals();
        ArrayMesh mesh_array = surface_tool.Commit();

        Mesh = mesh_array;

		Update_Shader();
    }
	public HashSet<Vector3I>? GenerateMesh()
	{
		Debug.WriteLine("Generating Mesh");
		if (WorldData.CurrentWorld == null)
			return null;

		//Generate verts
		HashSet<Vector3I> verts = new();

		for (int x = -XRenderDistance; x < XRenderDistance; ++x)
			for (int y = -YRenderDistance; y < YRenderDistance; ++y)
			{
				Chunk current = WorldData.CurrentWorld.RequestChunkAt(new(x + playerChunk.X, y + playerChunk.Y));
				int chunkX = (x + playerChunk.X) * XRenderDistance;
				int chunkY = (y + playerChunk.Y) * YRenderDistance;
				for (int xc = 0; xc < Chunk.EdgeLength; ++xc)
					for (int zc = 0; zc < Chunk.EdgeLength; ++zc)
						for (int yc = 0; yc < WorldData.CurrentWorld.WorldHeight; ++yc)
						{
							Vector3I pos = new(xc, yc, zc);
							
							if (current.GetTileAt(pos) != TileRegistry.AIR)
							{
                                int xPos = pos.X + chunkX;
                                int zPos = pos.Z + chunkY;
								//Top
								if (current.GetTileAt(pos + new Vector3I(0, 1, 0)) == TileRegistry.AIR)
								{
									verts.Add(new(xPos, pos.Y, zPos));
									verts.Add(new(xPos + 1, pos.Y, zPos));
									verts.Add(new(xPos, pos.Y, zPos + 1));
									verts.Add(new(xPos + 1, pos.Y, zPos + 1));
								}
								//Right
								if (current.GetTileAt(pos + new Vector3I(1, 0, 0)) == TileRegistry.AIR) {
									verts.Add(new(xPos + 1, pos.Y, zPos));
									verts.Add(new(xPos + 1, pos.Y - 1, zPos));
									verts.Add(new(xPos + 1, pos.Y, zPos + 1));
									verts.Add(new(xPos + 1, pos.Y - 1, zPos + 1));
                                }
                                //Left
                                if (current.GetTileAt(pos + new Vector3I(0, 0, 1)) == TileRegistry.AIR)
                                {
                                    verts.Add(new(xPos, pos.Y, zPos + 1));
                                    verts.Add(new(xPos, pos.Y - 1, zPos + 1));
                                    verts.Add(new(xPos + 1, pos.Y, zPos + 1));
                                    verts.Add(new(xPos + 1, pos.Y - 1, zPos + 1));
                                }
                            }
						}
			}

		return verts;
	}
	bool debug_on = false;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!debug_on && Input.IsKeyLabelPressed(Key.F3))
		{
			if (Input.IsActionPressed("debug_reload_world"))
			{
				World = World; //This runs the setter code.
				Update = true;
                debug_on = true;
            }
		}
		else if (debug_on && !Input.IsKeyLabelPressed(Key.F3))
			debug_on = false;

		if (Update)
		{
			var verts = GenerateMesh();
			if (verts != null)
				RenderMesh(verts);
			else
				Debug.WriteLine("No world data found");

			Update = false;
		}
	}
}

public enum World
{
	Debug,
	Overworld
}
public enum Axis
{
	Top,
	Left,
	Right
}