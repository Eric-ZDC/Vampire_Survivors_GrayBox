using Godot;

/// <summary>
/// 玩家控制器：移动、跳跃、远程攻击、近战、换弹、受击、无敌帧、升级。
/// </summary>
public partial class Player : CharacterBody2D
{
    // ============================================================
    //  数值配置
    // ============================================================

    private const float PixelsPerUnit = 30f;

    [Export] public float MoveSpeedUnits = 5f;
    [Export] public float MaxHealth = 100f;
    [Export] public float PickupRangeUnits = 2f;
    [Export] public float InvincibleTime = 0.5f;

    // 远程武器
    [Export] public float WeaponDamage = 1f;
    [Export] public float WeaponCooldown = 1f;
    [Export] public int MagazineSize = 7;
    [Export] public float BulletSpeed = 500f;
    [Export] public float ReloadTime = 1.5f;

    // 近战
    [Export] public float MeleeDamage = 3f;
    [Export] public float MeleeCooldown = 0.8f;
    [Export] public float MeleeRange = 60f;
    [Export] public float MeleeArcDegrees = 90f;

    // 跳跃
    [Export] public float JumpDuration = 0.6f;
    [Export] public float JumpCooldown = 0.3f;
    [Export] public float JumpHeight = 20f;

    // 升级
    [Export] public int CurrentLevel = 1;
    [Export] public int BaseExpToLevel = 10;
    [Export] public int ExpPerLevelIncrease = 10;

    // ============================================================
    //  运行时状态
    // ============================================================

    public float CurrentHealth { get; private set; }
    public int CurrentExp { get; private set; }
    public int ExpToNextLevel => BaseExpToLevel + (CurrentLevel - 1) * ExpPerLevelIncrease;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;
    public bool IsJumping => _isJumping;
    public bool IsInvincible => _invincibleTimer > 0f || _isJumping;

    private float _invincibleTimer = 0f;
    private float _shootCooldown = 0f;
    private float _reloadTimer = 0f;
    private float _meleeCooldown = 0f;
    private float _jumpTimer = 0f;
    private float _jumpCooldownTimer = 0f;

    private int _currentAmmo;
    private bool _isReloading = false;
    private bool _isJumping = false;

    private int _pendingLevelUps = 0;

    private Area2D _hitBox;
    private Node2D _sprite;
    private PackedScene _bulletScene;

    private PlayerStats _stats;
    private UpgradeUI _upgradeUI;

    // 武器扩展
    private int _projectileCount = 1;
    private float _lifeStealAmount = 0f;
    private float _shieldInterval = 0f;
    private float _shieldTimer = 0f;
    private bool _hasShield = false;

    // ============================================================
    //  只读换算属性
    // ============================================================

    public float MoveSpeed => (_stats?.FinalMoveSpeed ?? MoveSpeedUnits) * PixelsPerUnit;
    public float PickupRange => (_stats?.FinalPickupRange ?? PickupRangeUnits) * PixelsPerUnit;

    // ============================================================
    //  生命周期
    // ============================================================

    public override void _Ready()
    {
        AddToGroup("player");

        CurrentHealth = MaxHealth;
        _currentAmmo = MagazineSize;

        _bulletScene = GD.Load<PackedScene>("res://Scenes/bullet.tscn");
        _sprite = GetNodeOrNull<Node2D>("Sprite2D");

        _stats = GetNodeOrNull<PlayerStats>("PlayerStats");
        _upgradeUI = GetTree().CurrentScene.GetNodeOrNull<UpgradeUI>("UI/UpgradeUI");

        _hitBox = GetNodeOrNull<Area2D>("HitBox");
        if (_hitBox == null)
        {
            GD.PrintErr("Player 找不到 HitBox 节点");
            return;
        }

        _hitBox.AreaEntered += OnHitBoxAreaEntered;
        _hitBox.BodyEntered += OnHitBoxBodyEntered;

        GD.Print("Player 初始化完成");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        UpdateTimers(dt);

        HandleMovement();
        HandleJump();
        HandleAttack();
        HandleMelee();
        HandleShield(dt);
    }

