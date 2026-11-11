using Godot;

/// <summary>
/// 敌人生成器：按间隔从屏幕边缘生成敌人。
/// </summary>
public partial class EnemySpawner : Node2D
{
    [Export] public PackedScene EnemyScene;
    [Export] public float SpawnInterval = 1.0f;
    [Export] public int MaxEnemies = 100;  // 调试先1个敌人
    [Export] public float SpawnRadius = 400f;

    private float _spawnTimer = 0f;
    private Node2D _enemyContainer;
    private Node2D _player;

    public override void _Ready()
    {
        if (EnemyScene == null)
            EnemyScene = GD.Load<PackedScene>("res://Scenes/Enemy.tscn");

        _enemyContainer = GetTree().CurrentScene.GetNodeOrNull<Node2D>("Enemies");
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (_enemyContainer == null)
            GD.PrintErr("EnemySpawner 找不到 Enemies 容器");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || _enemyContainer == null) return;

        _spawnTimer -= (float)delta;
        if (_spawnTimer > 0f) return;

        _spawnTimer = SpawnInterval;

        if (_enemyContainer.GetChildCount() >= MaxEnemies) return;

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (EnemyScene == null) return;

        var enemy = EnemyScene.Instantiate<Enemy>();

        float angle = (float)GD.RandRange(0, Mathf.Tau);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SpawnRadius;
        enemy.GlobalPosition = _player.GlobalPosition + offset;

        _enemyContainer.AddChild(enemy);
    }
}