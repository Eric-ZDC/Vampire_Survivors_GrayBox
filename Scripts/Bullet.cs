using Godot;

/// <summary>
/// 玩家远程子弹：沿固定方向飞行，命中敌人造成伤害后销毁。
/// </summary>
public partial class Bullet : Area2D
{
    [Export] public float Speed = 500f;
    [Export] public float Damage = 1f;
    [Export] public float Lifetime = 2f;

    private Vector2 _direction = Vector2.Right;
    private float _lifeTimer = 0f;

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.Normalized();
        Rotation = _direction.Angle();
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += _direction * Speed * (float)delta;

        _lifeTimer += (float)delta;
        if (_lifeTimer >= Lifetime)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Enemy enemy)
        {
            enemy.TakeDamage(Damage);
            QueueFree();
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area.GetParent() is Enemy enemy)
        {
            enemy.TakeDamage(Damage);
            QueueFree();
        }
    }
}