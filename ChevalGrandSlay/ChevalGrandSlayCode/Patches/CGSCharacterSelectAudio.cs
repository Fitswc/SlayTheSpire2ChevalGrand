using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

internal sealed class CGSCharacterSelectAudio : IPatchMethod
{
    public static string PatchId => "CGSCharacterSelectAudio";

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)])];

    private const string Intro = "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce";
    private const string Transition = "event:/ChevalGrandSlaySFX/SFX/CGSSTransition";

    private static AudioEventHandle? _introHandle;

    public static bool Prefix([HarmonyArgument(0)] string sfx, [HarmonyArgument(1)] float volume)
    {
        if (sfx != Intro && sfx != Transition)
            return true;

        // 新的介绍或转场开始前，先停止上一次介绍；转场也覆盖多人同步出发。
        StopIntro();

        if (sfx == Transition)
            return true;

        // 保留 SfxCmd 原有的播放条件。
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return false;

        _introHandle = FmodStudioEventInstances.TryCreateHandle(
            AudioSource.Event(Intro), new AudioPlaybackOptions());
        if (_introHandle == null)
        {
            Entry.Logger.Warn("The character introduction audio instance CANNOT be created.");
            return false;
        }

        _introHandle.TrySetVolume(volume);

        if (!_introHandle.TryPlay())
            StopIntro();

        // 自我介绍由本补丁播放，跳过原方法，避免重复播放。
        return false;
    }

    internal static void StopIntro()
    {
        var handle = _introHandle;
        _introHandle = null;

        handle?.TryStop(allowFadeOut: false);
        handle?.TryRelease();
    }
}

// 切换角色、出发或关闭子菜单时，停止尚未播完的自我介绍。
internal sealed class CGSCharacterSelectStopIntroPatch : IPatchMethod
{
    public static string PatchId => "character_select_stop_intro";

    public static ModPatchTarget[] GetTargets()
    {
        //Version require
        var screen = typeof(NCharacterSelectScreen);
        var randomCharacterMethod = AccessTools.DeclaredMethod(screen, "DoRandomCharacterChange") is not null
            ? "DoRandomCharacterChange"
            : "OnLocalCharacterChangedForRandom";
        return
        [
            new(screen, "SelectCharacter"),
            new(screen, randomCharacterMethod),
            new(screen, "OnEmbarkPressed"),
            new(screen, "OnSubmenuClosed")
        ];
    }

    public static void Prefix()
    {
        CGSCharacterSelectAudio.StopIntro();
    }
}
