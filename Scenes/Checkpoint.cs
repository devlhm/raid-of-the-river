using Godot;
using RiverRaid.Scripts;

namespace RiverRaid.Scenes;

public partial class Checkpoint : Area2D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}
	
	private void OnBodyEntered(Node body)
	{
		if (body is Player player)
		{
			player.OnCheckpoint(GlobalPosition);
			GetParent().CallDeferred("remove_child", this);
			CallDeferred("queue_free");
		}
	}
}