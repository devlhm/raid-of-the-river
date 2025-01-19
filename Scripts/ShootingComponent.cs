using System;
using Godot;

namespace RiverRaid.Scripts;

public partial class ShootingComponent : Node2D
{
	[Export]
	private PackedScene _bulletScene;

	[Export] private int _shotsPerSecond = 3;
	[Export] private bool _isPlayer;
	private float _shotInterval;

	[Export] private float _bulletSpeed = 100f;
	[Export] private Vector2 _bulletDirection = Vector2.Up;
	[Export] private float _bulletTtl = 5f;

	private float _elapsed;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_shotInterval = 1 / (float) _shotsPerSecond;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if(_elapsed >= _shotInterval)
		{
			if (_isPlayer && !Input.IsActionPressed("shoot"))
				return;

			SpawnShot();
			_elapsed = 0;
		}
		
		_elapsed = Math.Min(_elapsed + (float) delta, _shotInterval);
	}

	private void SpawnShot()
	{
		var bullet = _bulletScene.Instantiate<Bullet>();

		bullet.Speed = _bulletSpeed;
		bullet.Direction = _bulletDirection;
		bullet.Ttl = _bulletTtl;

		bullet.GlobalPosition = GlobalPosition;
		
		bullet.CollisionMask = _isPlayer ? (uint)4 : (uint)2;
		
		GetTree().GetRoot().AddChild(bullet);
	}
}