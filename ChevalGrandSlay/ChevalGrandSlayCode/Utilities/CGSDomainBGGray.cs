using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ChevalGrandSlay.Utilities;

public class CGSDomainBGGray
{
    private const string BackgroundShaderPath = Entry.ResPath + "/shaders/CGSDomainBGGrayScale.gdshader";

    private sealed record BackgroundEffect(
        NCombatRoom Room,
        Node Background,
        Control Parent,
        int OriginalIndex,
        CanvasGroup Group,
        Control Layout,
        Tween Tween);

    private static BackgroundEffect? _effect;
    private static bool _reportedUnsupportedBackground;

    /// 获得领域时调用；将当前战斗背景从画面中间向上下展开为黑白。
    public static void FadeBackgroundToGrayscale(double duration)
    {
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree())
            return;

        // 多名玩家获得领域时共用同一个背景效果，不重复创建或重置渐变。
        if (_effect is { } active && active.Room == room && GodotObject.IsInstanceValid(active.Group))
            return;

        RestoreBackgroundColor();
        if (TryFindBackground(room) is not { } target)
            return;

        var (background, parent) = target;
        var shader = GD.Load<Shader>(BackgroundShaderPath);
        if (shader == null)
        {
            Entry.Logger.Warn($"Cannot load domain background shader: {BackgroundShaderPath}");
            return;
        }

        int originalIndex = background.GetIndex();
        var material = new ShaderMaterial { Shader = shader };
        var group = new CanvasGroup { Name = "CGSDomainBackground", Material = material };
        // 用等大的 Control 保持原背景的锚点布局，CanvasGroup 本身没有 Control 尺寸。
        var layout = new Control
        {
            Name = "BackgroundLayout",
            Size = parent.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        parent.AddChild(group);
        parent.MoveChild(group, originalIndex);
        group.AddChild(layout);
        background.Reparent(layout, false);

        var tween = room.CreateTween();
        _effect = new BackgroundEffect(room, background, parent, originalIndex, group, layout, tween);
        parent.Resized += ResizeBackgroundLayout;
        room.TreeExiting += RestoreBackgroundColor;

        material.SetShaderParameter("reveal_progress", 0f);
        tween.TweenProperty(material, "shader_parameter/reveal_progress", 1f, Math.Max(0.0, duration))
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    private static (Node Background, Control Parent)? TryFindBackground(NCombatRoom room)
    {
        var background = room.Background;
        if (!GodotObject.IsInstanceValid(background) || background.IsQueuedForDeletion())
        {
            ReportUnsupportedBackground("NCombatRoom.Background is missing or being freed.");
            return null;
        }

        var parent = background.GetParent();
        if (parent is Control control && GodotObject.IsInstanceValid(control) &&
            !control.IsQueuedForDeletion() && control.GetChildren().Contains(background))
            return (background, control);

        ReportUnsupportedBackground(
            $"Expected a background child of a live Control, found parent {parent?.GetType().Name ?? "<none>"}.");
        return null;
    }

    private static void ReportUnsupportedBackground(string reason)
    {
        if (_reportedUnsupportedBackground)
            return;

        _reportedUnsupportedBackground = true;
        Entry.Logger.Warn($"[Domain background] Grayscale skipped. {reason}");
    }

    private static void ResizeBackgroundLayout()
    {
        if (_effect is { } effect && GodotObject.IsInstanceValid(effect.Parent) &&
            GodotObject.IsInstanceValid(effect.Layout))
            effect.Layout.Size = effect.Parent.Size;
    }

    /// <summary>战斗结束或退出场景时恢复背景。</summary>
    public static void RestoreBackgroundColor()
    {
        if (_effect is not { } effect)
            return;

        _effect = null;
        if (GodotObject.IsInstanceValid(effect.Tween))
            effect.Tween.Kill();
        if (GodotObject.IsInstanceValid(effect.Room))
            effect.Room.TreeExiting -= RestoreBackgroundColor;
        if (GodotObject.IsInstanceValid(effect.Parent))
            effect.Parent.Resized -= ResizeBackgroundLayout;

        if (GodotObject.IsInstanceValid(effect.Background) && effect.Background.GetParent() == effect.Layout &&
            GodotObject.IsInstanceValid(effect.Parent) && !effect.Parent.IsQueuedForDeletion())
        {
            effect.Background.Reparent(effect.Parent, false);
            effect.Parent.MoveChild(effect.Background, Math.Min(effect.OriginalIndex, effect.Parent.GetChildCount() - 1));
        }

        if (!GodotObject.IsInstanceValid(effect.Group) || effect.Group.IsQueuedForDeletion())
            return;

        if (GodotObject.IsInstanceValid(effect.Background) && effect.Group.IsAncestorOf(effect.Background))
        {
            effect.Group.Material = null;
            Entry.Logger.Warn("[Domain background] Cannot restore the original background parent. Kept the wrapper to preserve the background.");
            return;
        }

        effect.Group.Hide();
        effect.Group.QueueFree();
    }
}
