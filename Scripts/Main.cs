using System;
using Godot;

namespace RiverRaid.Scripts;

public partial class Main : Node2D
{
	// Called when the node enters the scene tree for the first time.
	[Export] private Node2D _scroll;
	[Export] private float _scrollSpeed = 50f;
	private float _scrollSpeedMult = 1f;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		float targetSpeed;

		if (Input.IsActionPressed("accelerate"))
			targetSpeed = 2.5f;
		else if (Input.IsActionPressed("brake"))
			targetSpeed = .5f;
		else targetSpeed = 1f;

		_scrollSpeedMult = Mathf.MoveToward(_scrollSpeedMult, targetSpeed, 2f * (float) delta);
		
		float newScrollY = _scroll.Position.Y + (float) delta * _scrollSpeed * _scrollSpeedMult;
		
		_scroll.Position = _scroll.Position with { Y = newScrollY };
	}
}