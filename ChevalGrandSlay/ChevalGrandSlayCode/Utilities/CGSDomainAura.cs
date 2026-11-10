using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ChevalGrandSlay.Utilities;

public partial class CGSDomainAura : CanvasGroup
{
    private Node2D? _body;
    private Tween? _fadeTween;
    private int _bodyIndex;

    public static void Show(Creature creature)
    {
        var body = creature.GetCreatureNode()?.Visuals.GetCurrentBody();
        if (body == null || !GodotObject.IsInstanceValid(body) || !body.IsInsideTree() ||
            body.GetParent() is CGSDomainAura)
            return;

        var shader = GD.Load<Shader>(Entry.ResPath + "/shaders/CGSDomainAura.gdshader");
        if (shader == null)
        {
            Entry.Logger.Warn("Cannot load domain character aura shader.");
            return;
        }

        var parent = body.GetParent();
        var material = new ShaderMaterial { Shader = shader };
        var aura = new CGSDomainAura
        {
            Name = "CGSDomainAura",
            Material = material,
            // Leave room around the silhouette for the outline and leftward trail.
            FitMargin = 72f,
            ClearMargin = 72f,
            _body = body,
            _bodyIndex = body.GetIndex()
        };
        parent.AddChild(aura);
        parent.MoveChild(aura, aura._bodyIndex);
        body.Reparent(aura, false);

        material.SetShaderParameter("intensity", 0f);
        aura._fadeTween = aura.CreateTween();
        aura._fadeTween.TweenProperty(material, "shader_parameter/intensity", 1f, 0.3)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    public static void Remove(Creature creature)
    {
        if (creature.GetCreatureNode()?.Visuals.GetCurrentBody().GetParent() is not CGSDomainAura aura)
            return;

        if (GodotObject.IsInstanceValid(aura._fadeTween))
            aura._fadeTween!.Kill();
        var parent = aura.GetParent();
        if (GodotObject.IsInstanceValid(aura._body))
        {
            aura._body!.Reparent(parent, false);
            parent.MoveChild(aura._body, aura._bodyIndex);
        }
        aura.Hide();
        aura.QueueFree();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_fadeTween))
            _fadeTween!.Kill();
    }
}
