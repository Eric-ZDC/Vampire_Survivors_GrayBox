using Godot;

/// <summary>
/// 经验掉落物：玩家进入拾取范围后自动吸取，增加经验。
/// </summary>
public partial class ExperiencePickup : Area2D
{
    [Export] public int ExperienceValue = 1;
    [Export] public float MoveSpeed = 200f;

    private Node2D _player;
    private bool _isAttracted = false;

    public override void _Ready()
    {
        AddToGroup("pickup");
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null) return;

        float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (_player is Player p && dist <= p.PickupRange)
            _isAttracted = true;

        if (_isAttracted)
        {
            Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += dir * MoveSpeed * (float)delta;

            if (dist < 12f)
                Collect();
        }
    }

    private void Collect()
    {
        if (_player is Player p)
            p.GainExp(ExperienceValue);
        QueueFree();
    }
}