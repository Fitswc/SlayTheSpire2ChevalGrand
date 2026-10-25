using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ChevalGrandSlay.Utilities;

public class CGSDomainBGGray
{
    private static NCombatRoom? _backgroundRoom;
    private static CanvasGroup? _backgroundGroup;
    private static Control? _backgroundLayout;
    private static Tween? _backgroundTween;
    private static int _backgroundIndex;

    private const string BackgroundShaderPath = Entry.ResPath + "/shaders/CGSDomainBGGrayScale.gdshader";

    /// 获得领域时调用；只将当前战斗背景逐渐变为黑白。
    public static void FadeBackgroundToGrayscale(double duration = 0.8)
    {
        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) ||
            room.Background == null || !room.IsInsideTree())
            return;

        // 多名玩家获得领域时共用同一个背景效果，不重复创建或重置渐变。
        if (_backgroundRoom == room && GodotObject.IsInstanceValid(_backgroundGroup))
            return;

        var shader = GD.Load<Shader>(BackgroundShaderPath);
        if (shader == null)
        {
            Entry.Logger.Warn($"Cannot load domain background shader: {BackgroundShaderPath}");
            return;
        }

        RestoreBackgroundColor();
        _backgroundRoom = room;
        _backgroundIndex = room.Background.GetIndex();
        var material = new ShaderMaterial { Shader = shader };
        _backgroundGroup = new CanvasGroup { Name = "CGSDomainBackground", Material = material };
        // 用等大的 Control 保持原背景的锚点布局，CanvasGroup 本身没有 Control 尺寸。
        _backgroundLayout = new Control
        {
            Name = "BackgroundLayout",
            Size = room.BgContainer.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        room.BgContainer.AddChild(_backgroundGroup);
        room.BgContainer.MoveChild(_backgroundGroup, _backgroundIndex);
        _backgroundGroup.AddChild(_backgroundLayout);
        room.Background.Reparent(_backgroundLayout, false);
        room.BgContainer.Resized += ResizeBackgroundLayout;
        room.TreeExiting += RestoreBackgroundColor;

        material.SetShaderParameter("saturation", 1f);
        _backgroundTween = room.CreateTween();
        _backgroundTween.TweenProperty(material, "shader_parameter/saturation", 0f, Math.Max(0.0, duration))
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private static void ResizeBackgroundLayout()
    {
        if (GodotObject.IsInstanceValid(_backgroundRoom) && GodotObject.IsInstanceValid(_backgroundLayout))
            _backgroundLayout!.Size = _backgroundRoom!.BgContainer.Size;
    }

    /// <summary>战斗结束或退出战斗场景时恢复背景，不更改全局环境参数。</summary>>
    public static void RestoreBackgroundColor()
    {
        if (GodotObject.IsInstanceValid(_backgroundTween))
            _backgroundTween!.Kill();
        _backgroundTween = null;

        if (GodotObject.IsInstanceValid(_backgroundRoom))
        {
            var room = _backgroundRoom!;
            room.TreeExiting -= RestoreBackgroundColor;
            room.BgContainer.Resized -= ResizeBackgroundLayout;
            if (GodotObject.IsInstanceValid(room.Background) && room.Background.GetParent() == _backgroundLayout)
            {
                room.Background.Reparent(room.BgContainer, false);
                room.BgContainer.MoveChild(room.Background, _backgroundIndex);
            }
        }

        if (GodotObject.IsInstanceValid(_backgroundGroup))
        {
            _backgroundGroup!.Hide();
            _backgroundGroup.QueueFree();
        }
        _backgroundRoom = null;
        _backgroundGroup = null;
        _backgroundLayout = null;
    }
}
