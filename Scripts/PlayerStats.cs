using Godot;

/// <summary>
/// 玩家可成长属性。升级选项通过修改这些值来生效。
/// 挂在 Player.tscn 的 PlayerStats 子节点上。
/// </summary>
public partial class PlayerStats : Node
{
    // ============================================================
    //  基础值
    // ============================================================

    public float BaseDamage = 1f;
    public float BaseAttackSpeed = 1f;      // 冷却倍数，越小越快
    public float BaseMoveSpeed = 5f;
    public float BaseMaxHealth = 100f;
    public float BasePickupRange = 2f;
    public int BaseMagazineSize = 7;
    public float BaseExpGain = 1f;

    // ============================================================
    //  升级累加倍率
    // ============================================================

    public float DamageMultiplier = 1f;
    public float AttackSpeedMultiplier = 1f;
    public float MoveSpeedMultiplier = 1f;
    public float MaxHealthMultiplier = 1f;
    public float PickupRangeMultiplier = 1f;
    public float ExpGainMultiplier = 1f;
    public int BonusMagazine = 0;

    // ============================================================
    //  最终值
    // ============================================================

    public float FinalDamage => BaseDamage * DamageMultiplier;
    public float FinalAttackSpeed => BaseAttackSpeed / AttackSpeedMultiplier;
    public float FinalMoveSpeed => BaseMoveSpeed * MoveSpeedMultiplier;
    public float FinalMaxHealth => BaseMaxHealth * MaxHealthMultiplier;
    public float FinalPickupRange => BasePickupRange * PickupRangeMultiplier;
    public int FinalMagazineSize => BaseMagazineSize + BonusMagazine;
    public float FinalExpGain => BaseExpGain * ExpGainMultiplier;
}