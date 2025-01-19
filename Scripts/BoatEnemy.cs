using Godot;
using RiverRaid.Scripts.Interfaces;

namespace RiverRaid.Scripts;

public partial class BoatEnemy : CharacterBody2D, IDamageable
{
	[Export] private float _speed = 300.0f;
	[Export] private Area2D _hurtbox;
	[Export] private RayCast2D _raycast;
	private Vector2 _direction;

	public override void _Ready()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		_direction = rng.Randf() > 0.5 ? Vector2.Right : Vector2.Left;
		Rotation = -_direction.AngleTo(Vector2.Up);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_raycast.IsColliding())
		{
			_direction = -_direction;
			Rotation = -_direction.AngleTo(Vector2.Up);
		}

		Position += _direction * _speed * (float) delta;
	}

	public bool TryDamage(int amount)
	{
		GetParent().CallDeferred("remove_child", this);
		CallDeferred("queue_free");
		return true;
	}
}