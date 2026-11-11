using Godot;
using System.Collections.Generic;

/// <summary>
/// 升级选项池：管理普通和稀有选项，提供随机三选一。
/// 设为 Autoload，节点名 UpgradePool。
/// </summary>
public partial class UpgradePool : Node
{
    private List<UpgradeOption> _normalOptions = new();
    private List<UpgradeOption> _rareOptions = new();

    public override void _Ready()
    {
        BuildOptions();
    }

    private void BuildOptions()
    {
        // ---------- 普通选项 ----------
        _normalOptions.Add(new UpgradeOption
        {
            Title = "伤害提升",
            Description = "伤害 +15%",
            ApplyEffect = (p, s) => s.DamageMultiplier += 0.15f
        });

        _normalOptions.Add(new UpgradeOption
        {
            Title = "弹药扩充",
            Description = "弹匣 +1",
            ApplyEffect = (p, s) => s.BonusMagazine += 1
        });

        _normalOptions.Add(new UpgradeOption
        {
            Title = "攻速提升",
            Description = "攻速 +10%",
            ApplyEffect = (p, s) => s.AttackSpeedMultiplier += 0.10f
        });

        _normalOptions.Add(new UpgradeOption
        {
            Title = "生命提升",
            Description = "最大生命 +20",
            ApplyEffect = (p, s) =>
            {
                s.MaxHealthMultiplier += 0.20f;
                p.HealBy(20f);
            }
        });

        _normalOptions.Add(new UpgradeOption
        {
            Title = "拾取范围",
            Description = "拾取范围 +20%",
            ApplyEffect = (p, s) => s.PickupRangeMultiplier += 0.20f
        });

        _normalOptions.Add(new UpgradeOption
        {
            Title = "经验获取",
            Description = "经验获取 +10%",
            ApplyEffect = (p, s) => s.ExpGainMultiplier += 0.10f
        });

        // ---------- 稀有选项 ----------
        _rareOptions.Add(new UpgradeOption
        {
            Title = "武器升级",
            Description = "弹幕数量 +1",
            IsRare = true,
            ApplyEffect = (p, s) => p.AddProjectileCount(1)
        });

        _rareOptions.Add(new UpgradeOption
        {
            Title = "击杀回血",
            Description = "击杀敌人回复 2 点生命",
            IsRare = true,
            ApplyEffect = (p, s) => p.EnableLifeSteal(2f)
        });

        _rareOptions.Add(new UpgradeOption
        {
            Title = "护盾",
            Description = "每隔 5 秒免疫一次伤害",
            IsRare = true,
            ApplyEffect = (p, s) => p.EnableShield(5f)
        });
    }

    /// <summary>
    /// 随机抽取 3 个不重复的选项。稀有选项有概率出现。
    /// </summary>
    public List<UpgradeOption> RollThree()
    {
        var result = new List<UpgradeOption>();
        var pool = new List<UpgradeOption>(_normalOptions);

        // 20% 概率混入一个稀有
        if (GD.Randf() < 0.2f && _rareOptions.Count > 0)
        {
            int rareIdx = GD.RandRange(0, _rareOptions.Count - 1);
            result.Add(_rareOptions[rareIdx]);
        }

        // 从普通池补足到 3 个
        while (result.Count < 3 && pool.Count > 0)
        {
            int idx = GD.RandRange(0, pool.Count - 1);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }
}