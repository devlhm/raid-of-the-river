using Godot;
using RiverRaid.Scripts.Interfaces;

namespace RiverRaid.Scripts;

public partial class Bridge : StaticBody2D, IDamageable
{
	[Export] private int _hp = 5;
	private void Destroy()
	{
		GetParent().RemoveChild(this);
		QueueFree();
	}

	public bool TryDamage(int amount)
	{
		_hp -= amount;
		if (_hp == 0)
			Destroy();

		return true;
	}
}