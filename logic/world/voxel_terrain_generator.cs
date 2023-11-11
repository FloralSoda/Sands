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
	public byte XRenderDistance = 2;
	[Export(hintString: "How far across the screen vertically the world should load, in chunks. Used for extreme aspect ratios")] 
	public byte YRenderDistance = 2;
	[Export(hintString: "The maximum height of the current world, in tiles. The player can go above this, but tiles cannot")]
	public byte WorldHeight = 255;
	[Export(hintString: "The world generator used.")]
	public World World = World.Debug;

	public WorldData? currentWorld;

	public Vector2I playerChunk;
	public Vector3 playerLocal;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (World == World.Debug)
			LoadDebugMap();
		else
			Debug.WriteLine("Overworld is not yet implemented. Sorry for the inconvenience!");

		GenerateMesh();
	}

	public void LoadDebugMap()
	{
		Debug.WriteLine("Generating Parent Chunk");
		Chunk simple = new(WorldHeight);
		Debug.WriteLine("Filling Region");
		simple.SetRegion(new(0, WorldHeight / 2, 0), new(Chunk.EdgeLength - 1, WorldHeight / 2, Chunk.EdgeLength - 1),TileRegistry.SANDBRICK);
		Debug.WriteLine("Producing Generator");
		RepetitionGenerator gen = new(simple);
		Debug.WriteLine("Building World");

		currentWorld = new WorldData((ulong)DateTime.Now.Ticks, gen, WorldHeight);
	}

	public void OnPlayerMove(Vector3 new_location) {
		playerLocal = new(new_location.X % Chunk.EdgeLength, new_location.Y, new_location.Z % Chunk.EdgeLength);
		playerChunk = new((int)Math.Floor(new_location.X / Chunk.EdgeLength), (int)Math.Floor(new_location.Z / Chunk.EdgeLength));
	}

	public void GenerateMesh()
	{
		if (currentWorld == null)
			return;

		//Generate verts
		HashSet<Vector3> verts = new();

		for (int x = 0; x < XRenderDistance; ++x)
			for (int y = 0; y < YRenderDistance; ++y)
			{
				Chunk current = currentWorld.RequestChunkAt(new(x + playerChunk.X, y + playerChunk.Y));
				for (int xc = 0; xc < Chunk.EdgeLength; ++xc)
					for (int zc = 0; zc < Chunk.EdgeLength; ++zc)
						for (int yc = 0; yc < currentWorld.WorldHeight; ++yc) {
							Vector3I pos = new(xc, yc, zc);
							if (current.GetTileAt(pos) == TileRegistry.AIR)
							{
								verts.Add(new(pos.X, pos.Y, pos.X));
								verts.Add(new(pos.X + 1, pos.Y, pos.Z));
								verts.Add(new(pos.X, pos.Y, pos.Z + 1));
								verts.Add(new(pos.X + 1, pos.Y, pos.Z + 1));
							}
						}
			}


		SurfaceTool surface_tool = new();
		surface_tool.Begin(Mesh.PrimitiveType.Triangles);

		int vertex_width_x = XRenderDistance;
		int vertex_width_z = YRenderDistance;


		for (int x = 0; x <= vertex_width_x; ++x) {
			for (int z = 0; z <= vertex_width_z; ++z)
			{
				surface_tool.SetUV(new(x, z));
				surface_tool.AddVertex(new(x, 0, z));
			}
		}
		for (int x = 0; x <= vertex_width_x; ++x)
		{
			for (int z = 0; z < vertex_width_z; ++z)
			{
				//Tri 1
				surface_tool.AddIndex((x * vertex_width_z) + z); //Top left
				surface_tool.AddIndex(((x + 1) * vertex_width_z) + z + 1); //Bottom Right
				surface_tool.AddIndex((x * vertex_width_z) + z + 1); //Top Right

				//Tri 2
				surface_tool.AddIndex((x * vertex_width_z) + z); //Top left
				surface_tool.AddIndex(((x + 1) * vertex_width_z) + z); //Bottom Left
				surface_tool.AddIndex(((x + 1) * vertex_width_z) + z + 1); //Bottom Right
			}
		}
		surface_tool.GenerateNormals();
		ArrayMesh mesh_array = surface_tool.Commit();

		Mesh = mesh_array;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Update)
		{
			GenerateMesh();
			Update = false;
		}
	}
}

public enum World
{
	Debug,
	Overworld
}
