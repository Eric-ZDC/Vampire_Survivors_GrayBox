using Godot;

public enum EnemyType { Normal, Elite }

/// <summary>
/// 敌人控制器：追踪玩家、承受伤害、死亡掉落经验。
/// </summary>
public partial class Enemy : CharacterBody2D
{
    private const float PixelsPerUnit = 30f;

    [Export] public EnemyType Type = EnemyType.Normal;
    [Export] public float MoveSpeedUnits = 3.5f;
    [Export] public float ContactDamage = 5f;
    [Export] public float ContactKnockback = 0.5f;
    [Export] public int ExperienceDrop = 1;

    public float MaxHealth;
    public float CurrentHealth;

    private Node2D _target;
    private float _hitFlashTimer = 0f;
    private const float HitFlashDuration = 0.1f;
    private Color _baseColor = Colors.White;

    public float MoveSpeed => MoveSpeedUnits * PixelsPerUnit;

    public override void _Ready()
    {
        AddToGroup("enemy");
        ApplyTypeStats();

        _target = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (_target == null)
            GD.PrintErr("Enemy 找不到玩家");

        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
            _baseColor = sprite.Modulate;
    }

    private void ApplyTypeStats()
    {
        switch (Type)
        {
            case EnemyType.Normal:
                MaxHealth = 10f;  // 用于调试先改成2
                ContactDamage = 5f;
                ExperienceDrop = 1;
                MoveSpeedUnits = 3.5f;
                break;

            case EnemyType.Elite:
                MaxHealth = 30f;
                ContactDamage = 10f;
                ExperienceDrop = 3;
                MoveSpeedUnits = 3.5f;
                Scale = new Vector2(1.3f, 1.3f);
                break;
        }

        CurrentHealth = MaxHealth;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer -= (float)delta;
            if (_hitFlashTimer <= 0f)
                ResetColor();
        }

        if (_target == null) return;

        Vector2 dir = (_target.GlobalPosition - GlobalPosition).Normalized();
        Velocity = dir * MoveSpeed;
        MoveAndSlide();
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        FlashOnHit();

        if (CurrentHealth <= 0f)
            Die();
    }

    private void FlashOnHit()
    {
        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite == null) return;
        sprite.Modulate = new Color(1f, 0.3f, 0.3f);
        _hitFlashTimer = HitFlashDuration;
    }

    private void ResetColor()
    {
        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
            sprite.Modulate = _baseColor;
    }

    private void Die()
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        player?.OnEnemyKilled();

        DropExperience();
        QueueFree();
    }

    private void DropExperience()
    {
        var pickupScene = GD.Load<PackedScene>("res://Scenes/experience_pickup.tscn");
        if (pickupScene == null)
        {
            GD.PrintErr("experience_pickup.tscn 未找到");
            return;
        }

        var pickup = pickupScene.Instantiate<ExperiencePickup>();
        pickup.GlobalPosition = GlobalPosition;
        pickup.ExperienceValue = ExperienceDrop;

        var container = GetTree().CurrentScene.GetNodeOrNull<Node2D>("Pickups");
        if (container != null)
            container.CallDeferred(Node.MethodName.AddChild, pickup);
        else
            GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, pickup);

        GD.Print($"敌人死亡，掉落经验 {ExperienceDrop}");
    }
}