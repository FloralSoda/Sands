using Godot;
using System;
using System.Diagnostics;

public partial class movement : CharacterBody3D
{
	[Signal]
	public delegate void PositionChangedEventHandler(Vector3 new_location);
	public const float MaxSpeed = 1.9f;
	public const float RunBoostPerc = 1.5f;
	public const float Acceleration = 0.5f;
	public const float Friction = 0.2f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private AnimatedSprite3D sprite;

    public override void _Ready()
    {
		sprite = GetNode<AnimatedSprite3D>("Sprite");
    }

    public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("motion_left", "motion_right", "motion_up", "motion_down").Rotated(Mathf.DegToRad(-45));

		string animation = "move_";
		if (Input.IsActionPressed("motion_left"))
			animation = "move_left";
		else if (Input.IsActionPressed("motion_right"))
			animation = "move_right";
		else if (Input.IsActionPressed("motion_up"))
			animation = "move_up";
		else if (Input.IsActionPressed("motion_down"))
			animation = "move_down";
		else
			animation = "default";

		if (sprite.Animation != animation)
			sprite.Play(animation);
		

		Vector3 direction = new(inputDir.X, 0, inputDir.Y);
		if (direction != Vector3.Zero) {
            Vector2 horizontal = new(velocity.X, velocity.Z);
            if (velocity.Length() == 0 || inputDir.Dot(horizontal) > 0.4f)
			{
                float maxSpeed = MaxSpeed;
				float accel = Acceleration;
				if (Input.IsActionPressed("motion_sprint"))
				{
					maxSpeed *= RunBoostPerc;
					accel *= RunBoostPerc;
				}
                velocity.X += direction.X * accel;
				velocity.Z += direction.Z * accel;

				if (Input.IsActionPressed("motion_sprint"))
					maxSpeed *= RunBoostPerc;

				if (horizontal.Length() > maxSpeed)
				{
					horizontal = horizontal.Normalized() * maxSpeed;
					velocity.X = horizontal.X;
					velocity.Z = horizontal.Y;
				}
			} else
			{
                velocity.X = Mathf.MoveToward(Velocity.X, 0, Friction);
                velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Friction);
            }

        }
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Friction);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Friction);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
