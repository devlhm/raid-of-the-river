using Godot;

namespace RiverRaid.Scripts;

public partial class BoatEnemy : Enemy
{
	[Export] private RayCast2D _raycast;

	public override void _Ready()
	{
		base._Ready();
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		Direction = rng.Randf() > 0.5 ? Vector2.Right : Vector2.Left;
		Rotation = -Direction.AngleTo(Vector2.Up);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_raycast.IsColliding())
		{
			Direction = -Direction;
			Rotation = -Direction.AngleTo(Vector2.Up);
		}

		Position += Direction * Speed * (float) delta;
	}
}