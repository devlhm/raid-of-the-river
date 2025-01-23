using Godot;
using RiverRaid.Scripts.Interfaces;

namespace RiverRaid.Scripts;

public partial class Bullet : Area2D
{
	[Export] private Timer _timer;
	[Export] private AnimationPlayer _animationPlayer;
	public Vector2 Direction;
	public float Speed;
	public float Ttl;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Rotation = Direction.AngleTo(Vector2.Up);
		_timer.WaitTime = Ttl;
		_timer.Timeout += OnTimerTimeout;
		_animationPlayer.Play("fade_in");
		AreaEntered += OnAreaEntered;
		_timer.Start();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 targetPos = Position + (float) delta * Speed * Direction;
		Position = targetPos;
	}

	private void OnTimerTimeout()
	{
		GetParent().RemoveChild(this);
		QueueFree();
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is BoatEnemy { Disabled: true })
			return; 
		
		_timer.Paused = true;

		var delete = true;
	
		if (area.GetParent() is IDamageable damageable)
			delete = damageable.TryDamage(1);

		if (delete)
		{
			GetParent().CallDeferred("remove_child", this);
			CallDeferred("queue_free");
		}
	}
}