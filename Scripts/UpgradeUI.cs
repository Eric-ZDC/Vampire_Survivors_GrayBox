using Godot;
using System.Collections.Generic;

/// <summary>
/// 升级三选一 UI。暂停游戏，展示三个选项，玩家选择后应用效果并恢复。
/// 直接挂在 Main/UI/UpgradeUI 节点上。
/// </summary>
public partial class UpgradeUI : CanvasLayer
{
    private HBoxContainer _buttonContainer;
    private PanelContainer _panel;
    private Player _player;
    private PlayerStats _stats;
    private UpgradePool _pool;

    public override void _Ready()
    {
        // 路径对应 Main/UI/UpgradeUI 下的子节点
        _panel = GetNode<PanelContainer>("PanelContainer");
        _buttonContainer = GetNode<HBoxContainer>("PanelContainer/HBoxContainer");

        _pool = GetNode<UpgradePool>("/root/UpgradePool");

        _panel.Visible = false;

        if (_pool == null)
            GD.PrintErr("UpgradeUI 找不到 UpgradePool，请确认已设为 Autoload");
    }

    /// <summary>
    /// 显示三选一。由 Player 升级时调用。
    /// </summary>
    public void ShowUpgrade()
    {
        // 每次弹出时重新获取玩家，避免 _Ready 时玩家还没进组
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        _stats = _player?.GetNodeOrNull<PlayerStats>("PlayerStats");

        GD.Print($"[UpgradeUI] _player = {(_player == null ? "null" : "ok")}, _stats = {(_stats == null ? "null" : "ok")}");

        if (_pool == null)
        {
            GD.PrintErr("UpgradeUI 找不到 UpgradePool");
            return;
        }

        if (_player == null)
        {
            GD.PrintErr("UpgradeUI 找不到 Player，请确认 Player 在 player 组里");
            return;
        }

        // 清空旧按钮
        foreach (Node child in _buttonContainer.GetChildren())
            child.QueueFree();

        var options = _pool.RollThree();
        foreach (var opt in options)
        {
            var btn = new Button();
            btn.Text = $"{opt.Title}\n{opt.Description}";
            btn.CustomMinimumSize = new Vector2(160, 80);

            if (opt.IsRare)
                btn.Modulate = new Color(1f, 0.85f, 0.3f);

            var captured = opt;
            btn.Pressed += () => OnOptionSelected(captured);

            _buttonContainer.AddChild(btn);
        }

        _panel.Visible = true;
        GetTree().Paused = true;
    }

    private void OnOptionSelected(UpgradeOption opt)
    {
        GD.Print($"[UpgradeUI] 选择选项: {opt.Title}");
        GD.Print($"[UpgradeUI] _player = {(_player == null ? "null" : "ok")}, _stats = {(_stats == null ? "null" : "ok")}");

        if (opt.ApplyEffect != null)
            opt.ApplyEffect.Invoke(_player, _stats);
        else
            GD.PrintErr($"选项 {opt.Title} 没有 ApplyEffect 回调");

        _panel.Visible = false;
        GetTree().Paused = false;

        _player?.OnUpgradeUIFinished();
    }
}