using ChevalGrandSlay.Characters;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib;

namespace ChevalGrandSlay.Utilities;

internal static class CGSLowHealthScreen
{
    private const string ShaderPath = Entry.ResPath + "/shaders/CGSLowHealthScreen.gdshader";
    private const float LowHealthThreshold = 0.30f;

    private static NRun? _run;
    private static Creature? _creature;
    private static CanvasLayer? _layer;

    public static void Register()
    {
        RitsuLibFramework.SubscribeLifecycle<RoomEnteredEvent>(AttachToRun, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => Detach(), replayCurrentState: false);
    }

    private static void AttachToRun(RoomEnteredEvent entered)
    {
        var run = NRun.Instance;
        if (run == null || run == _run || !run.IsInsideTree())
            return;

        var player = LocalContext.GetMe(entered.RunState.Players);
        if (player?.Character is not CGSCharacter)
            return;

        Detach();
        _run = run;
        run.TreeExiting += Detach;

        var shader = GD.Load<Shader>(ShaderPath);
        if (shader == null)
        {
            Entry.Logger.Warn($"[Low health screen] Cannot load shader: {ShaderPath}");
            return;
        }

        var layer = new CanvasLayer { Name = "CGSLowHealthScreen", Layer = 1, Visible = false };
        run.AddChild(layer);
        // Capture the composed screen even when a previous effect has already sampled it.
        layer.AddChild(new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport });
        var overlay = new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = shader }
        };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _layer = layer;
        _creature = player.Creature;
        _creature.CurrentHpChanged += UpdateVisibility;
        _creature.MaxHpChanged += UpdateVisibility;
        UpdateVisibility(0, 0);
    }

    private static void UpdateVisibility(int previousHp, int currentHp)
    {
        if (_layer == null || _creature == null)
            return;

        _layer.Visible = _creature.CurrentHp > 0 &&
            _creature.CurrentHp <= _creature.MaxHp * LowHealthThreshold;
    }

    private static void Detach()
    {
        if (_creature != null)
        {
            _creature.CurrentHpChanged -= UpdateVisibility;
            _creature.MaxHpChanged -= UpdateVisibility;
            _creature = null;
        }

        if (GodotObject.IsInstanceValid(_run))
            _run!.TreeExiting -= Detach;
        _run = null;

        if (GodotObject.IsInstanceValid(_layer))
        {
            _layer!.Visible = false;
            _layer.QueueFree();
        }
        _layer = null;
    }
}