    private void UpdateTimers(float delta)
    {
        if (_invincibleTimer > 0f) _invincibleTimer -= delta;
        if (_shootCooldown > 0f) _shootCooldown -= delta;
        if (_reloadTimer > 0f) _reloadTimer -= delta;
        if (_meleeCooldown > 0f) _meleeCooldown -= delta;
        if (_jumpCooldownTimer > 0f) _jumpCooldownTimer -= delta;
    }

    // ============================================================
    //  移动
    // ============================================================

    private void HandleMovement()
    {
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = inputDir * MoveSpeed;
        MoveAndSlide();
    }

    // ============================================================
    //  跳跃
    // ============================================================

    private void HandleJump()
    {
        if (Input.IsActionJustPressed("jump") && !_isJumping && _jumpCooldownTimer <= 0f)
        {
            _isJumping = true;
            _jumpTimer = JumpDuration;
            _jumpCooldownTimer = JumpDuration + JumpCooldown;
            GD.Print("玩家跳跃");
        }

        if (_isJumping)
        {
            _jumpTimer -= (float)GetPhysicsProcessDeltaTime();

            if (_sprite != null)
            {
                float t = 1f - (_jumpTimer / JumpDuration);
                float height = Mathf.Sin(t * Mathf.Pi) * JumpHeight;
                _sprite.Position = new Vector2(0, -height);
            }

            if (_jumpTimer <= 0f)
            {
                _isJumping = false;
                if (_sprite != null)
                    _sprite.Position = Vector2.Zero;
                GD.Print("玩家落地");
            }
        }
    }

    // ============================================================
    //  远程攻击
    // ============================================================

    private void HandleAttack()
    {
        if (!Input.IsActionPressed("attack_ranged")) return;
        if (_isReloading) return;

        if (_currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        TryShoot();
    }

    private void TryShoot()
    {
        if (_shootCooldown > 0f) return;
        if (_bulletScene == null) return;

        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 baseDir = (mousePos - GlobalPosition).Normalized();
        Vector2 muzzleOffset = _sprite != null ? _sprite.Position : Vector2.Zero;

        float spreadStep = 10f;
        float startAngle = -spreadStep * (_projectileCount - 1) / 2f;

        for (int i = 0; i < _projectileCount; i++)
        {
            float angle = Mathf.DegToRad(startAngle + i * spreadStep);
            Vector2 dir = baseDir.Rotated(angle);

            var bullet = _bulletScene.Instantiate<Bullet>();
            bullet.GlobalPosition = GlobalPosition + muzzleOffset;
            bullet.SetDirection(dir);
            bullet.Damage = _stats?.FinalDamage ?? WeaponDamage;
            bullet.Speed = BulletSpeed;

            var container = GetTree().CurrentScene.GetNodeOrNull<Node2D>("Projectiles");
            if (container != null)
                container.AddChild(bullet);
            else
                GetTree().CurrentScene.AddChild(bullet);
        }

        _currentAmmo--;
        _shootCooldown = _stats?.FinalAttackSpeed ?? WeaponCooldown;
        GD.Print($"发射 {_projectileCount} 发子弹，剩余弹药 {_currentAmmo}");
    }

    // ============================================================
    //  近战攻击
    // ============================================================

    private void HandleMelee()
    {
        if (!Input.IsActionJustPressed("attack_melee")) return;
        if (_meleeCooldown > 0f) return;

        PerformMelee();
    }

    private void PerformMelee()
    {
        _meleeCooldown = MeleeCooldown;

        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 attackDir = (mousePos - GlobalPosition).Normalized();
        float halfArc = Mathf.DegToRad(MeleeArcDegrees / 2f);

        var enemies = GetTree().GetNodesInGroup("enemy");
        int hitCount = 0;

        foreach (var node in enemies)
        {
            if (node is not Enemy enemy) continue;

            Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
            float dist = toEnemy.Length();

            if (dist > MeleeRange) continue;

            float angle = attackDir.AngleTo(toEnemy.Normalized());
            if (Mathf.Abs(angle) > halfArc) continue;

            enemy.TakeDamage(MeleeDamage);
            hitCount++;
        }

        GD.Print($"近战攻击，命中 {hitCount} 个敌人");
    }

    // ============================================================
    //  换弹
    // ============================================================

    private void StartReload()
    {
        if (_isReloading) return;
        if (_currentAmmo >= (_stats?.FinalMagazineSize ?? MagazineSize)) return;

        _isReloading = true;
        _reloadTimer = ReloadTime;
        GD.Print("开始换弹...");
        FinishReloadAsync();
    }

    private async void FinishReloadAsync()
    {
        await ToSignal(GetTree().CreateTimer(ReloadTime), "timeout");
        if (!IsInsideTree()) return;

        _currentAmmo = _stats?.FinalMagazineSize ?? MagazineSize;
        _isReloading = false;
        _reloadTimer = 0f;
        GD.Print($"换弹完成，弹药 {_currentAmmo}");
    }

    // ============================================================
    //  护盾
    // ============================================================

    private void HandleShield(float delta)
    {
        if (_hasShield && _shieldTimer > 0f)
        {
            _shieldTimer -= delta;
            if (_shieldTimer <= 0f)
                _shieldTimer = _shieldInterval;
        }
    }

    // ============================================================
    //  升级
    // ============================================================

    public void GainExp(int amount)
    {
        int finalAmount = Mathf.RoundToInt(amount * (_stats?.FinalExpGain ?? 1f));
        CurrentExp += finalAmount;
        GD.Print($"获得经验 {finalAmount}，当前 {CurrentExp}/{ExpToNextLevel}");

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            CurrentLevel++;
            _pendingLevelUps++;
            GD.Print($"升级！当前等级 {CurrentLevel}");
        }

        if (_pendingLevelUps > 0)
            TriggerUpgradeUI();
    }

