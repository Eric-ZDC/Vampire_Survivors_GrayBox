using Godot;

/// <summary>
/// 单个升级选项的数据。包含名称、描述、稀有度和应用效果的回调。
/// </summary>
public partial class UpgradeOption : Resource
{
    [Export] public string Title = "";
    [Export] public string Description = "";
    [Export] public bool IsRare = false;

    // 应用效果的回调，参数是 Player 和 PlayerStats
    public System.Action<Player, PlayerStats> ApplyEffect;
}