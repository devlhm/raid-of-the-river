using Godot;
using RiverRaid.Scripts.Interfaces;

namespace RiverRaid.Scripts;

public partial class Player : CharacterBody2D, IDamageable
{
	private const float Speed = 300.0f;
    
	[Export] private Area2D _hurtbox;
	[Export] private AnimationPlayer _animPlayer;
	[Export] private int _lives = 3;
	[Export] private Vector2 _startPos;
	[Export] private Timer _invincibilityTimer;
	[Export] private ShootingComponent _shootingComponent;
	private bool _invincible;

	public override void _Ready()
	{
		Position = _startPos;
		_hurtbox.BodyEntered += OnHurtboxBodyEntered;
		_invincibilityTimer.Timeout += OnInvincibilityTimerTimeout;
	}

	private void OnInvincibilityTimerTimeout()
	{
		_invincible = false;
		SetPhysicsProcess(true);
		_shootingComponent.SetPhysicsProcess(true);
	}

	private void SetInvincible(bool value)
	{
		_invincible = value;
	}

	private void OnHurtboxBodyEntered(Node body)
	{
		if (_invincible)
			return;

		TryDamage();
		
		Position = _startPos;
		SetPhysicsProcess(false);
	}

	public bool TryDamage(int amount = 1)
	{
		if (_invincible) return false;
		
		_invincible = true;
		_shootingComponent.SetPhysicsProcess(false);
		_lives--;
		if (_lives == 0)
		{
			GD.Print("Game over");
		}
		_animPlayer.Play("damage");
		
		_invincibilityTimer.Start();

		return true;
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

		float targetY;
		if (Input.IsActionPressed("brake"))
			targetY = _startPos.Y + 30;
		else if (Input.IsActionPressed("accelerate"))
			targetY = _startPos.Y - 50;
		else
			targetY = _startPos.Y;

		Position = Position with { Y = Mathf.MoveToward(Position.Y, targetY, 5f) };
		
		velocity.X = direction != Vector2.Zero ?
			Mathf.MoveToward(Velocity.X, direction.X * Speed, Speed) :
			Mathf.MoveToward(Velocity.X, 0, Speed);

		Velocity = velocity;
		MoveAndSlide();
	}
}