using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Audio;

namespace ChevalGrandSlay;

internal static class CharacterSelectAudio
{
    private const string Intro =
        "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce";

    private const string Transition =
        "event:/ChevalGrandSlaySFX/SFX/CGSSTransition";

    private static GodotObject? _introInstance;

    public static void Install()
    {
        var harmony = new Harmony($"{Entry.ModId}.CharacterSelectAudio");

        // 接管 SfxCmd.Play(string, float) 中的介绍语音。
        harmony.Patch(
            AccessTools.Method(
                typeof(SfxCmd),
                nameof(SfxCmd.Play),
                new[] { typeof(string), typeof(float) }),
            prefix: new HarmonyMethod(
                typeof(CharacterSelectAudio),
                nameof(BeforePlay)));

        // 在界面操作之前停止介绍语音。
        foreach (var method in new[]
                 {
                     "SelectCharacter",
                     "OnLocalCharacterChangedForRandom",
                     "OnEmbarkPressed",
                     "OnSubmenuClosed"
                 })
        {
            harmony.Patch(
                AccessTools.Method(typeof(NCharacterSelectScreen), method),
                prefix: new HarmonyMethod(
                    typeof(CharacterSelectAudio),
                    nameof(StopIntro)));
        }
    }

    private static bool BeforePlay(string sfx, float volume)
    {
        // 转场开始前再清理一次，也覆盖多人同步出发。
        if (sfx == Transition)
        {
            StopIntro();
            return true;
        }

        // 其他音效继续使用游戏原来的逻辑。
        if (sfx != Intro)
            return true;

        StopIntro();

        // 保留 SfxCmd 原有的播放条件。
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return false;

        _introInstance = FmodStudioEventInstances.TryCreate(sfx);
        if (_introInstance == null)
        {
            Entry.Logger.Info("The character introduction audio instance cannot be created.");
            return false;
        }

        _introInstance.Call("set_volume", volume);

        if (!FmodStudioEventInstances.TryStart(_introInstance))
            StopIntro();

        // 禁止原方法再次播放，避免同一句播放两次。
        return false;
    }

    private static void StopIntro()
    {
        var instance = _introInstance;
        _introInstance = null;

        if (instance == null || !GodotObject.IsInstanceValid(instance))
            return;

        FmodStudioEventInstances.TryStop(instance, allowFadeOut: false);
        FmodStudioEventInstances.TryRelease(instance);
    }
}