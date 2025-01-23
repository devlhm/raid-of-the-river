using Godot;
using RiverRaid.Scripts.Interfaces;

namespace RiverRaid.Scripts;

public partial class Player : CharacterBody2D, IDamageable
{
	private const float Speed = 300.0f;
	
	[Signal]
	public delegate void DamagedEventHandler();
	
	[Signal]
	public delegate void CheckpointEventHandler(Vector2 position);
    
	[Export] private Area2D _hurtbox;
	[Export] private AnimationPlayer _animPlayer;
	[Export] private int _lives = 3;
	[Export] private Vector2 _startPos;
	[Export] private Timer _invincibilityTimer;
	[Export] private ShootingComponent _shootingComponent;
	[Export] private float _minYPos;
	[Export] private float _maxYPos;
	public bool Invincible;

	public override void _Ready()
	{
		Position = _startPos;
		
		_hurtbox.BodyEntered += OnHurtboxBodyEntered;
		_invincibilityTimer.Timeout += OnInvincibilityTimerTimeout;
	}

	private void OnInvincibilityTimerTimeout()
	{
		Invincible = false;
		SetPhysicsProcess(true);
		_shootingComponent.SetPhysicsProcess(true);
	}

	private void SetInvincible(bool value)
	{
		Invincible = value;
	}

	private void OnHurtboxBodyEntered(Node body)
	{
		if (body is BoatEnemy { Disabled: true })
			return; 
		
		TryDamage();
	}

	public bool TryDamage(int amount = 1)
	{
		if (Invincible) return false;
		Invincible = true;
		
		Position = _startPos;
		SetPhysicsProcess(false);
		_shootingComponent.SetPhysicsProcess(false);
		
		_lives--;
		if (_lives == 0)
		{
			GD.Print("Game over");
			GetTree().Paused = true;
		}
		else
		{
			EmitSignal(SignalName.Damaged);
		}
		
		_animPlayer.Play("damage");

		return true;
	}

	public void OnRespawn()
	{
		Invincible = false;
		SetPhysicsProcess(true);
		_shootingComponent.SetPhysicsProcess(true);
	}

	public void OnCheckpoint(Vector2 position)
	{
		_startPos = position;
		EmitSignal(SignalName.Checkpoint, position);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Vector2.Zero;

		if (Input.IsActionPressed("left"))
			direction = Vector2.Left;
		else if (Input.IsActionPressed("right"))
			direction = Vector2.Right;
		
		velocity.X = direction != Vector2.Zero ?
			Mathf.MoveToward(Velocity.X, direction.X * Speed, Speed) :
			Mathf.MoveToward(Velocity.X, 0, Speed);

		Velocity = velocity;
		MoveAndSlide();
	}

	public void SetStartPos(float speed, float minSpeed, float maxSpeed)
	{
		_startPos = new Vector2(_startPos.X, OffsetVertical(speed, minSpeed, maxSpeed));
	}

	public float OffsetVertical(float speed, float minSpeed, float maxSpeed)
	{
		float weight = (speed - minSpeed) / (maxSpeed - minSpeed);
		float targetYPos = Mathf.Lerp(_minYPos, _maxYPos, weight);
		Position = Position with { Y =  targetYPos};
		return targetYPos;
	}
}