using Godot;
using RiverRaid.Scripts.Interfaces;

namespace RiverRaid.Scripts;

public abstract partial class Enemy : CharacterBody2D, IDamageable
{
    [Export] protected float Speed = 300.0f;
    [Export] private int _score = 10;
    protected Vector2 Direction;
    public bool Disabled;
    
    public override void _Ready()
    {
        AddToGroup("enemy");
    }
    
    public bool TryDamage(int amount)
    {
        DisableEnemy();
		
        return true;
    }

    private void DisableEnemy()
    {
        GetTree().CallGroup("main", "AddScore", _score);
        SetVisible(false);
		
        SetPhysicsProcess(false);
        Disabled = true;
    }

    public void EnableEnemy()
    {
        SetVisible(true);

        SetPhysicsProcess(true);
        Disabled = false;
    }

    public void Destroy()
    {
        GetParent().CallDeferred("remove_child", this);
        CallDeferred("queue_free");
    }
}