    private void TriggerUpgradeUI()
    {
        if (_upgradeUI != null)
            _upgradeUI.ShowUpgrade();
        else
            GD.PrintErr("UpgradeUI 未找到");
    }

    public void OnUpgradeUIFinished()
    {
        _pendingLevelUps--;
        if (_pendingLevelUps > 0)
            TriggerUpgradeUI();
    }

    public void HealBy(float amount)
    {
        float max = _stats?.FinalMaxHealth ?? MaxHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, max);
        GD.Print($"回复生命 {amount}，当前 {CurrentHealth}/{max}");
    }

    public void AddProjectileCount(int count)
    {
        _projectileCount += count;
        GD.Print($"弹幕数量增加到 {_projectileCount}");
    }

    public void EnableLifeSteal(float amount)
    {
        _lifeStealAmount += amount;
        GD.Print($"击杀回血 +{amount}");
    }

    public void EnableShield(float interval)
    {
        _shieldInterval = interval;
        _shieldTimer = interval;
        _hasShield = true;
        GD.Print($"护盾开启，每 {interval} 秒免疫一次");
    }

    public void OnEnemyKilled()
    {
        if (_lifeStealAmount > 0f)
            HealBy(_lifeStealAmount);
    }

    // ============================================================
    //  受击与生命
    // ============================================================

    private void OnHitBoxBodyEntered(Node2D body)
    {
        if (body is Enemy enemy)
            TakeDamage(enemy.ContactDamage);
    }

    private void OnHitBoxAreaEntered(Area2D area)
    {
        if (area.GetParent() is Enemy enemy)
            TakeDamage(enemy.ContactDamage);
    }

    public void TakeDamage(float damage)
    {
        if (_isJumping) return;

        // 护盾免疫
        if (_hasShield && _shieldTimer <= 0f)
        {
            _shieldTimer = _shieldInterval;
            GD.Print("护盾抵挡了一次伤害");
            return;
        }

        if (IsInvincible) return;

        CurrentHealth -= damage;
        _invincibleTimer = InvincibleTime;

        GD.Print($"玩家受伤 {damage}，剩余生命 {CurrentHealth}，无敌帧开始");
        StartInvincibleFlash();

        if (CurrentHealth <= 0f)
            Die();
    }

    private async void StartInvincibleFlash()
    {
        if (_sprite == null) return;

        float elapsed = 0f;
        const float interval = 0.1f;

        while (elapsed < InvincibleTime)
        {
            _sprite.Visible = !_sprite.Visible;
            await ToSignal(GetTree().CreateTimer(interval), "timeout");
            elapsed += interval;
        }
        _sprite.Visible = true;
    }

    private void Die()
    {
        GD.Print("玩家死亡");
        SetPhysicsProcess(false);
    }

    // ============================================================
    //  调试绘制
    // ============================================================

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, PickupRange, new Color(1, 1, 0, 0.15f));
    }
}