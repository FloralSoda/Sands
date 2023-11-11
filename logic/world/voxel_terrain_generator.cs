#nullable enable

using Godot;
using Sands.logic.tile;
using Sands.logic.world;
using Sands.logic.world.generators;
using System;
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
		Debug.WriteLine("Generating Chunk");
		Chunk simple = new(WorldHeight);
		Debug.WriteLine("Filling Region");
		simple.SetRegion(new(0, WorldHeight / 2, 0), new(Chunk.EdgeLength - 1, WorldHeight / 2, Chunk.EdgeLength - 1),TileRegistry.SANDBRICK);
		Debug.WriteLine("Producing Generator");
		RepetitionGenerator gen = new(simple);
		Debug.WriteLine("Building World");
		currentWorld = new WorldData((ulong)DateTime.Now.Ticks, gen);
	}

	public void GenerateMesh()
	{
		int[] indices = new int[]
		{
			0,1,2,0,2,3
		};

		SurfaceTool surface_tool = new();
		surface_tool.Begin(Mesh.PrimitiveType.Triangles);

		int vertex_width_x = XRenderDistance * Chunk.EdgeLength;
		int vertex_width_z = YRenderDistance * Chunk.EdgeLength;


        for (int x = 0; x <= vertex_width_x; ++x) {
			for (int z = 0; z <= vertex_width_z; ++z)
			{
				surface_tool.SetUV(new(x % 2, z % 2));
				surface_tool.AddVertex(new(x, 0, z));
            }
		}
		for (int x = 0; x < vertex_width_x; ++x)
			for (int z = 0; z < vertex_width_z; ++z) {
				//Tri 1
				surface_tool.AddIndex((x * vertex_width_z) + z); //Top left
                surface_tool.AddIndex((x * vertex_width_z) + z + 1); //Top Right
                surface_tool.AddIndex(((x+1) * vertex_width_z) + z + 1); //Bottom Right

                //Tri 2
                surface_tool.AddIndex((x * vertex_width_z) + z); //Top left
                surface_tool.AddIndex(((x + 1) * vertex_width_z) + z + 1); //Bottom Right
				surface_tool.AddIndex(((x + 1) * vertex_width_z) + z); //Bottom Left
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
