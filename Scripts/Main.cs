using System;
using System.Collections.Generic;
using Godot;

namespace RiverRaid.Scripts;

public partial class Main : Node2D
{
	// Called when the node enters the scene tree for the first time.
	[Export] private Node2D _scroll;
	[Export] private float _baseScrollSpeed = 50f;
	[Export] private float _maxScrollSpeedMult = 2.5f;
	[Export] private float _minScrollSpeedMult = 0.5f;
	[Export] private Player _player;
	[Export] private Terrain _terrain;
	private int _score = 0;
	private float _spawnPoint;
	
	private float _scrollSpeedMult = 1f;
	public override void _Ready()
	{
		AddToGroup("main");
		_player.Damaged += OnPlayerDamaged;
		_player.Checkpoint += OnCheckpoint;
		_player.SetStartPos(_baseScrollSpeed, _minScrollSpeedMult * _baseScrollSpeed, _maxScrollSpeedMult * _baseScrollSpeed);
	}

	private void OnCheckpoint(Vector2 position)
	{
		_spawnPoint = position.Y;
		_terrain.OnCheckpoint();
	}

	private void OnPlayerDamaged()
	{
		_scrollSpeedMult = 1f;
		SetPhysicsProcess(false);

		Tween tween = CreateTween();
		tween.TweenProperty(_scroll, "position", new Vector2(_scroll.Position.X, _spawnPoint), 1)
			.SetEase(Tween.EaseType.Out)
			.SetDelay(0.3);
		tween.TweenCallback(Callable.From(() =>
		{
			SetPhysicsProcess(true);
			_player.OnRespawn();
		})).SetDelay(0.5);
		tween.TweenCallback(Callable.From(() => GetTree().CallGroup("enemy", "EnableEnemy")));

		tween.Play();
	}

	public override void _PhysicsProcess(double delta)
	{
		float targetSpeedMult;

		if (Input.IsActionPressed("accelerate"))
			targetSpeedMult = _maxScrollSpeedMult;
		else if (Input.IsActionPressed("brake"))
			targetSpeedMult = _minScrollSpeedMult;
		else targetSpeedMult = 1f;

		_scrollSpeedMult = Mathf.MoveToward(_scrollSpeedMult, targetSpeedMult, 2f * (float) delta);
		
		float speed = _baseScrollSpeed * _scrollSpeedMult;
		float newScrollY = _scroll.Position.Y + speed * (float) delta;
		
		_player.OffsetVertical(speed, _minScrollSpeedMult * _baseScrollSpeed, _maxScrollSpeedMult * _baseScrollSpeed);
		
		_scroll.Position = _scroll.Position with { Y = newScrollY };
	}

	private void AddScore(int amt)
	{
		_score += amt;
		GD.Print(_score);
	}
}